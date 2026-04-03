using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.GuestLink;

/// <summary>
/// Aggregate root for guest magic links. Token stored as SHA-256 hash.
/// Uses opaque ID prefix <c>lnk_</c>. Default 7-day expiry.
/// </summary>
public sealed class GuestMagicLink : AggregateRoot<Guid>
{
    /// <summary>Gets the room occurrence this link grants access to.</summary>
    public Guid RoomOccurrenceId { get; private set; }

    /// <summary>Gets the user who created this magic link.</summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>Gets the SHA-256 hash of the magic link token.</summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>Gets the current status of this magic link.</summary>
    public GuestMagicLinkStatus Status { get; private set; }

    /// <summary>Gets the UTC timestamp when this link expires.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Gets the number of times this link has been used.</summary>
    public int UsageCount { get; private set; }

    private GuestMagicLink() { } // EF Core

    /// <summary>
    /// Creates a new active guest magic link.
    /// </summary>
    /// <param name="roomOccurrenceId">The room occurrence this link grants access to.</param>
    /// <param name="createdByUserId">The user creating this link.</param>
    /// <param name="tokenHash">The SHA-256 hash of the generated token.</param>
    /// <param name="expiry">The duration before this link expires.</param>
    /// <returns>A new <see cref="GuestMagicLink"/> in <see cref="GuestMagicLinkStatus.Active"/> status.</returns>
    public static GuestMagicLink Create(Guid roomOccurrenceId, Guid createdByUserId, string tokenHash, TimeSpan expiry)
    {
        return new GuestMagicLink
        {
            Id = Guid.NewGuid(),
            RoomOccurrenceId = roomOccurrenceId,
            CreatedByUserId = createdByUserId,
            TokenHash = tokenHash,
            Status = GuestMagicLinkStatus.Active,
            UsageCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiry),
        };
    }

    /// <summary>
    /// Determines whether this magic link is still valid for use.
    /// A link is valid when its status is <see cref="GuestMagicLinkStatus.Active"/>
    /// and it has not expired.
    /// </summary>
    /// <returns><c>true</c> if the link can be used; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return Status == GuestMagicLinkStatus.Active
            && DateTimeOffset.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Records a usage of this magic link. Increments the usage count
    /// and updates the version. Only applies to active links.
    /// </summary>
    public void IncrementUsage()
    {
        if (Status != GuestMagicLinkStatus.Active) return;

        UsageCount++;
        IncrementVersion();
    }

    /// <summary>
    /// Revokes this magic link, preventing further use.
    /// Only transitions from <see cref="GuestMagicLinkStatus.Active"/> status.
    /// </summary>
    public void Revoke()
    {
        if (Status != GuestMagicLinkStatus.Active) return;

        Status = GuestMagicLinkStatus.Revoked;
        IncrementVersion();
    }
}

/// <summary>Lifecycle status of a guest magic link.</summary>
public enum GuestMagicLinkStatus
{
    /// <summary>Link is active and can be used.</summary>
    Active = 0,

    /// <summary>Link was explicitly revoked.</summary>
    Revoked = 1,

    /// <summary>Link expired due to timeout.</summary>
    Expired = 2
}
