using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EddnIndexLookup.Filters;

/// <summary>
/// Removes HEAD operations from the generated OpenAPI document.
/// </summary>
public sealed class HideHeadOperationsDocumentFilter : IDocumentFilter
{
    /// <inheritdoc/>
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var pathItem in swaggerDoc.Paths.Values)
        {
            // OpenAPI 2+ uses HttpMethod rather than OperationType
            pathItem.Operations?.Remove(HttpMethod.Head);
        }
    }
}