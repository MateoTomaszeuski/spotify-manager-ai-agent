using API.Data;
using API.Hubs;
using API.Interfaces;
using API.Middleware;
using API.Repositories;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' is not configured");

builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IAgentActionRepository, AgentActionRepository>();
builder.Services.AddScoped<IThemeRepository, ThemeRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IThemeService, ThemeService>();

builder.Services.AddHttpClient<API.Interfaces.ISpotifyAuthService, API.Services.Spotify.SpotifyAuthService>();
builder.Services.AddHttpClient<API.Interfaces.ISpotifyUserService, API.Services.Spotify.SpotifyUserService>();
builder.Services.AddHttpClient<API.Interfaces.ISpotifyPlaylistService, API.Services.Spotify.SpotifyPlaylistService>();
builder.Services.AddHttpClient<API.Interfaces.ISpotifyTrackService, API.Services.Spotify.SpotifyTrackService>();
builder.Services.AddScoped<ISpotifyService, SpotifyService>();
builder.Services.AddScoped<ISpotifyTokenService, SpotifyTokenService>();

builder.Services.AddScoped<API.Interfaces.IAgentNotificationService, API.Services.Agents.AgentNotificationService>();
builder.Services.AddScoped<API.Interfaces.IDuplicateCleanerService, API.Services.Agents.DuplicateCleanerService>();
builder.Services.AddScoped<API.Interfaces.IPlaylistCreatorService, API.Services.Agents.PlaylistCreatorService>();
builder.Services.AddScoped<API.Interfaces.IMusicDiscoveryService, API.Services.Agents.MusicDiscoveryService>();
builder.Services.AddScoped<API.Interfaces.IMusicSuggestionService, API.Services.Agents.MusicSuggestionService>();
builder.Services.AddScoped<API.Interfaces.IAgentAnalyticsService, API.Services.Agents.AgentAnalyticsService>();
builder.Services.AddScoped<API.Interfaces.IAgentHistoryService, API.Services.Agents.AgentHistoryService>();

builder.Services.AddScoped<API.Interfaces.IAIPromptBuilder, API.Services.AI.AIPromptBuilder>();
builder.Services.AddScoped<API.Interfaces.IAIResponseParser, API.Services.AI.AIResponseParser>();

builder.Services.AddScoped<API.Interfaces.IDiscoveryQueryGenerator, API.Services.Helpers.DiscoveryQueryGenerator>();
builder.Services.AddScoped<API.Interfaces.ITrackDiscoveryHelper, API.Services.Helpers.TrackDiscoveryHelper>();

builder.Services.AddScoped<API.Interfaces.IPlaylistAnalyticsService, API.Services.Spotify.PlaylistAnalyticsService>();

builder.Services.AddSingleton<IAIService, AIService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "https://localhost:5173",
                  "https://127.0.0.1:5173",
                  "https://spotify.mateo.tomaszeuski.com",
                  "https://accounts.google.com"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("*");
    });
});

builder.Services.AddSignalR();

// Google OAuth Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        var googleClientId = builder.Configuration["Google:ClientId"]
                             ?? throw new InvalidOperationException("Google:ClientId is not configured");

        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.Authority = "https://accounts.google.com";
        
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuers = new[] { "https://accounts.google.com", "accounts.google.com" },
            ValidateAudience = true,
            ValidAudience = googleClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Fetch Google's public keys for token validation
        options.MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";

        options.Events = new JwtBearerEvents {
            OnAuthenticationFailed = context => {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context => {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var email = context.Principal?.FindFirst("email")?.Value ?? "unknown";
                logger.LogDebug("Token validated for user: {Email}", email);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "API is running!");
app.UseMiddleware<UserContextMiddleware>();
app.MapControllers().RequireAuthorization();
app.MapHub<AgentHub>("/hubs/agent").RequireAuthorization();

app.Run();

public partial class Program { }
