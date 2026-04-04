using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Muntada.Tenancy.Application.Services;
using Muntada.Tenancy.Domain.Tenant;

namespace Muntada.Tenancy.Infrastructure.Services;

/// <summary>
/// Middleware that resolves the current tenant from JWT claims or X-Tenant-ID header,
/// and enforces read-only access for suspended tenants.
/// Uses in-memory caching to avoid querying the database on every request.
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    /// <summary>Initializes a new instance of the middleware.</summary>
    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Resolves tenant context and enforces suspension read-only.</summary>
    public async Task InvokeAsync(HttpContext context, TenancyDbContext dbContext, TenantContextAccessor tenantContext, IMemoryCache cache)
    {
        var tenantIdString = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();

        if (string.IsNullOrEmpty(tenantIdString))
        {
            var claim = context.User.FindFirst("tenant_id");
            tenantIdString = claim?.Value;
        }

        if (!string.IsNullOrEmpty(tenantIdString) && Guid.TryParse(tenantIdString, out var tenantId))
        {
            var cacheKey = $"tenant_status:{tenantId}";

            var tenantStatus = await cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                var tenant = await dbContext.Tenants
                    .AsNoTracking()
                    .Where(t => t.Id == tenantId)
                    .Select(t => new { t.Id, t.Status })
                    .FirstOrDefaultAsync();

                return tenant is not null
                    ? new CachedTenantStatus(tenant.Id, tenant.Status)
                    : null;
            });

            if (tenantStatus is not null)
            {
                tenantContext.TenantId = tenantStatus.Id;
                tenantContext.HasTenant = true;
                tenantContext.IsSuspended = tenantStatus.Status == TenantStatus.Suspended;

                if (tenantContext.IsSuspended && IsMutatingRequest(context.Request.Method))
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        type = "https://muntada.com/errors/tenant-suspended",
                        title = "Tenant suspended",
                        status = 403,
                        detail = "This organization is suspended. You can view existing data but cannot create new resources. Contact your organization owner to resolve.",
                        code = "TENANT_SUSPENDED"
                    });
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool IsMutatingRequest(string method)
    {
        return method is "POST" or "PUT" or "PATCH" or "DELETE";
    }

    /// <summary>
    /// Cached tenant status to avoid repeated DB queries.
    /// </summary>
    private sealed record CachedTenantStatus(Guid Id, TenantStatus Status);
}

/// <summary>
/// Scoped service that holds the resolved tenant context for the current request.
/// </summary>
public class TenantContextAccessor : ITenantContext
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <inheritdoc />
    public bool HasTenant { get; set; }

    /// <inheritdoc />
    public bool IsSuspended { get; set; }
}
