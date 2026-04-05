namespace Muntada.Rooms.Api.Dtos;

/// <summary>
/// Response DTO for a room invite.
/// </summary>
public sealed record RoomInviteResponse(
    string Id,
    string RoomOccurrenceId,
    string? InvitedEmail,
    string? InvitedUserId,
    string InviteToken,
    string Status,
    string InviteType,
    string InvitedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Request DTO for creating multiple room invites in a single batch.
/// </summary>
public sealed record CreateRoomInvitesRequest(
    List<InviteItemRequest> Invites);

/// <summary>
/// A single invite item within a batch creation request.
/// </summary>
public sealed record InviteItemRequest(
    string? Email,
    string? UserId,
    string InviteType);

/// <summary>
/// Request DTO for joining a room via invite token.
/// </summary>
public sealed record JoinRoomRequest(
    string Token,
    string? UserId,
    string? DisplayName);

/// <summary>
/// Response DTO returned after successfully joining a room.
/// </summary>
public sealed record JoinRoomResponse(
    string ParticipantId,
    string OccurrenceId,
    string DisplayName,
    string Role,
    string Status);
