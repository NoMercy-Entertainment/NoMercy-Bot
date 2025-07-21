using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NoMercyBot.Services;
using NoMercyBot.Services.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NoMercyBot.Server.Swagger;

public class ServiceProviderDocumentFilter : IDocumentFilter
{
    private readonly IEnumerable<IAuthService> _authServices;

    public ServiceProviderDocumentFilter(IEnumerable<IAuthService> authServices)
    {
        _authServices = authServices;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Get all available providers
        IEnumerable<string> providers = _authServices.Select(s => s.GetType().Name.Replace("AuthService", "").ToLower());

        // For each operation that has a {provider} parameter
        foreach (KeyValuePair<string, OpenApiPathItem> path in swaggerDoc.Paths)
        {
            foreach (KeyValuePair<OperationType, OpenApiOperation> operation in path.Value.Operations)
            {
                OpenApiParameter? providerParameter = operation.Value.Parameters
                    .FirstOrDefault(p => p.Name == "provider");

                if (providerParameter != null)
                {
                    // Add examples of available providers
                    providerParameter.Schema.Example = new OpenApiString("twitch");
                    providerParameter.Description += "\nAvailable providers: " + 
                        string.Join(", ", providers);
                    
                    // Optional: Add enum values
                    providerParameter.Schema.Enum = providers.Select(p => 
                        new OpenApiString(p)).ToList<IOpenApiAny>();
                }
            }
        }
    }
}