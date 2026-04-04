using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Muntada.Rooms.Application.Commands;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Tests.Integration;

/// <summary>
/// Integration tests for the full room lifecycle: create template, create occurrence,
/// assign moderator, and transition through all states via command handlers.
/// </summary>
public class RoomLifecycleTests : IDisposable
{
    private readonly RoomsDbContext _db;
    private const string TenantId = "tenant-lifecycle";
    private const string UserId = "user-moderator-1";

    /// <summary>
    /// Initializes a new instance with an InMemory database.
    /// </summary>
    public RoomLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<RoomsDbContext>()
            .UseInMemoryDatabase(databaseName: $"RoomLifecycle_{Guid.NewGuid()}")
            .Options;

        _db = new RoomsDbContext(options);
    }

    /// <summary>
    /// Helper: creates a template and a standalone scheduled occurrence.
    /// </summary>
    private async Task<(RoomOccurrence Occurrence, string OccurrenceId)> CreateScheduledOccurrenceAsync(
        bool allowRecording = true)
    {
        // Create template
        var templateHandler = new CreateRoomTemplateCommandHandler(_db, NullLogger<CreateRoomTemplateCommandHandler>.Instance);
        await templateHandler.Handle(new CreateRoomTemplateCommand(
            TenantId,
            $"Test Template {Guid.NewGuid():N}",
            "Integration test template",
            MaxParticipants: 50,
            AllowGuestAccess: true,
            AllowRecording: allowRecording,
            AllowTranscription: false,
            DefaultTranscriptionLanguage: null,
            AutoStartRecording: false,
            CreatedBy: UserId), CancellationToken.None);

        // Create occurrence (standalone with moderator => Scheduled)
        var occurrenceHandler = new CreateRoomOccurrenceCommandHandler(_db);
        var occurrence = await occurrenceHandler.Handle(new CreateRoomOccurrenceCommand(
            TenantId,
            "Test Room Session",
            DateTimeOffset.UtcNow.AddHours(1),
            "Asia/Riyadh",
            ModeratorUserId: UserId,
            MaxParticipants: 50,
            AllowGuestAccess: true,
            AllowRecording: allowRecording,
            AllowTranscription: false,
            DefaultTranscriptionLanguage: null,
            AutoStartRecording: false,
            GracePeriodSeconds: 300,
            CreatedBy: UserId), CancellationToken.None);

        return (occurrence, occurrence.Id.Value);
    }

    [Fact]
    public async Task CreateTemplate_ThenCreateOccurrence_ProducesScheduledRoom()
    {
        // Act
        var (occurrence, _) = await CreateScheduledOccurrenceAsync();

        // Assert
        occurrence.Should().NotBeNull();
        occurrence.Status.Should().Be(RoomOccurrenceStatus.Scheduled);
        occurrence.TenantId.Should().Be(TenantId);
        occurrence.ModeratorAssignment.Should().NotBeNull();
        occurrence.ModeratorAssignment!.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task ScheduledRoom_GoLive_SetsLiveStartedAt()
    {
        // Arrange
        var (_, occurrenceId) = await CreateScheduledOccurrenceAsync();
        var handler = new TransitionRoomStatusCommandHandler(_db, NullLogger<TransitionRoomStatusCommandHandler>.Instance);

        // Act
        var result = await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.FirstParticipantJoins), CancellationToken.None);

        // Assert
        result.Status.Should().Be(RoomOccurrenceStatus.Live);
        result.LiveStartedAt.Should().NotBeNull();
        result.LiveStartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LiveRoom_ModeratorDisconnects_TransitionsToGrace()
    {
        // Arrange
        var (_, occurrenceId) = await CreateScheduledOccurrenceAsync();
        var handler = new TransitionRoomStatusCommandHandler(_db, NullLogger<TransitionRoomStatusCommandHandler>.Instance);

        // Go Live first
        await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.FirstParticipantJoins), CancellationToken.None);

        // Act
        var result = await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.ModeratorDisconnects), CancellationToken.None);

        // Assert
        result.Status.Should().Be(RoomOccurrenceStatus.Grace);
        result.GraceStartedAt.Should().NotBeNull();
        result.GraceStartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GraceRoom_ModeratorReconnects_ReturnsToLive()
    {
        // Arrange
        var (_, occurrenceId) = await CreateScheduledOccurrenceAsync();
        var handler = new TransitionRoomStatusCommandHandler(_db, NullLogger<TransitionRoomStatusCommandHandler>.Instance);

        await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.FirstParticipantJoins), CancellationToken.None);
        await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.ModeratorDisconnects), CancellationToken.None);

        // Act
        var result = await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.ModeratorReconnects), CancellationToken.None);

        // Assert
        result.Status.Should().Be(RoomOccurrenceStatus.Live);
        result.GraceStartedAt.Should().BeNull();
    }

    [Fact]
    public async Task GraceRoom_Timeout_TransitionsToEnded()
    {
        // Arrange
        var (_, occurrenceId) = await CreateScheduledOccurrenceAsync();
        var handler = new TransitionRoomStatusCommandHandler(_db, NullLogger<TransitionRoomStatusCommandHandler>.Instance);

        await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.FirstParticipantJoins), CancellationToken.None);
        await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.ModeratorDisconnects), CancellationToken.None);

        // Act — EndRoom trigger from Grace state (grace timeout scenario)
        var result = await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.EndRoom), CancellationToken.None);

        // Assert
        result.Status.Should().Be(RoomOccurrenceStatus.Ended);
        result.LiveEndedAt.Should().NotBeNull();
        result.LiveEndedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EndedRoom_Archive_TransitionsToArchived()
    {
        // Arrange
        var (_, occurrenceId) = await CreateScheduledOccurrenceAsync();
        var handler = new TransitionRoomStatusCommandHandler(_db, NullLogger<TransitionRoomStatusCommandHandler>.Instance);

        await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.FirstParticipantJoins), CancellationToken.None);
        await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.EndRoom), CancellationToken.None);

        // Act
        var result = await handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.RetentionExpires), CancellationToken.None);

        // Assert
        result.Status.Should().Be(RoomOccurrenceStatus.Archived);
    }

    [Fact]
    public async Task InvalidTransition_ScheduledToEnded_Throws()
    {
        // Arrange
        var (_, occurrenceId) = await CreateScheduledOccurrenceAsync();
        var handler = new TransitionRoomStatusCommandHandler(_db, NullLogger<TransitionRoomStatusCommandHandler>.Instance);

        // Act — Scheduled cannot go directly to Ended
        var act = () => handler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.EndRoom), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("Cannot apply trigger"));
    }

    [Fact]
    public async Task UpdateOccurrence_CancelFlag_SetsCancelled()
    {
        // Arrange
        var (_, occurrenceId) = await CreateScheduledOccurrenceAsync();
        var handler = new UpdateRoomOccurrenceCommandHandler(_db);

        // Act
        var result = await handler.Handle(new UpdateRoomOccurrenceCommand(
            TenantId,
            occurrenceId,
            Title: null,
            MaxParticipants: null,
            AllowGuestAccess: null,
            AllowRecording: null,
            AllowTranscription: null,
            DefaultTranscriptionLanguage: null,
            AutoStartRecording: null,
            UpdateSettings: false,
            IsCancelled: true), CancellationToken.None);

        // Assert
        result.IsCancelled.Should().BeTrue();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
