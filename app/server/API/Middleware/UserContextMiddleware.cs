using System.Security.Claims;
using API.Interfaces;
using API.Services;

namespace API.Middleware;

public class UserContextMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger) {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserService userService) {
        if (context.User.Identity?.IsAuthenticated == true) {
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                       ?? context.User.FindFirst("email")?.Value;

            var name = context.User.FindFirst(ClaimTypes.Name)?.Value
                      ?? context.User.FindFirst("name")?.Value
                      ?? context.User.FindFirst("preferred_username")?.Value;

            if (!string.IsNullOrEmpty(email)) {
                try {
                    var user = await userService.GetOrCreateUserAsync(email, name);
                    context.Items["User"] = user;
                    context.Items["UserEmail"] = email;

                    _logger.LogDebug("Authenticated request from user: {Email}", email);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Failed to get or create user for email: {Email}", email);
                }
            } else {
                _logger.LogWarning("Authenticated user has no email claim");
            }
        }

        await _next(context);
    }
}