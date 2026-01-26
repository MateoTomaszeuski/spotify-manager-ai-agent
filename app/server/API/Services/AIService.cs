using System.Text.Json;
using API.Interfaces;
using API.Models.AI;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

namespace API.Services;

public class AIService : IAIService {
    private readonly AzureOpenAIClient _azureClient;
    private readonly ChatClient _chatClient;
    private readonly ILogger<AIService> _logger;
    private readonly string _model;
    private const int SummaryTriggerTokens = 100000;

    public AIService(IConfiguration configuration, ILogger<AIService> logger) {
        _logger = logger;

        var endpoint = configuration["AzureAI:Endpoint"] ?? throw new InvalidOperationException("AzureAI:Endpoint is required in configuration");
        var apiKey = configuration["AzureAI:ApiKey"];
        _model = configuration["AzureAI:Model"] ?? "gpt-4o";

        if (!string.IsNullOrEmpty(apiKey)) {
            _azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        } else {
            _azureClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        }
        
        _chatClient = _azureClient.GetChatClient(_model);
    }

    public int EstimateTokenCount(string text) {
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    public int CountMessageTokens(List<AIMessage> messages) {
        var totalTokens = 0;
        foreach (var msg in messages) {
            if (!string.IsNullOrEmpty(msg.Content)) {
                totalTokens += EstimateTokenCount(msg.Content);
            }
            if (msg.ToolCalls != null) {
                totalTokens += EstimateTokenCount(JsonSerializer.Serialize(msg.ToolCalls));
            }
        }
        return totalTokens;
    }

    public bool NeedsSummarization(int tokenCount) {
        return tokenCount >= SummaryTriggerTokens;
    }

    public async Task<string> GenerateSummaryAsync(List<AIMessage> messages) {
        try {
            var conversationText = string.Join("\n\n", messages.Select(m => $"{m.Role}: {m.Content}"));

            var summaryMessages = new List<AIMessage>
            {
                new AIMessage(
                    "system",
                    "You are a conversation summarizer. Create a concise but comprehensive summary of the following conversation that preserves all important context, decisions, and information. The summary will be used to maintain context in an ongoing conversation."
                ),
                new AIMessage(
                    "user",
                    $"Please summarize the following conversation:\n\n{conversationText}"
                )
            };

            var response = await GetChatCompletionAsync(summaryMessages);
            return response.Response ?? "Summary generation failed";
        } catch (Exception ex) {
            _logger.LogError(ex, "Error generating summary");
            return "Unable to generate summary";
        }
    }

    public async Task<AIResponse> GetChatCompletionAsync(List<AIMessage> messages, List<AITool>? tools = null) {
        try {
            var chatMessages = new List<ChatMessage>();

            foreach (var msg in messages) {
                switch (msg.Role.ToLower()) {
                    case "system":
                        chatMessages.Add(ChatMessage.CreateSystemMessage(msg.Content));
                        break;
                    case "user":
                        chatMessages.Add(ChatMessage.CreateUserMessage(msg.Content));
                        break;
                    case "assistant":
                        chatMessages.Add(ChatMessage.CreateAssistantMessage(msg.Content));
                        break;
                    case "tool":
                        chatMessages.Add(ChatMessage.CreateToolMessage(msg.ToolCallId ?? "", msg.Content));
                        break;
                }
            }

            var completionOptions = new ChatCompletionOptions {
                Temperature = 0.9f,
                TopP = 0.95f
            };

            if (tools != null && tools.Count > 0) {
                foreach (var tool in tools) {
                    var functionDefinition = ChatTool.CreateFunctionTool(
                        tool.Function.Name,
                        tool.Function.Description,
                        BinaryData.FromString(JsonSerializer.Serialize(tool.Function.Parameters))
                    );
                    completionOptions.Tools.Add(functionDefinition);
                }
            }

            _logger.LogDebug("Azure OpenAI API Request - Model: {Model}, Messages: {MessageCount}", _model, chatMessages.Count);

            var completion = await _chatClient.CompleteChatAsync(chatMessages, completionOptions);

            if (completion == null || completion.Value == null) {
                return new AIResponse("", Error: "No response from Azure OpenAI service");
            }

            var result = completion.Value;

            if (result.FinishReason == ChatFinishReason.ToolCalls && result.ToolCalls.Count > 0) {
                var executedToolCalls = new List<ExecutedToolCall>();

                foreach (var toolCall in result.ToolCalls) {
                    if (toolCall.Kind == ChatToolCallKind.Function) {
                        var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.FunctionArguments)
                            ?? new Dictionary<string, object>();

                        executedToolCalls.Add(new ExecutedToolCall(
                            toolCall.Id,
                            toolCall.FunctionName,
                            arguments
                        ));
                    }
                }

                var content = result.Content.Count > 0 ? string.Join("", result.Content.Select(c => c.Text)) : "";
                return new AIResponse(content, executedToolCalls);
            }

            var responseContent = result.Content.Count > 0 ? string.Join("", result.Content.Select(c => c.Text)) : "";
            return new AIResponse(responseContent);
        } catch (RequestFailedException ex) {
            _logger.LogError(ex, "Azure OpenAI service request failed: {StatusCode} {Message}", ex.Status, ex.Message);

            var errorMessage = ex.Status switch {
                429 => "Azure OpenAI service rate limit exceeded. Please try again later.",
                503 => "Azure OpenAI service is temporarily unavailable. Please try again later.",
                _ => $"Azure OpenAI service request failed: {ex.Message}"
            };

            return new AIResponse("", Error: errorMessage);
        } catch (Exception ex) {
            _logger.LogError(ex, "Azure OpenAI Service error");
            return new AIResponse("", Error: ex.Message);
        }
    }
}