import type { RoomOccurrenceStatus } from '../api/roomsApi';

/** Color mapping for each room lifecycle status. */
const STATUS_COLORS: Record<RoomOccurrenceStatus, { bg: string; text: string }> = {
  Draft: { bg: '#9ca3af', text: '#fff' },
  Scheduled: { bg: '#3b82f6', text: '#fff' },
  Live: { bg: '#22c55e', text: '#fff' },
  Grace: { bg: '#f59e0b', text: '#000' },
  Ended: { bg: '#6b7280', text: '#fff' },
  Archived: { bg: '#374151', text: '#9ca3af' },
};

interface RoomStatusBadgeProps {
  status: RoomOccurrenceStatus;
}

/** Badge component showing room lifecycle status with color coding. */
export function RoomStatusBadge({ status }: RoomStatusBadgeProps) {
  const colors = STATUS_COLORS[status];

  return (
    <span
      style={{
        display: 'inline-block',
        padding: '2px 10px',
        borderRadius: 12,
        fontSize: '0.75rem',
        fontWeight: 600,
        color: colors.text,
        backgroundColor: colors.bg,
        textTransform: 'uppercase',
        letterSpacing: '0.05em',
      }}
      aria-label={`Room status: ${status}`}
    >
      {status}
    </span>
  );
}
