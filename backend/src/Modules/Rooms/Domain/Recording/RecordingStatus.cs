namespace Muntada.Rooms.Domain.Recording;

/// <summary>
/// Represents the processing status of a <see cref="Recording"/>.
/// </summary>
public enum RecordingStatus
{
    /// <summary>Recording is being processed (upload, encoding).</summary>
    Processing,

    /// <summary>Recording is ready for download or streaming.</summary>
    Ready,

    /// <summary>Recording processing failed.</summary>
    Failed
}
