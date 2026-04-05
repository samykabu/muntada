using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Muntada.Rooms.Application.Commands;
using Muntada.Rooms.Domain.Invite;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Tests.Integration;

/// <summary>
/// Integration tests for the invite creation, join, and revoke flow
/// via command handlers with InMemory database.
/// </summary>
public class InviteFlowTests : IDisposable
{
    private readonly RoomsDbContext _db;
    private const string TenantId = "tenant-invite";
    private const string UserId = "user-moderator-1";

    /// <summary>
    /// Initializes a new instance with an InMemory database.
    /// </summary>
    public InviteFlowTests()
    {
        var options = new DbContextOptionsBuilder<RoomsDbContext>()
            .UseInMemoryDatabase(databaseName: $"InviteFlow_{Guid.NewGuid()}")
            .Options;

        _db = new RoomsDbContext(options);
    }

    /// <summary>
    /// Helper: creates a standalone scheduled occurrence.
    /// </summary>
    private async Task<string> CreateScheduledOccurrenceAsync()
    {
        var handler = new CreateRoomOccurrenceCommandHandler(_db);
        var occurrence = await handler.Handle(new CreateRoomOccurrenceCommand(
            TenantId,
            "Invite Test Room",
            DateTimeOffset.UtcNow.AddHours(1),
            "Asia/Riyadh",
            ModeratorUserId: UserId,
            MaxParticipants: 50,
            AllowGuestAccess: true,
            AllowRecording: true,
            AllowTranscription: false,
            DefaultTranscriptionLanguage: null,
            AutoStartRecording: false,
            GracePeriodSeconds: 300,
            CreatedBy: UserId), CancellationToken.None);

        return occurrence.Id.Value;
    }

