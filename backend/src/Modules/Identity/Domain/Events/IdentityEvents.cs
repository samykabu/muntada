using Muntada.SharedKernel.Domain;

namespace Muntada.Identity.Domain.Events;

/// <summary>
/// Raised when a new user registers in the system.
/// </summary>
/// <param name="EventId">Unique identifier for this event instance.</param>
/// <param name="OccurredAt">UTC timestamp when this event occurred.</param>
/// <param name="UserId">The unique identifier of the registered user.</param>
/// <param name="Email">The email address used for registration.</param>
/// <param name="AggregateId">The opaque identifier of the source aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
public sealed record UserRegisteredEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    string Email,
    string AggregateId,
    string AggregateType,
    int Version) : IIntegrationEvent
{
    /// <summary>
    /// Creates a new <see cref="UserRegisteredEvent"/>.
    /// </summary>
    /// <param name="userId">The unique identifier of the registered user.</param>
    /// <param name="email">The email address used for registration.</param>
    /// <returns>A new event instance with auto-generated metadata.</returns>
    public static UserRegisteredEvent Create(Guid userId, string email) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, email,
            userId.ToString(), nameof(User.User), Version: 1);
}

/// <summary>
/// Raised when a user verifies their email address.
/// </summary>
/// <param name="EventId">Unique identifier for this event instance.</param>
/// <param name="OccurredAt">UTC timestamp when this event occurred.</param>
/// <param name="UserId">The unique identifier of the user whose email was verified.</param>
/// <param name="AggregateId">The opaque identifier of the source aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
public sealed record UserEmailVerifiedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    string AggregateId,
    string AggregateType,
    int Version) : IIntegrationEvent
{
    /// <summary>
    /// Creates a new <see cref="UserEmailVerifiedEvent"/>.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose email was verified.</param>
    /// <returns>A new event instance with auto-generated metadata.</returns>
    public static UserEmailVerifiedEvent Create(Guid userId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId,
            userId.ToString(), nameof(User.User), Version: 1);
}

/// <summary>
/// Raised when a user successfully logs in and a session is created.
/// </summary>
/// <param name="EventId">Unique identifier for this event instance.</param>
/// <param name="OccurredAt">UTC timestamp when this event occurred.</param>
/// <param name="UserId">The unique identifier of the user who logged in.</param>
/// <param name="SessionId">The unique identifier of the created session.</param>
/// <param name="AggregateId">The opaque identifier of the source aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
public sealed record UserLoggedInEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    Guid SessionId,
    string AggregateId,
    string AggregateType,
    int Version) : IIntegrationEvent
{
    /// <summary>
    /// Creates a new <see cref="UserLoggedInEvent"/>.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who logged in.</param>
    /// <param name="sessionId">The unique identifier of the created session.</param>
    /// <returns>A new event instance with auto-generated metadata.</returns>
    public static UserLoggedInEvent Create(Guid userId, Guid sessionId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, sessionId,
            userId.ToString(), nameof(Session.Session), Version: 1);
}

/// <summary>
/// Raised when a user logs out of a session.
/// </summary>
/// <param name="EventId">Unique identifier for this event instance.</param>
/// <param name="OccurredAt">UTC timestamp when this event occurred.</param>
/// <param name="UserId">The unique identifier of the user who logged out.</param>
/// <param name="SessionId">The unique identifier of the terminated session.</param>
/// <param name="AggregateId">The opaque identifier of the source aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
public sealed record UserLoggedOutEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    Guid SessionId,
    string AggregateId,
    string AggregateType,
    int Version) : IIntegrationEvent
{
    /// <summary>
    /// Creates a new <see cref="UserLoggedOutEvent"/>.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who logged out.</param>
    /// <param name="sessionId">The unique identifier of the terminated session.</param>
    /// <returns>A new event instance with auto-generated metadata.</returns>
    public static UserLoggedOutEvent Create(Guid userId, Guid sessionId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, sessionId,
            userId.ToString(), nameof(Session.Session), Version: 1);
}

