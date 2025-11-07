using Microsoft.Extensions.Primitives;

namespace StudyPlannerApi.Middleware
{
    public class ApiKeyAuthenticationMiddleware
    {
        private const string API_KEY_HEADER_NAME = "X-API-Key";
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

        public ApiKeyAuthenticationMiddleware(
            RequestDelegate next, 
            IConfiguration configuration,
            ILogger<ApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for Swagger endpoints
            if (context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/index.html") ||
                context.Request.Path.Value?.EndsWith(".css") == true ||
                context.Request.Path.Value?.EndsWith(".js") == true ||
                context.Request.Path.Value?.EndsWith(".json") == true)
            {
                await _next(context);
                return;
            }

            // Check if API key is present in headers
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out StringValues extractedApiKey))
            {
                _logger.LogWarning("API Key missing from request");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    error = "Unauthorized", 
                    message = $"API Key is required. Include '{API_KEY_HEADER_NAME}' header in your request." 
                });
                return;
            }

            // Get the API key from configuration
            var configuredApiKey = _configuration["ApiKey"];
            
            if (string.IsNullOrEmpty(configuredApiKey))
            {
                _logger.LogError("API Key is not configured in application settings");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    error = "Internal Server Error", 
                    message = "API Key is not configured on the server." 
                });
                return;
            }

            // Validate the API key
            if (!configuredApiKey.Equals(extractedApiKey.ToString()))
            {
                _logger.LogWarning("Invalid API Key provided");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    error = "Unauthorized", 
                    message = "Invalid API Key." 
                });
                return;
            }

            _logger.LogInformation("API Key validated successfully");
            await _next(context);
        }
    }

    public static class ApiKeyAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        }
    }
}
