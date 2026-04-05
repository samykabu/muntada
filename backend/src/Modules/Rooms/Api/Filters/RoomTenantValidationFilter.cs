using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Muntada.Rooms.Api.Filters;

/// <summary>
/// Action filter that validates the tenantId route parameter is present and non-empty.
/// Applied to all Rooms module controllers for consistent tenant validation.
/// </summary>
public class RoomTenantValidationFilter : IActionFilter
{
    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.RouteData.Values.TryGetValue("tenantId", out var tenantId))
        {
            if (string.IsNullOrWhiteSpace(tenantId?.ToString()))
            {
                context.Result = new BadRequestObjectResult(new { error = "tenantId is required" });
            }
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context) { }
}