/// <summary>
/// Raised when a user changes their password.
/// </summary>
/// <param name="EventId">Unique identifier for this event instance.</param>
/// <param name="OccurredAt">UTC timestamp when this event occurred.</param>
/// <param name="UserId">The unique identifier of the user who changed their password.</param>
/// <param name="AggregateId">The opaque identifier of the source aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
public sealed record PasswordChangedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    string AggregateId,
    string AggregateType,
    int Version) : IIntegrationEvent
{
    /// <summary>
    /// Creates a new <see cref="PasswordChangedEvent"/>.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who changed their password.</param>
    /// <returns>A new event instance with auto-generated metadata.</returns>
    public static PasswordChangedEvent Create(Guid userId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId,
            userId.ToString(), nameof(User.User), Version: 1);
}

/// <summary>
/// Raised when a user session is explicitly revoked (e.g., by admin or user action).
/// </summary>
/// <param name="EventId">Unique identifier for this event instance.</param>
/// <param name="OccurredAt">UTC timestamp when this event occurred.</param>
/// <param name="UserId">The unique identifier of the user whose session was revoked.</param>
/// <param name="SessionId">The unique identifier of the revoked session.</param>
/// <param name="AggregateId">The opaque identifier of the source aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
public sealed record SessionRevokedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    Guid SessionId,
    string AggregateId,
    string AggregateType,
    int Version) : IIntegrationEvent
{
    /// <summary>
    /// Creates a new <see cref="SessionRevokedEvent"/>.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose session was revoked.</param>
    /// <param name="sessionId">The unique identifier of the revoked session.</param>
    /// <returns>A new event instance with auto-generated metadata.</returns>
    public static SessionRevokedEvent Create(Guid userId, Guid sessionId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, sessionId,
            sessionId.ToString(), nameof(Session.Session), Version: 1);
}

/// <summary>
/// Raised when a Personal Access Token is created.
/// </summary>
/// <param name="EventId">Unique identifier for this event instance.</param>
/// <param name="OccurredAt">UTC timestamp when this event occurred.</param>
/// <param name="UserId">The unique identifier of the user who created the PAT.</param>
/// <param name="PatId">The unique identifier of the created PAT.</param>
/// <param name="Scopes">The permission scopes granted to the PAT.</param>
/// <param name="AggregateId">The opaque identifier of the source aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
public sealed record PATCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    Guid PatId,
    List<string> Scopes,
    string AggregateId,
    string AggregateType,
    int Version) : IIntegrationEvent
{
    /// <summary>
    /// Creates a new <see cref="PATCreatedEvent"/>.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who created the PAT.</param>
    /// <param name="patId">The unique identifier of the created PAT.</param>
    /// <param name="scopes">The permission scopes granted to the PAT.</param>
    /// <returns>A new event instance with auto-generated metadata.</returns>
    public static PATCreatedEvent Create(Guid userId, Guid patId, List<string> scopes) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, patId, scopes,
            patId.ToString(), nameof(Pat.PersonalAccessToken), Version: 1);
}

/// <summary>
/// Raised when a Personal Access Token is revoked.
/// </summary>
/// <param name="EventId">Unique identifier for this event instance.</param>
/// <param name="OccurredAt">UTC timestamp when this event occurred.</param>
/// <param name="UserId">The unique identifier of the user who revoked the PAT.</param>
/// <param name="PatId">The unique identifier of the revoked PAT.</param>
/// <param name="AggregateId">The opaque identifier of the source aggregate.</param>
/// <param name="AggregateType">The type name of the source aggregate.</param>
/// <param name="Version">The schema version of this event.</param>
public sealed record PATRevokedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    Guid PatId,
    string AggregateId,
    string AggregateType,
    int Version) : IIntegrationEvent
{
    /// <summary>
    /// Creates a new <see cref="PATRevokedEvent"/>.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who revoked the PAT.</param>
    /// <param name="patId">The unique identifier of the revoked PAT.</param>
    /// <returns>A new event instance with auto-generated metadata.</returns>
    public static PATRevokedEvent Create(Guid userId, Guid patId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, patId,
            patId.ToString(), nameof(Pat.PersonalAccessToken), Version: 1);
}
