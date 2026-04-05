import { useMemo } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useGetOccurrencesQuery } from '../api/roomsApi';
import type { RoomOccurrenceResponse } from '../api/roomsApi';
import { RoomCalendar } from '../components/RoomCalendar';

/** Page displaying a calendar view of all scheduled rooms for the tenant. */
export function RoomCalendarPage() {
  const { tenantId } = useParams<{ tenantId: string }>();
  const navigate = useNavigate();

  // Fetch a broad range of occurrences (60 days past and future) for the calendar.
  const dateRange = useMemo(() => {
    const now = new Date();
    const from = new Date(now);
    from.setDate(from.getDate() - 30);
    const to = new Date(now);
    to.setDate(to.getDate() + 60);
    return {
      from: from.toISOString(),
      to: to.toISOString(),
    };
  }, []);

  const { data, isLoading, error } = useGetOccurrencesQuery(
    {
      tenantId: tenantId!,
      from: dateRange.from,
      to: dateRange.to,
      pageSize: 200,
    },
    { skip: !tenantId },
  );

  const handleOccurrenceClick = (occurrence: RoomOccurrenceResponse) => {
    navigate(`/${tenantId}/rooms/${occurrence.id}`);
  };

  if (!tenantId) return <p>Missing tenant context.</p>;

  return (
    <div style={{ maxWidth: 960, margin: '2rem auto', padding: '1rem', fontFamily: 'system-ui, sans-serif' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <h1 style={{ margin: 0 }}>Room Calendar</h1>
        <button
          type="button"
          onClick={() => navigate(`/${tenantId}/rooms/create`)}
          style={{ padding: '0.5rem 1rem' }}
        >
          Create Room
        </button>
      </div>

      {isLoading && <p>Loading rooms...</p>}
      {error && <p style={{ color: 'red' }}>Failed to load rooms.</p>}

      {data && (
        <RoomCalendar
          occurrences={data.items}
          onOccurrenceClick={handleOccurrenceClick}
        />
      )}
    </div>
  );
}
