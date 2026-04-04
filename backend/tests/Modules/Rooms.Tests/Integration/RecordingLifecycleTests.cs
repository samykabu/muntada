using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Muntada.Rooms.Application.Commands;
using Muntada.Rooms.Application.Services;
using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Recording;
using Muntada.Rooms.Infrastructure;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Tests.Integration;

/// <summary>
/// Integration tests for the recording start/stop flow
/// via command handlers with InMemory database and mocked IRecordingService.
/// </summary>
public class RecordingLifecycleTests : IDisposable
{
    private readonly RoomsDbContext _db;
    private readonly Mock<IRecordingService> _recordingServiceMock;
    private const string TenantId = "tenant-recording";
    private const string UserId = "user-moderator-1";

    /// <summary>
    /// Initializes a new instance with an InMemory database and mocked recording service.
    /// </summary>
    public RecordingLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<RoomsDbContext>()
            .UseInMemoryDatabase(databaseName: $"RecordingLifecycle_{Guid.NewGuid()}")
            .Options;

        _db = new RoomsDbContext(options);

        _recordingServiceMock = new Mock<IRecordingService>();
        _recordingServiceMock
            .Setup(s => s.StartRecordingAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("egress-test-id-123");
        _recordingServiceMock
            .Setup(s => s.StopRecordingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Helper: creates a standalone occurrence and transitions it to Live.
    /// </summary>
    private async Task<string> CreateLiveOccurrenceAsync(bool allowRecording = true)
    {
        var occurrenceHandler = new CreateRoomOccurrenceCommandHandler(_db);
        var occurrence = await occurrenceHandler.Handle(new CreateRoomOccurrenceCommand(
            TenantId,
            "Recording Test Room",
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

        var occurrenceId = occurrence.Id.Value;

        // Transition to Live
        var transitionHandler = new TransitionRoomStatusCommandHandler(_db, NullLogger<TransitionRoomStatusCommandHandler>.Instance);
        await transitionHandler.Handle(new TransitionRoomStatusCommand(
            TenantId, occurrenceId, RoomTrigger.FirstParticipantJoins), CancellationToken.None);

        return occurrenceId;
    }

    /// <summary>
    /// Helper: creates a standalone occurrence that stays in Scheduled status.
    /// </summary>
    private async Task<string> CreateScheduledOccurrenceAsync(bool allowRecording = true)
    {
        var occurrenceHandler = new CreateRoomOccurrenceCommandHandler(_db);
        var occurrence = await occurrenceHandler.Handle(new CreateRoomOccurrenceCommand(
            TenantId,
            "Scheduled Recording Room",
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

        return occurrence.Id.Value;
    }

    [Fact]
    public async Task StartRecording_OnLiveRoom_CreatesProcessingRecording()
    {
        // Arrange
        var occurrenceId = await CreateLiveOccurrenceAsync();
        var handler = new StartRecordingCommandHandler(_db, _recordingServiceMock.Object, NullLogger<StartRecordingCommandHandler>.Instance);

        // Act
        var recording = await handler.Handle(new StartRecordingCommand(
            occurrenceId, TenantId), CancellationToken.None);

        // Assert
        recording.Should().NotBeNull();
        recording.Status.Should().Be(RecordingStatus.Processing);
        recording.TenantId.Should().Be(TenantId);
        recording.RoomOccurrenceId.Value.Should().Be(occurrenceId);
        recording.LiveKitEgressId.Should().Be("egress-test-id-123");
        recording.S3Path.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StopRecording_MarksReady()
    {
        // Arrange — start a recording first
        var occurrenceId = await CreateLiveOccurrenceAsync();
        var startHandler = new StartRecordingCommandHandler(_db, _recordingServiceMock.Object, NullLogger<StartRecordingCommandHandler>.Instance);
        await startHandler.Handle(new StartRecordingCommand(
            occurrenceId, TenantId), CancellationToken.None);

        var stopHandler = new StopRecordingCommandHandler(_db, _recordingServiceMock.Object);

        // Act
        var recording = await stopHandler.Handle(new StopRecordingCommand(
            occurrenceId,
            TenantId,
            FileSizeBytes: 1024000,
            DurationSeconds: 3600), CancellationToken.None);

        // Assert
        recording.Status.Should().Be(RecordingStatus.Ready);
        recording.FileSizeBytes.Should().Be(1024000);
        recording.DurationSeconds.Should().Be(3600);
    }

    [Fact]
    public async Task StartRecording_OnNonLiveRoom_Throws()
    {
        // Arrange — occurrence stays in Scheduled status
        var occurrenceId = await CreateScheduledOccurrenceAsync();
        var handler = new StartRecordingCommandHandler(_db, _recordingServiceMock.Object, NullLogger<StartRecordingCommandHandler>.Instance);

        // Act
        var act = () => handler.Handle(new StartRecordingCommand(
            occurrenceId, TenantId), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("Room must be Live"));
    }

    [Fact]
    public async Task StartRecording_WhenRecordingDisabled_Throws()
    {
        // Arrange — create room with AllowRecording=false, then go Live
        var occurrenceId = await CreateLiveOccurrenceAsync(allowRecording: false);
        var handler = new StartRecordingCommandHandler(_db, _recordingServiceMock.Object, NullLogger<StartRecordingCommandHandler>.Instance);

        // Act
        var act = () => handler.Handle(new StartRecordingCommand(
            occurrenceId, TenantId), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("Recording is not enabled"));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
