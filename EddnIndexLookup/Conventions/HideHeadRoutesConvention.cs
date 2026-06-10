using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace EddnIndexLookup.Conventions;

/// <summary>
/// Hide HEAD endpoints from API explorer
/// </summary>
public class HideHeadRoutesConvention : IActionModelConvention
{
    /// <inheritdoc/>
    public void Apply(ActionModel action)
    {
        foreach (var selector in action.Selectors)
        {
            if (selector.ActionConstraints?.OfType<HttpMethodActionConstraint>()
                .Any(c => c.HttpMethods.Contains("HEAD", StringComparer.OrdinalIgnoreCase)) == true)
            {
                selector.EndpointMetadata.Add(new ApiExplorerSettingsAttribute
                {
                    IgnoreApi = true
                });
            }
        }
    }
}
