using Muntada.Rooms.Domain.Occurrence;
using Muntada.Rooms.Domain.Template;

namespace Muntada.Rooms.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the <see cref="RoomOccurrence"/> state machine transitions.
/// Constitution IV: TDD mandatory for state machine transitions.
/// </summary>
public class RoomOccurrenceStateMachineTests
{
    private static RoomOccurrence CreateScheduledRoom()
    {
        var settings = RoomSettings.Create(50);
        var room = RoomOccurrence.CreateStandalone(
            "tnt_test", "Test Room", DateTimeOffset.UtcNow.AddHours(1),
            "Asia/Riyadh", settings, "usr_creator");
        room.AssignModeratorAndSchedule("usr_moderator");
        return room;
    }

    private static RoomOccurrence CreateLiveRoom()
    {
        var room = CreateScheduledRoom();
        room.GoLive();
        return room;
    }

    // ─── Valid transitions ───

    [Fact]
    public void Draft_to_Scheduled_succeeds_when_moderator_assigned()
    {
        var settings = RoomSettings.Create(50);
        var room = RoomOccurrence.CreateStandalone(
            "tnt_test", "Test Room", DateTimeOffset.UtcNow.AddHours(1),
            "Asia/Riyadh", settings, "usr_creator");

        room.Status.Should().Be(RoomOccurrenceStatus.Draft);

        room.AssignModeratorAndSchedule("usr_moderator");

        room.Status.Should().Be(RoomOccurrenceStatus.Scheduled);
        room.ModeratorAssignment.Should().NotBeNull();
        room.ModeratorAssignment!.UserId.Should().Be("usr_moderator");
    }

    [Fact]
    public void Scheduled_to_Live_succeeds_on_first_participant()
    {
        var room = CreateScheduledRoom();

        room.GoLive();

        room.Status.Should().Be(RoomOccurrenceStatus.Live);
        room.LiveStartedAt.Should().NotBeNull();
    }

    [Fact]
    public void Live_to_Grace_succeeds_on_moderator_disconnect()
    {
        var room = CreateLiveRoom();

        room.StartGracePeriod();

        room.Status.Should().Be(RoomOccurrenceStatus.Grace);
        room.GraceStartedAt.Should().NotBeNull();
        room.ModeratorAssignment!.DisconnectedAt.Should().NotBeNull();
    }

    [Fact]
    public void Grace_to_Live_succeeds_on_moderator_reconnect()
    {
        var room = CreateLiveRoom();
        room.StartGracePeriod();

        room.ModeratorReconnects();

        room.Status.Should().Be(RoomOccurrenceStatus.Live);
        room.GraceStartedAt.Should().BeNull();
        room.ModeratorAssignment!.DisconnectedAt.Should().BeNull();
    }

    [Fact]
    public void Grace_to_Ended_succeeds_on_timeout()
    {
        var room = CreateLiveRoom();
        room.StartGracePeriod();

        room.End();

        room.Status.Should().Be(RoomOccurrenceStatus.Ended);
        room.LiveEndedAt.Should().NotBeNull();
    }

    [Fact]
    public void Live_to_Ended_succeeds_on_explicit_end()
    {
        var room = CreateLiveRoom();

        room.End();

        room.Status.Should().Be(RoomOccurrenceStatus.Ended);
        room.LiveEndedAt.Should().NotBeNull();
    }

    [Fact]
    public void Ended_to_Archived_succeeds_on_retention_expiry()
    {
        var room = CreateLiveRoom();
        room.End();

        room.Archive();

        room.Status.Should().Be(RoomOccurrenceStatus.Archived);
    }

    [Fact]
    public void Handover_during_Grace_returns_to_Live()
    {
        var room = CreateLiveRoom();
        room.StartGracePeriod();

        room.HandoverModerator("usr_new_mod");

        room.Status.Should().Be(RoomOccurrenceStatus.Live);
        room.ModeratorAssignment!.UserId.Should().Be("usr_new_mod");
        room.GraceStartedAt.Should().BeNull();
    }

    // ─── Invalid transitions ───

    [Fact]
    public void Draft_to_Live_throws_InvalidOperationException()
    {
        var settings = RoomSettings.Create(50);
        var room = RoomOccurrence.CreateStandalone(
            "tnt_test", "Test Room", DateTimeOffset.UtcNow.AddHours(1),
            "Asia/Riyadh", settings, "usr_creator");

        var act = () => room.GoLive();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Scheduled_to_Grace_throws_InvalidOperationException()
    {
        var room = CreateScheduledRoom();

        var act = () => room.StartGracePeriod();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Scheduled_to_Ended_throws_InvalidOperationException()
    {
        var room = CreateScheduledRoom();

        var act = () => room.End();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ended_to_Live_throws_InvalidOperationException()
    {
        var room = CreateLiveRoom();
        room.End();

        var act = () => room.GoLive();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Archived_to_any_throws_InvalidOperationException()
    {
        var room = CreateLiveRoom();
        room.End();
        room.Archive();

        var actLive = () => room.GoLive();
        var actEnd = () => room.End();

        actLive.Should().Throw<InvalidOperationException>();
        actEnd.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Handover_when_not_in_Grace_throws()
    {
        var room = CreateLiveRoom();

        var act = () => room.HandoverModerator("usr_new");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Grace period*");
    }

    [Fact]
    public void ChangeModerator_when_Live_throws()
    {
        var room = CreateLiveRoom();

        var act = () => room.ChangeModerator("usr_new");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft or Scheduled*");
    }

    // ─── Domain events ───

    [Fact]
    public void GoLive_raises_domain_event()
    {
        var room = CreateScheduledRoom();

        room.GoLive();

        room.DomainEvents.Should().ContainSingle(e =>
            e is Muntada.Rooms.Domain.Events.RoomStatusChangedDomainEvent);
    }

    [Fact]
    public void StartGracePeriod_raises_domain_event()
    {
        var room = CreateLiveRoom();

        room.StartGracePeriod();

        room.DomainEvents.Should().HaveCount(2); // GoLive + StartGrace
    }
}
