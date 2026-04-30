using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NoMercyBot.Api.Middleware;

public class AuthorizationLoggingMiddleware(
    RequestDelegate next,
    ILogger<AuthorizationLoggingMiddleware> logger
)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.StatusCode is 401 or 403)
        {
            string method = context.Request.Method;
            string path = context.Request.Path;
            string query = context.Request.QueryString.ToString();
            string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string userAgent = context.Request.Headers.UserAgent.ToString();
            string endpoint = context.GetEndpoint()?.DisplayName ?? "unknown";
            string user = context.User.Identity?.Name ?? "anonymous";
            bool isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

            logger.LogWarning(
                "Auth {StatusCode} | {Method} {Path}{Query} | Endpoint: {Endpoint} | IP: {IP} | User: {User} (authenticated: {IsAuth}) | UA: {UserAgent}",
                context.Response.StatusCode,
                method,
                path,
                query,
                endpoint,
                ip,
                user,
                isAuthenticated,
                userAgent
            );
        }
    }
}
