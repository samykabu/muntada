using Muntada.Rooms.Domain.Template;
using Muntada.SharedKernel.Domain.Exceptions;

namespace Muntada.Rooms.Tests.Unit.Domain;

/// <summary>
/// Unit tests for <see cref="RoomTemplate"/> domain logic.
/// </summary>
public class RoomTemplateTests
{
    [Fact]
    public void Create_with_valid_data_succeeds()
    {
        var settings = RoomSettings.Create(50, allowGuestAccess: true, allowRecording: true);

        var template = RoomTemplate.Create("tnt_test", "Weekly Standup", "Team standup", settings, "usr_admin");

        template.Should().NotBeNull();
        template.Id.Should().NotBeNull();
        template.Name.Should().Be("Weekly Standup");
        template.Description.Should().Be("Team standup");
        template.TenantId.Should().Be("tnt_test");
        template.Settings.MaxParticipants.Should().Be(50);
    }

    [Fact]
    public void Create_with_short_name_throws()
    {
        var settings = RoomSettings.Create(50);

        var act = () => RoomTemplate.Create("tnt_test", "AB", null, settings, "usr_admin");

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Create_with_long_name_throws()
    {
        var settings = RoomSettings.Create(50);
        var longName = new string('A', 101);

        var act = () => RoomTemplate.Create("tnt_test", longName, null, settings, "usr_admin");

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Create_with_long_description_throws()
    {
        var settings = RoomSettings.Create(50);
        var longDesc = new string('A', 501);

        var act = () => RoomTemplate.Create("tnt_test", "Valid Name", longDesc, settings, "usr_admin");

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Update_changes_description_and_settings()
    {
        var settings = RoomSettings.Create(50);
        var template = RoomTemplate.Create("tnt_test", "Template", null, settings, "usr_admin");
        var newSettings = RoomSettings.Create(100, allowGuestAccess: false);

        template.Update("Updated description", newSettings);

        template.Description.Should().Be("Updated description");
        template.Settings.MaxParticipants.Should().Be(100);
        template.Settings.AllowGuestAccess.Should().BeFalse();
    }

    [Fact]
    public void Name_is_immutable_after_creation()
    {
        var settings = RoomSettings.Create(50);
        var template = RoomTemplate.Create("tnt_test", "Original", null, settings, "usr_admin");

        // Name property has private setter — cannot be changed after creation
        // Update method only changes Description and Settings
        template.Update("New desc", settings);

        template.Name.Should().Be("Original");
    }

    [Fact]
    public void RoomSettings_rejects_invalid_max_participants()
    {
        var actZero = () => RoomSettings.Create(0);
        var actNegative = () => RoomSettings.Create(-1);
        var actTooHigh = () => RoomSettings.Create(10_001);

        actZero.Should().Throw<ValidationException>();
        actNegative.Should().Throw<ValidationException>();
        actTooHigh.Should().Throw<ValidationException>();
    }

    [Fact]
    public void RoomSettings_rejects_transcription_without_recording()
    {
        var act = () => RoomSettings.Create(50, allowRecording: false, allowTranscription: true);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void RoomSettings_auto_disables_autostart_when_recording_disabled()
    {
        var settings = RoomSettings.Create(50, allowRecording: false, autoStartRecording: true);

        settings.AutoStartRecording.Should().BeFalse();
    }
}
