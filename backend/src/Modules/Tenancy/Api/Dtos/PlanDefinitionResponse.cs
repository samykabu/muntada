namespace Muntada.Tenancy.Api.Dtos;

/// <summary>
/// Response DTO representing an available plan definition with its pricing and limits.
/// Returned when listing all active plans that tenants can subscribe to.
/// </summary>
/// <param name="Id">The unique identifier of the plan definition.</param>
/// <param name="Name">The display name of the plan.</param>
/// <param name="Tier">The pricing tier of the plan.</param>
/// <param name="MonthlyPriceUsd">The monthly price in US dollars.</param>
/// <param name="Limits">The usage limits and feature flags for this plan.</param>
public sealed record PlanDefinitionResponse(
    Guid Id,
    string Name,
    string Tier,
    decimal MonthlyPriceUsd,
    PlanLimitsResponse Limits);
