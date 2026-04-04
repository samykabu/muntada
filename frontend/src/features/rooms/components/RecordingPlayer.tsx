import { useState } from 'react';
import type { RecordingResponse, Transcript } from '../api/roomsApi';

interface RecordingPlayerProps {
  /** The recording to play. */
  recording: RecordingResponse;
}

/** Formats seconds into HH:MM:SS or MM:SS. */
function formatDuration(totalSeconds: number): string {
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  const mm = String(m).padStart(2, '0');
  const ss = String(s).padStart(2, '0');
  return h > 0 ? `${h}:${mm}:${ss}` : `${mm}:${ss}`;
}

/** Formats bytes into a human-readable string. */
function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

const TRANSCRIPT_STATUS_LABELS: Record<string, string> = {
  Processing: 'Processing...',
  Ready: 'Ready',
  Failed: 'Failed',
};

/** Audio/video player for recordings with transcript viewer. */
export function RecordingPlayer({ recording }: RecordingPlayerProps) {
  const [activeTranscript, setActiveTranscript] = useState<Transcript | null>(
    recording.transcripts.find((t) => t.status === 'Ready') ?? null,
  );

  const playbackUrl = recording.downloadUrl;

  return (
    <div>
      {/* Recording Info */}
      <div style={{ marginBottom: '1rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <span
            style={{
              display: 'inline-block',
              padding: '2px 8px',
              borderRadius: 12,
              fontSize: '0.75rem',
              fontWeight: 600,
              color: '#fff',
              backgroundColor:
                recording.status === 'Ready' ? '#22c55e' : recording.status === 'Processing' ? '#f59e0b' : '#ef4444',
            }}
          >
            {recording.status}
          </span>
          <span style={{ marginLeft: 12, fontSize: '0.875rem', color: '#6b7280' }}>
            {formatDuration(recording.durationSeconds)} &middot; {formatFileSize(recording.fileSizeBytes)}
          </span>
        </div>
        <span style={{ fontSize: '0.75rem', color: '#9ca3af' }}>
          Visibility: {recording.visibility}
        </span>
      </div>

      {/* Player */}
      {recording.status === 'Ready' && playbackUrl ? (
        <div style={{ marginBottom: '1rem' }}>
          {/* Use <audio> for simplicity; could be <video> if the recording has video tracks. */}
          <audio controls style={{ width: '100%' }} src={playbackUrl}>
            Your browser does not support the audio element.
          </audio>
        </div>
      ) : recording.status === 'Processing' ? (
        <p style={{ color: '#f59e0b', fontStyle: 'italic' }}>Recording is still being processed...</p>
      ) : recording.status === 'Ready' && !playbackUrl ? (
        <p style={{ color: '#6b7280' }}>Download URL not yet available.</p>
      ) : (
        <p style={{ color: '#ef4444' }}>Recording failed to process.</p>
      )}

      {/* Transcripts */}
      {recording.transcripts.length > 0 && (
        <div style={{ border: '1px solid #e5e7eb', borderRadius: 4, padding: '1rem' }}>
          <h4 style={{ margin: '0 0 0.5rem' }}>Transcripts</h4>

          {/* Transcript selector */}
          <div style={{ display: 'flex', gap: 8, marginBottom: '0.75rem', flexWrap: 'wrap' }}>
            {recording.transcripts.map((t) => (
              <button
                key={t.language}
                type="button"
                onClick={() => setActiveTranscript(t)}
                disabled={t.status !== 'Ready'}
                style={{
                  padding: '4px 12px',
                  borderRadius: 4,
                  border: activeTranscript?.language === t.language ? '2px solid #3b82f6' : '1px solid #d1d5db',
                  backgroundColor: activeTranscript?.language === t.language ? '#eff6ff' : '#fff',
                  cursor: t.status === 'Ready' ? 'pointer' : 'default',
                  opacity: t.status === 'Ready' ? 1 : 0.5,
                }}
              >
                {t.language.toUpperCase()} - {TRANSCRIPT_STATUS_LABELS[t.status] ?? t.status}
              </button>
            ))}
          </div>

          {/* Transcript viewer */}
          {activeTranscript && activeTranscript.status === 'Ready' && (
            <div style={{ backgroundColor: '#f9fafb', borderRadius: 4, padding: '0.75rem', maxHeight: 300, overflowY: 'auto' }}>
              <p style={{ fontSize: '0.8rem', color: '#6b7280', margin: '0 0 4px' }}>
                Language: {activeTranscript.language.toUpperCase()}
              </p>
              <p style={{ fontSize: '0.875rem', color: '#374151', margin: 0 }}>
                {activeTranscript.textDownloadUrl ? (
                  <a
                    href={activeTranscript.textDownloadUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    style={{ color: '#3b82f6' }}
                  >
                    View full transcript
                  </a>
                ) : (
                  <span style={{ color: '#9ca3af' }}>Transcript download URL not available.</span>
                )}
              </p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
