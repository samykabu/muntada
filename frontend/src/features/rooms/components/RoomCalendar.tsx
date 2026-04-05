import { useState, useMemo } from 'react';
import type { RoomOccurrenceResponse } from '../api/roomsApi';
import { RoomStatusBadge } from './RoomStatusBadge';

interface RoomCalendarProps {
  /** Room occurrences to display on the calendar. */
  occurrences: RoomOccurrenceResponse[];
  /** Called when a room occurrence is clicked. */
  onOccurrenceClick?: (occurrence: RoomOccurrenceResponse) => void;
}

const DAY_NAMES = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

/** Returns a date key for grouping occurrences by day (YYYY-MM-DD). */
function toDateKey(dateStr: string): string {
  const d = new Date(dateStr);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/** Simple calendar grid showing scheduled rooms. */
export function RoomCalendar({ occurrences, onOccurrenceClick }: RoomCalendarProps) {
  const [viewDate, setViewDate] = useState(() => new Date());

  const year = viewDate.getFullYear();
  const month = viewDate.getMonth();

  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  const startOffset = firstDay.getDay();
  const totalDays = lastDay.getDate();

  /** Map of date keys to occurrences for the current month. */
  const occurrencesByDate = useMemo(() => {
    const map: Record<string, RoomOccurrenceResponse[]> = {};
    for (const occ of occurrences) {
      const key = toDateKey(occ.scheduledAt);
      (map[key] ??= []).push(occ);
    }
    return map;
  }, [occurrences]);

  const goToPrevMonth = () => setViewDate(new Date(year, month - 1, 1));
  const goToNextMonth = () => setViewDate(new Date(year, month + 1, 1));
  const goToToday = () => setViewDate(new Date());

  const todayKey = toDateKey(new Date().toISOString());

  /** Build an array of cell entries for the grid. */
  const cells: Array<{ day: number | null; dateKey: string | null }> = [];
  for (let i = 0; i < startOffset; i++) {
    cells.push({ day: null, dateKey: null });
  }
  for (let d = 1; d <= totalDays; d++) {
    const dateKey = `${year}-${String(month + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
    cells.push({ day: d, dateKey });
  }

  return (
    <div>
      {/* Navigation */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <button type="button" onClick={goToPrevMonth}>&larr; Prev</button>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <h2 style={{ margin: 0 }}>{MONTH_NAMES[month]} {year}</h2>
          <button type="button" onClick={goToToday} style={{ fontSize: '0.8rem' }}>Today</button>
        </div>
        <button type="button" onClick={goToNextMonth}>Next &rarr;</button>
      </div>

      {/* Day headers */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(7, 1fr)',
          gap: 1,
          textAlign: 'center',
          fontWeight: 600,
          marginBottom: 4,
        }}
      >
        {DAY_NAMES.map((d) => (
          <div key={d} style={{ padding: '0.25rem' }}>{d}</div>
        ))}
      </div>

      {/* Calendar grid */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(7, 1fr)',
          gap: 1,
        }}
      >
        {cells.map((cell, idx) => {
          const dayOccurrences = cell.dateKey ? occurrencesByDate[cell.dateKey] ?? [] : [];
          const isToday = cell.dateKey === todayKey;

          return (
            <div
              key={idx}
              style={{
                minHeight: 80,
                padding: 4,
                border: '1px solid #e5e7eb',
                backgroundColor: isToday ? '#eff6ff' : cell.day ? '#fff' : '#f9fafb',
              }}
            >
              {cell.day && (
                <>
                  <div
                    style={{
                      fontSize: '0.8rem',
                      fontWeight: isToday ? 700 : 400,
                      color: isToday ? '#2563eb' : '#374151',
                      marginBottom: 2,
                    }}
                  >
                    {cell.day}
                  </div>
                  {dayOccurrences.slice(0, 3).map((occ) => (
                    <button
                      key={occ.id}
                      type="button"
                      onClick={() => onOccurrenceClick?.(occ)}
                      style={{
                        display: 'block',
                        width: '100%',
                        textAlign: 'left',
                        background: 'none',
                        border: 'none',
                        padding: '2px 4px',
                        cursor: 'pointer',
                        borderRadius: 4,
                        fontSize: '0.7rem',
                        marginBottom: 2,
                        backgroundColor: '#f0f9ff',
                      }}
                      title={`${occ.title} - ${new Date(occ.scheduledAt).toLocaleTimeString()}`}
                    >
                      <span style={{ marginRight: 4 }}>
                        {new Date(occ.scheduledAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                      <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {occ.title}
                      </span>
                      <div style={{ marginTop: 2 }}>
                        <RoomStatusBadge status={occ.status} />
                      </div>
                    </button>
                  ))}
                  {dayOccurrences.length > 3 && (
                    <span style={{ fontSize: '0.65rem', color: '#6b7280' }}>
                      +{dayOccurrences.length - 3} more
                    </span>
                  )}
                </>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
