import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useGetOccurrenceQuery } from '../api/roomsApi';
import { RoomStatusBadge } from '../components/RoomStatusBadge';
import { ParticipantList } from '../components/ParticipantList';
import { InviteDialog } from '../components/InviteDialog';
import { useRoom } from '../hooks/useRoom';

/** Live room page with participant list, status, and moderator controls. */
export function LiveRoomPage() {
  const { tenantId, occurrenceId } = useParams<{ tenantId: string; occurrenceId: string }>();
  const [showInvites, setShowInvites] = useState(false);

  const { data: occurrence, isLoading, error } = useGetOccurrenceQuery(
    { tenantId: tenantId!, occurrenceId: occurrenceId! },
    { skip: !tenantId || !occurrenceId },
  );

  const room = useRoom({
    tenantId: tenantId!,
    occurrenceId: occurrenceId!,
    autoConnect: !!tenantId && !!occurrenceId,
  });

  if (!tenantId || !occurrenceId) return <p>Missing room context.</p>;
  if (isLoading) return <p>Loading room...</p>;
  if (error) return <p style={{ color: 'red' }}>Failed to load room.</p>;
  if (!occurrence) return null;

  const currentStatus = room.roomStatus ?? occurrence.status;
  const joinLink = `${window.location.origin}/${tenantId}/rooms/${occurrenceId}/join`;

  return (
    <div style={{ maxWidth: 800, margin: '2rem auto', padding: '1rem', fontFamily: 'system-ui, sans-serif' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <h1 style={{ margin: '0 0 0.25rem' }}>{occurrence.title}</h1>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <RoomStatusBadge status={currentStatus} />
            <span style={{ fontSize: '0.875rem', color: '#6b7280' }}>
              Scheduled: {new Date(occurrence.scheduledAt).toLocaleString()}
            </span>
            {room.isConnected && (
              <span style={{ fontSize: '0.75rem', color: '#22c55e' }}>Connected</span>
            )}
            {room.connectionError && (
              <span style={{ fontSize: '0.75rem', color: '#ef4444' }}>{room.connectionError}</span>
            )}
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button type="button" onClick={() => setShowInvites(true)}>
            Invite
          </button>
          {!room.isConnected && (
            <button type="button" onClick={room.connect}>
              Reconnect
            </button>
          )}
        </div>
      </div>

      {/* Grace Period Warning */}
      {currentStatus === 'Grace' && room.graceCountdown !== null && (
        <div
          role="alert"
          style={{
            padding: '0.75rem 1rem',
            backgroundColor: '#fef3c7',
            border: '1px solid #f59e0b',
            borderRadius: 4,
            marginBottom: '1rem',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <span style={{ fontWeight: 500 }}>
            Moderator disconnected. Room will end in {room.graceCountdown} seconds unless a moderator reconnects.
          </span>
        </div>
      )}

      {/* Ended Message */}
      {currentStatus === 'Ended' && (
        <div
          style={{
            padding: '0.75rem 1rem',
            backgroundColor: '#f3f4f6',
            border: '1px solid #d1d5db',
            borderRadius: 4,
            marginBottom: '1rem',
            textAlign: 'center',
          }}
        >
          <p style={{ fontWeight: 500, margin: 0 }}>This room has ended.</p>
          {occurrence.liveStartedAt && occurrence.liveEndedAt && (
            <p style={{ fontSize: '0.875rem', color: '#6b7280', margin: '4px 0 0' }}>
              Duration: {new Date(occurrence.liveStartedAt).toLocaleTimeString()} &ndash;{' '}
              {new Date(occurrence.liveEndedAt).toLocaleTimeString()}
            </p>
          )}
        </div>
      )}

      {/* Room Info */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: '1rem',
          marginBottom: '1.5rem',
        }}
      >
        <div style={{ padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: 4 }}>
          <div style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: 4 }}>Moderator</div>
          <div style={{ fontWeight: 500 }}>{occurrence.moderator?.displayName ?? occurrence.moderator?.userId ?? 'Unassigned'}</div>
        </div>
        <div style={{ padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: 4 }}>
          <div style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: 4 }}>Max Participants</div>
          <div style={{ fontWeight: 500 }}>{occurrence.settings.maxParticipants}</div>
        </div>
        <div style={{ padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: 4 }}>
          <div style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: 4 }}>Recording</div>
          <div style={{ fontWeight: 500 }}>{occurrence.settings.allowRecording ? 'Enabled' : 'Disabled'}</div>
        </div>
        <div style={{ padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: 4 }}>
          <div style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: 4 }}>Grace Period</div>
          <div style={{ fontWeight: 500 }}>{occurrence.gracePeriodSeconds}s</div>
        </div>
      </div>

      {/* Participant List */}
      <ParticipantList participants={room.participants} />

      {/* Invite Dialog */}
      {showInvites && (
        <InviteDialog
          tenantId={tenantId}
          occurrenceId={occurrenceId}
          joinLink={joinLink}
          allowGuestAccess={occurrence.settings.allowGuestAccess}
          onClose={() => setShowInvites(false)}
        />
      )}
    </div>
  );
}
