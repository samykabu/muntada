import { useState, type FormEvent } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  useGetTemplatesQuery,
  useCreateOccurrenceMutation,
  useCreateSeriesMutation,
} from '../api/roomsApi';
import type { RoomTemplateResponse } from '../api/roomsApi';

type RoomMode = 'one-off' | 'series';

const RECURRENCE_PRESETS = [
  { label: 'Daily', rule: 'FREQ=DAILY;INTERVAL=1' },
  { label: 'Weekdays', rule: 'FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR' },
  { label: 'Weekly', rule: 'FREQ=WEEKLY;INTERVAL=1' },
  { label: 'Bi-weekly', rule: 'FREQ=WEEKLY;INTERVAL=2' },
  { label: 'Monthly', rule: 'FREQ=MONTHLY;INTERVAL=1' },
];

/** Page for creating one-off rooms or recurring room series. */
export function CreateRoomPage() {
  const { tenantId } = useParams<{ tenantId: string }>();
  const navigate = useNavigate();

  const { data: templatesData, isLoading: templatesLoading } = useGetTemplatesQuery(
    { tenantId: tenantId! },
    { skip: !tenantId },
  );
  const [createOccurrence, { isLoading: creatingOcc, error: occError }] = useCreateOccurrenceMutation();
  const [createSeries, { isLoading: creatingSeries, error: seriesError }] = useCreateSeriesMutation();

  const [mode, setMode] = useState<RoomMode>('one-off');
  const [selectedTemplate, setSelectedTemplate] = useState<RoomTemplateResponse | null>(null);
  const [title, setTitle] = useState('');
  const [scheduledAt, setScheduledAt] = useState('');
  const [timezone, setTimezone] = useState(Intl.DateTimeFormat().resolvedOptions().timeZone);
  const [moderatorUserId, setModeratorUserId] = useState('');
  const [recurrenceRule, setRecurrenceRule] = useState(RECURRENCE_PRESETS[0].rule);
  const [seriesEndDate, setSeriesEndDate] = useState('');

  const isLoading = creatingOcc || creatingSeries;
  const error = occError || seriesError;

  const titleError =
    title.length > 0 && (title.length < 3 || title.length > 200)
      ? 'Title must be between 3 and 200 characters'
      : null;

  const isValid =
    !!tenantId &&
    !!selectedTemplate &&
    title.length >= 3 &&
    title.length <= 200 &&
    !!scheduledAt &&
    !!moderatorUserId;

  const handleTemplateSelect = (templateId: string) => {
    const tpl = templatesData?.items.find((t) => t.id === templateId) ?? null;
    setSelectedTemplate(tpl);
    if (tpl && !title) {
      setTitle(tpl.name);
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!isValid || !tenantId || !selectedTemplate) return;

    try {
      if (mode === 'one-off') {
        await createOccurrence({
          tenantId,
          templateId: selectedTemplate.id,
          title,
          scheduledAt: new Date(scheduledAt).toISOString(),
          organizerTimeZoneId: timezone,
          moderatorUserId,
        }).unwrap();
      } else {
        await createSeries({
          tenantId,
          templateId: selectedTemplate.id,
          title,
          recurrenceRule,
          organizerTimeZoneId: timezone,
          startsAt: new Date(scheduledAt).toISOString(),
          endsAt: seriesEndDate ? new Date(seriesEndDate).toISOString() : undefined,
          moderatorUserId,
        }).unwrap();
      }
      navigate(`/${tenantId}/rooms`);
    } catch {
      // Error rendered from RTK Query state
    }
  };

  if (!tenantId) return <p>Missing tenant context.</p>;

  return (
    <div style={{ maxWidth: 560, margin: '2rem auto', padding: '1rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>Create Room</h1>

      {/* Mode Selector */}
      <div style={{ display: 'flex', gap: 8, marginBottom: '1.5rem' }}>
        <button
          type="button"
          onClick={() => setMode('one-off')}
          style={{
            flex: 1,
            padding: '0.5rem',
            border: mode === 'one-off' ? '2px solid #3b82f6' : '1px solid #d1d5db',
            borderRadius: 4,
            backgroundColor: mode === 'one-off' ? '#eff6ff' : '#fff',
            cursor: 'pointer',
          }}
        >
          One-off Room
        </button>
        <button
          type="button"
          onClick={() => setMode('series')}
          style={{
            flex: 1,
            padding: '0.5rem',
            border: mode === 'series' ? '2px solid #3b82f6' : '1px solid #d1d5db',
            borderRadius: 4,
            backgroundColor: mode === 'series' ? '#eff6ff' : '#fff',
            cursor: 'pointer',
          }}
        >
          Recurring Series
        </button>
      </div>

      <form onSubmit={handleSubmit}>
        {/* Template Selector */}
        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="room-template" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
            Room Template *
          </label>
          {templatesLoading ? (
            <p>Loading templates...</p>
          ) : (
            <select
              id="room-template"
              value={selectedTemplate?.id ?? ''}
              onChange={(e) => handleTemplateSelect(e.target.value)}
              required
              style={{ width: '100%' }}
            >
              <option value="">Select a template</option>
              {templatesData?.items.map((tpl) => (
                <option key={tpl.id} value={tpl.id}>
                  {tpl.name} (max {tpl.settings.maxParticipants} participants)
                </option>
              ))}
            </select>
          )}
        </div>

        {/* Template settings preview */}
        {selectedTemplate && (
          <div
            style={{
              marginBottom: '1rem',
              padding: '0.75rem',
              backgroundColor: '#f9fafb',
              borderRadius: 4,
              fontSize: '0.8rem',
              color: '#6b7280',
            }}
          >
            <strong>Template Settings:</strong>{' '}
            Max {selectedTemplate.settings.maxParticipants} participants
            {selectedTemplate.settings.allowGuestAccess && ' | Guests allowed'}
            {selectedTemplate.settings.allowRecording && ' | Recording'}
            {selectedTemplate.settings.allowTranscription && ' | Transcription'}
          </div>
        )}

        {/* Title */}
        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="room-title" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
            Room Title *
          </label>
          <input
            id="room-title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
            minLength={3}
            maxLength={200}
            placeholder="Weekly Engineering Standup"
            style={{ width: '100%' }}
          />
          {titleError && <p style={{ color: 'red', fontSize: '0.8rem', margin: '4px 0 0' }}>{titleError}</p>}
        </div>

        {/* Schedule */}
        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="room-scheduled-at" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
            {mode === 'one-off' ? 'Scheduled Date & Time *' : 'Series Start Date & Time *'}
          </label>
          <input
            id="room-scheduled-at"
            type="datetime-local"
            value={scheduledAt}
            onChange={(e) => setScheduledAt(e.target.value)}
            required
            style={{ width: '100%' }}
          />
        </div>

        {/* Timezone */}
        <div style={{ marginBottom: '1rem' }}>
          <label htmlFor="room-timezone" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
            Timezone *
          </label>
          <input
            id="room-timezone"
            type="text"
            value={timezone}
            onChange={(e) => setTimezone(e.target.value)}
            required
            placeholder="Asia/Riyadh"
            style={{ width: '100%' }}
          />
          <p style={{ fontSize: '0.75rem', color: '#9ca3af', margin: '4px 0 0' }}>
            IANA timezone identifier (e.g., Asia/Riyadh, America/New_York)
          </p>
        </div>

        {/* Recurrence (series only) */}
        {mode === 'series' && (
          <>
            <div style={{ marginBottom: '1rem' }}>
              <label htmlFor="room-recurrence" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
                Recurrence Pattern *
              </label>
              <select
                id="room-recurrence"
                value={recurrenceRule}
                onChange={(e) => setRecurrenceRule(e.target.value)}
                style={{ width: '100%' }}
              >
                {RECURRENCE_PRESETS.map((p) => (
                  <option key={p.rule} value={p.rule}>{p.label}</option>
                ))}
              </select>
            </div>

            <div style={{ marginBottom: '1rem' }}>
              <label htmlFor="room-series-end" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
                Series End Date (optional)
              </label>
              <input
                id="room-series-end"
                type="date"
                value={seriesEndDate}
                onChange={(e) => setSeriesEndDate(e.target.value)}
                style={{ width: '100%' }}
              />
              <p style={{ fontSize: '0.75rem', color: '#9ca3af', margin: '4px 0 0' }}>
                Leave blank for an indefinite series.
              </p>
            </div>
          </>
        )}

        {/* Moderator */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label htmlFor="room-moderator" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
            Moderator User ID *
          </label>
          <input
            id="room-moderator"
            type="text"
            value={moderatorUserId}
            onChange={(e) => setModeratorUserId(e.target.value)}
            required
            placeholder="User ID of the moderator"
            style={{ width: '100%' }}
          />
        </div>

        {error && (
          <p role="alert" style={{ color: 'red', marginBottom: '0.75rem' }}>
            Failed to create room. Please try again.
          </p>
        )}

        <button type="submit" disabled={isLoading || !isValid} style={{ width: '100%', padding: '0.5rem' }}>
          {isLoading ? 'Creating...' : mode === 'one-off' ? 'Create Room' : 'Create Series'}
        </button>
      </form>
    </div>
  );
}
