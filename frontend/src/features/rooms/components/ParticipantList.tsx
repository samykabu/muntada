import type { RoomParticipantResponse, ParticipantRole, MediaState } from '../api/roomsApi';

interface ParticipantListProps {
  /** Current list of participants in the room. */
  participants: RoomParticipantResponse[];
  /** Whether the list is loading. */
  isLoading?: boolean;
}

const ROLE_COLORS: Record<ParticipantRole, string> = {
  Moderator: '#7c3aed',
  Member: '#3b82f6',
  Guest: '#9ca3af',
};

/** Returns a display label for audio/video state. */
function mediaIcon(state: MediaState): string {
  switch (state) {
    case 'Unmuted':
      return '🎤';
    case 'Muted':
      return '🔇';
    case 'On':
      return '📹';
    case 'Off':
      return '⬛';
    default:
      return '';
  }
}

/** Real-time participant list with audio/video indicators. */
export function ParticipantList({ participants, isLoading }: ParticipantListProps) {
  if (isLoading) return <p>Loading participants...</p>;

  const activeParticipants = participants.filter((p) => !p.leftAt);
  const inactiveParticipants = participants.filter((p) => p.leftAt);

  return (
    <div>
      <h3>Participants ({activeParticipants.length} active)</h3>

      {activeParticipants.length === 0 && (
        <p style={{ color: '#6b7280', fontStyle: 'italic' }}>No participants in the room yet.</p>
      )}

      <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
        {activeParticipants.map((p) => (
          <li
            key={p.id}
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              padding: '0.5rem 0',
              borderBottom: '1px solid #f3f4f6',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <span
                style={{
                  display: 'inline-block',
                  width: 8,
                  height: 8,
                  borderRadius: '50%',
                  backgroundColor: '#22c55e',
                }}
                aria-label="Online"
              />
              <span style={{ fontWeight: 500 }}>{p.displayName}</span>
              <span
                style={{
                  display: 'inline-block',
                  padding: '1px 6px',
                  borderRadius: 8,
                  fontSize: '0.7rem',
                  color: '#fff',
                  backgroundColor: ROLE_COLORS[p.role],
                }}
              >
                {p.role}
              </span>
            </div>
            <div style={{ display: 'flex', gap: 8, fontSize: '1rem' }}>
              <span title={`Audio: ${p.audioState}`}>{mediaIcon(p.audioState)}</span>
              <span title={`Video: ${p.videoState}`}>{mediaIcon(p.videoState)}</span>
            </div>
          </li>
        ))}
      </ul>

      {inactiveParticipants.length > 0 && (
        <details style={{ marginTop: '1rem' }}>
          <summary style={{ cursor: 'pointer', color: '#6b7280', fontSize: '0.875rem' }}>
            {inactiveParticipants.length} participant(s) left
          </summary>
          <ul style={{ listStyle: 'none', padding: 0, margin: '0.5rem 0 0' }}>
            {inactiveParticipants.map((p) => (
              <li
                key={p.id}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 8,
                  padding: '0.25rem 0',
                  color: '#9ca3af',
                }}
              >
                <span
                  style={{
                    display: 'inline-block',
                    width: 8,
                    height: 8,
                    borderRadius: '50%',
                    backgroundColor: '#d1d5db',
                  }}
                  aria-label="Offline"
                />
                <span>{p.displayName}</span>
                <span style={{ fontSize: '0.75rem' }}>
                  (left {new Date(p.leftAt!).toLocaleTimeString()})
                </span>
              </li>
            ))}
          </ul>
        </details>
      )}
    </div>
  );
}
