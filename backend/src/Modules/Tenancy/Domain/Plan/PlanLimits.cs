using Muntada.SharedKernel.Domain;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Tenancy.Domain.Plan;

/// <summary>
/// Value object representing the usage limits and feature flags for a subscription plan.
/// Encapsulates all configurable quotas that constrain tenant resource consumption.
/// </summary>
public sealed class PlanLimits : ValueObject
{
    /// <summary>
    /// Gets the maximum number of rooms a tenant can create per month.
    /// A value of <c>0</c> indicates unlimited rooms.
    /// </summary>
    public int MaxRoomsPerMonth { get; }

    /// <summary>
    /// Gets the maximum number of participants allowed in a single room. Must be at least 1.
    /// </summary>
    public int MaxParticipantsPerRoom { get; }

    /// <summary>
    /// Gets the maximum storage in gigabytes allocated to the tenant. Must be at least 0.
    /// </summary>
    public int MaxStorageGB { get; }

    /// <summary>
    /// Gets the maximum number of recording hours allowed per month. Must be at least 0.
    /// </summary>
    public int MaxRecordingHoursPerMonth { get; }

    /// <summary>
    /// Gets the maximum number of days data is retained. Must be at least 1.
    /// </summary>
    public int MaxDataRetentionDays { get; }

    /// <summary>
    /// Gets a value indicating whether the plan allows room recording.
    /// </summary>
    public bool AllowRecording { get; }

    /// <summary>
    /// Gets a value indicating whether the plan allows guest (unauthenticated) access to rooms.
    /// </summary>
    public bool AllowGuestAccess { get; }

    /// <summary>
    /// Gets a value indicating whether the plan allows custom branding (logo, colors, domain).
    /// </summary>
    public bool AllowCustomBranding { get; }

    private PlanLimits(
        int maxRoomsPerMonth,
        int maxParticipantsPerRoom,
        int maxStorageGB,
        int maxRecordingHoursPerMonth,
        int maxDataRetentionDays,
        bool allowRecording,
        bool allowGuestAccess,
        bool allowCustomBranding)
    {
        MaxRoomsPerMonth = maxRoomsPerMonth;
        MaxParticipantsPerRoom = maxParticipantsPerRoom;
        MaxStorageGB = maxStorageGB;
        MaxRecordingHoursPerMonth = maxRecordingHoursPerMonth;
        MaxDataRetentionDays = maxDataRetentionDays;
        AllowRecording = allowRecording;
        AllowGuestAccess = allowGuestAccess;
        AllowCustomBranding = allowCustomBranding;
    }

    /// <summary>
    /// Creates a new <see cref="PlanLimits"/> instance after validating all constraints.
    /// </summary>
    /// <param name="maxRoomsPerMonth">Maximum rooms per month (0 = unlimited).</param>
    /// <param name="maxParticipantsPerRoom">Maximum participants per room (>= 1).</param>
    /// <param name="maxStorageGB">Maximum storage in GB (>= 0).</param>
    /// <param name="maxRecordingHoursPerMonth">Maximum recording hours per month (>= 0).</param>
    /// <param name="maxDataRetentionDays">Maximum data retention in days (>= 1).</param>
    /// <param name="allowRecording">Whether recording is allowed.</param>
    /// <param name="allowGuestAccess">Whether guest access is allowed.</param>
    /// <param name="allowCustomBranding">Whether custom branding is allowed.</param>
    /// <returns>A valid <see cref="PlanLimits"/> instance.</returns>
    /// <exception cref="ValidationException">Thrown when any constraint is violated.</exception>
    public static PlanLimits Create(
        int maxRoomsPerMonth,
        int maxParticipantsPerRoom,
        int maxStorageGB,
        int maxRecordingHoursPerMonth,
        int maxDataRetentionDays,
        bool allowRecording,
        bool allowGuestAccess,
        bool allowCustomBranding)
    {
        if (maxRoomsPerMonth < 0)
            throw new ValidationException(nameof(MaxRoomsPerMonth),
                "Maximum rooms per month must be 0 (unlimited) or a positive number.");

        if (maxParticipantsPerRoom < 1)
            throw new ValidationException(nameof(MaxParticipantsPerRoom),
                "Maximum participants per room must be at least 1.");

        if (maxStorageGB < 0)
            throw new ValidationException(nameof(MaxStorageGB),
                "Maximum storage must be at least 0 GB.");

        if (maxRecordingHoursPerMonth < 0)
            throw new ValidationException(nameof(MaxRecordingHoursPerMonth),
                "Maximum recording hours per month must be at least 0.");

        if (maxDataRetentionDays < 1)
            throw new ValidationException(nameof(MaxDataRetentionDays),
                "Maximum data retention must be at least 1 day.");

        return new PlanLimits(
            maxRoomsPerMonth,
            maxParticipantsPerRoom,
            maxStorageGB,
            maxRecordingHoursPerMonth,
            maxDataRetentionDays,
            allowRecording,
            allowGuestAccess,
            allowCustomBranding);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxRoomsPerMonth;
        yield return MaxParticipantsPerRoom;
        yield return MaxStorageGB;
        yield return MaxRecordingHoursPerMonth;
        yield return MaxDataRetentionDays;
        yield return AllowRecording;
        yield return AllowGuestAccess;
        yield return AllowCustomBranding;
    }
}
