using Muntada.SharedKernel.Domain;

namespace Muntada.Rooms.Domain.Template;

/// <summary>
/// Value object representing the configurable settings for a room.
/// Shared between <see cref="RoomTemplate"/> and room occurrences.
/// </summary>
public sealed class RoomSettings : ValueObject
{
    /// <summary>
    /// Gets the maximum number of participants allowed in the room.
    /// Must be between 1 and 10,000 and within the tenant's plan limit.
    /// </summary>
    public int MaxParticipants { get; private set; }

    /// <summary>
    /// Gets whether unauthenticated guests can join via magic link with listen-only permissions.
    /// </summary>
    public bool AllowGuestAccess { get; private set; }

    /// <summary>
    /// Gets whether recording is enabled for the room.
    /// Must be allowed by the tenant's plan.
    /// </summary>
    public bool AllowRecording { get; private set; }

    /// <summary>
    /// Gets whether transcription is enabled for the room.
    /// Requires <see cref="AllowRecording"/> to be true.
    /// </summary>
    public bool AllowTranscription { get; private set; }

    /// <summary>
    /// Gets the ISO 639-1 language code for transcription (e.g., "en", "ar").
    /// Null if transcription is disabled.
    /// </summary>
    public string? DefaultTranscriptionLanguage { get; private set; }

    /// <summary>
    /// Gets whether recording should start automatically when the room goes live.
    /// </summary>
    public bool AutoStartRecording { get; private set; }

    private RoomSettings() { }

    /// <summary>
    /// Creates a new <see cref="RoomSettings"/> value object with the specified configuration.
    /// </summary>
    /// <param name="maxParticipants">Maximum participants (1-10,000).</param>
    /// <param name="allowGuestAccess">Whether guest access via magic link is allowed.</param>
    /// <param name="allowRecording">Whether recording is enabled.</param>
    /// <param name="allowTranscription">Whether transcription is enabled.</param>
    /// <param name="defaultTranscriptionLanguage">ISO 639-1 language code for transcription.</param>
    /// <param name="autoStartRecording">Whether recording auto-starts on room live.</param>
    /// <returns>A new <see cref="RoomSettings"/> instance.</returns>
    public static RoomSettings Create(
        int maxParticipants,
        bool allowGuestAccess = true,
        bool allowRecording = true,
        bool allowTranscription = false,
        string? defaultTranscriptionLanguage = null,
        bool autoStartRecording = false)
    {
        if (maxParticipants < 1 || maxParticipants > 10_000)
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                nameof(MaxParticipants), "MaxParticipants must be between 1 and 10,000.");

        if (allowTranscription && !allowRecording)
            throw new SharedKernel.Domain.Exceptions.ValidationException(
                nameof(AllowTranscription), "Transcription requires recording to be enabled.");

        return new RoomSettings
        {
            MaxParticipants = maxParticipants,
            AllowGuestAccess = allowGuestAccess,
            AllowRecording = allowRecording,
            AllowTranscription = allowTranscription,
            DefaultTranscriptionLanguage = allowTranscription ? defaultTranscriptionLanguage : null,
            AutoStartRecording = autoStartRecording && allowRecording
        };
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxParticipants;
        yield return AllowGuestAccess;
        yield return AllowRecording;
        yield return AllowTranscription;
        yield return DefaultTranscriptionLanguage;
        yield return AutoStartRecording;
    }
}