    /// <summary>
    /// Helper: transitions a scheduled occurrence to Live.
    /// </summary>
    private async Task TransitionToLiveAsync(string occurrenceId)
    {
        var handler = new TransitionRoomStatusCommandHandler(_db, NullLogger<TransitionRoomStatusCommandHandler>.Instance);
        await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.FirstParticipantJoins), CancellationToken.None);
    }

    [Fact]
    public async Task GenerateEmailInvite_CreatesInviteWithToken()
    {
        // Arrange
        var occurrenceId = await CreateScheduledOccurrenceAsync();
        var handler = new GenerateRoomInviteCommandHandler(_db, NullLogger<GenerateRoomInviteCommandHandler>.Instance);

        // Act
        var invites = await handler.Handle(new GenerateRoomInviteCommand(
            occurrenceId,
            TenantId,
            new List<InviteRequest>
            {
                new("test@example.com", null, RoomInviteType.Email)
            },
            InvitedBy: UserId), CancellationToken.None);

        // Assert
        invites.Should().HaveCount(1);
        var invite = invites[0];
        invite.InviteToken.Should().NotBeNullOrEmpty();
        invite.Status.Should().Be(RoomInviteStatus.Pending);
        invite.InviteType.Should().Be(RoomInviteType.Email);
        invite.InvitedEmail.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GenerateGuestMagicLink_CreatesGuestInvite()
    {
        // Arrange
        var occurrenceId = await CreateScheduledOccurrenceAsync();
        var handler = new GenerateRoomInviteCommandHandler(_db, NullLogger<GenerateRoomInviteCommandHandler>.Instance);

        // Act
        var invites = await handler.Handle(new GenerateRoomInviteCommand(
            occurrenceId,
            TenantId,
            new List<InviteRequest>
            {
                new(null, null, RoomInviteType.GuestMagicLink)
            },
            InvitedBy: UserId), CancellationToken.None);

        // Assert
        invites.Should().HaveCount(1);
        var invite = invites[0];
        invite.InviteToken.Should().NotBeNullOrEmpty();
        invite.Status.Should().Be(RoomInviteStatus.Pending);
        invite.InviteType.Should().Be(RoomInviteType.GuestMagicLink);
    }

    [Fact]
    public async Task JoinRoom_WithValidToken_CreatesParticipant()
    {
        // Arrange — create occurrence, transition to Live, generate invite
        var occurrenceId = await CreateScheduledOccurrenceAsync();
        await TransitionToLiveAsync(occurrenceId);

        var inviteHandler = new GenerateRoomInviteCommandHandler(_db, NullLogger<GenerateRoomInviteCommandHandler>.Instance);
        var invites = await inviteHandler.Handle(new GenerateRoomInviteCommand(
            occurrenceId,
            TenantId,
            new List<InviteRequest>
            {
                new("joiner@example.com", "user-joiner", RoomInviteType.Email)
            },
            InvitedBy: UserId), CancellationToken.None);

        var token = invites[0].InviteToken;

        // Act
        var joinHandler = new JoinRoomCommandHandler(_db, NullLogger<JoinRoomCommandHandler>.Instance);
        var result = await joinHandler.Handle(new JoinRoomCommand(
            occurrenceId,
            token,
            UserId: "user-joiner",
            DisplayName: "Test Joiner"), CancellationToken.None);

        // Assert
        result.ParticipantState.Should().NotBeNull();
        result.ParticipantState.DisplayName.Should().Be("Test Joiner");
        result.ParticipantState.UserId.Should().Be("user-joiner");
        result.Occurrence.Id.Value.Should().Be(occurrenceId);

        // Verify invite is now Accepted
        var updatedInvite = await _db.RoomInvites
            .FirstAsync(i => i.InviteToken == token);
        updatedInvite.Status.Should().Be(RoomInviteStatus.Accepted);
    }

    [Fact]
    public async Task JoinRoom_WithExpiredToken_Throws()
    {
        // Arrange — create occurrence, transition to Live, generate invite
        var occurrenceId = await CreateScheduledOccurrenceAsync();
        await TransitionToLiveAsync(occurrenceId);

        var inviteHandler = new GenerateRoomInviteCommandHandler(_db, NullLogger<GenerateRoomInviteCommandHandler>.Instance);
        var invites = await inviteHandler.Handle(new GenerateRoomInviteCommand(
            occurrenceId,
            TenantId,
            new List<InviteRequest>
            {
                new("expired@example.com", null, RoomInviteType.Email)
            },
            InvitedBy: UserId), CancellationToken.None);

        var invite = invites[0];

        // Manually expire the invite using reflection (ExpiresAt is private set)
        var expiresAtProp = typeof(RoomInvite).GetProperty("ExpiresAt");
        expiresAtProp!.SetValue(invite, DateTimeOffset.UtcNow.AddDays(-1));
        await _db.SaveChangesAsync();

        // Act
        var joinHandler = new JoinRoomCommandHandler(_db, NullLogger<JoinRoomCommandHandler>.Instance);
        var act = () => joinHandler.Handle(new JoinRoomCommand(
            occurrenceId,
            invite.InviteToken,
            UserId: "user-expired",
            DisplayName: "Expired User"), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("Invalid or expired"));
    }

    [Fact]
    public async Task RevokeInvite_InvalidatesToken()
    {
        // Arrange
        var occurrenceId = await CreateScheduledOccurrenceAsync();
        var inviteHandler = new GenerateRoomInviteCommandHandler(_db, NullLogger<GenerateRoomInviteCommandHandler>.Instance);
        var invites = await inviteHandler.Handle(new GenerateRoomInviteCommand(
            occurrenceId,
            TenantId,
            new List<InviteRequest>
            {
                new("revoke@example.com", null, RoomInviteType.Email)
            },
            InvitedBy: UserId), CancellationToken.None);

        var inviteId = invites[0].Id.Value;

        // Act
        var revokeHandler = new RevokeRoomInviteCommandHandler(_db);
        await revokeHandler.Handle(new RevokeRoomInviteCommand(
            inviteId, TenantId), CancellationToken.None);

        // Assert
        var revokedInvite = await _db.RoomInvites
            .FirstAsync(i => i.Id == new RoomInviteId(inviteId));
        revokedInvite.Status.Should().Be(RoomInviteStatus.Revoked);
    }

    [Fact]
    public async Task JoinRoom_WhenRoomEnded_Throws()
    {
        // Arrange — create occurrence, go live, then end it
        var occurrenceId = await CreateScheduledOccurrenceAsync();
        await TransitionToLiveAsync(occurrenceId);

        // Generate invite while room is Live
        var inviteHandler = new GenerateRoomInviteCommandHandler(_db, NullLogger<GenerateRoomInviteCommandHandler>.Instance);
        var invites = await inviteHandler.Handle(new GenerateRoomInviteCommand(
            occurrenceId,
            TenantId,
            new List<InviteRequest>
            {
                new("late@example.com", null, RoomInviteType.Email)
            },
            InvitedBy: UserId), CancellationToken.None);

        var token = invites[0].InviteToken;

        // End the room
        var transitionHandler = new TransitionRoomStatusCommandHandler(_db, NullLogger<TransitionRoomStatusCommandHandler>.Instance);
        await transitionHandler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.EndRoom), CancellationToken.None);

        // Act
        var joinHandler = new JoinRoomCommandHandler(_db, NullLogger<JoinRoomCommandHandler>.Instance);
        var act = () => joinHandler.Handle(new JoinRoomCommand(
            occurrenceId,
            token,
            UserId: "user-late",
            DisplayName: "Late User"), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("Cannot join room"));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
