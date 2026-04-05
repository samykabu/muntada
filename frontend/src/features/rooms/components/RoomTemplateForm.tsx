import { useState, type FormEvent } from 'react';
import type { RoomSettings } from '../api/roomsApi';

/** Props for the room template form. */
export interface RoomTemplateFormProps {
  /** Initial values for editing; omit for create mode. */
  initialValues?: {
    name: string;
    description?: string;
    settings: RoomSettings;
  };
  /** Whether the name field is disabled (immutable for existing templates). */
  nameDisabled?: boolean;
  /** Called when the form is submitted with valid data. */
  onSubmit: (data: { name: string; description?: string; settings: RoomSettings }) => void;
  /** Whether a submission is in progress. */
  isLoading?: boolean;
  /** Error message to display. */
  error?: string;
}

const DEFAULT_SETTINGS: RoomSettings = {
  maxParticipants: 50,
  allowGuestAccess: true,
  allowRecording: true,
  allowTranscription: false,
  autoStartRecording: false,
};

/** Form for creating or editing room templates. */
export function RoomTemplateForm({
  initialValues,
  nameDisabled = false,
  onSubmit,
  isLoading = false,
  error,
}: RoomTemplateFormProps) {
  const [name, setName] = useState(initialValues?.name ?? '');
  const [description, setDescription] = useState(initialValues?.description ?? '');
  const [settings, setSettings] = useState<RoomSettings>(initialValues?.settings ?? DEFAULT_SETTINGS);

  const nameError =
    name.length > 0 && (name.length < 3 || name.length > 100)
      ? 'Name must be between 3 and 100 characters'
      : null;

  const maxParticipantsError =
    settings.maxParticipants < 1 || settings.maxParticipants > 10000
      ? 'Max participants must be between 1 and 10,000'
      : null;

  const isValid =
    name.length >= 3 && name.length <= 100 && !maxParticipantsError;

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!isValid) return;
    onSubmit({
      name,
      description: description || undefined,
      settings,
    });
  };

  const updateSetting = <K extends keyof RoomSettings>(key: K, value: RoomSettings[K]) => {
    setSettings((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <form onSubmit={handleSubmit}>
      {/* Name */}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="template-name" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
          Template Name *
        </label>
        <input
          id="template-name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={nameDisabled}
          required
          minLength={3}
          maxLength={100}
          placeholder="Weekly Standup"
          style={{ width: '100%' }}
        />
        {nameError && <p style={{ color: 'red', fontSize: '0.8rem', margin: '4px 0 0' }}>{nameError}</p>}
      </div>

      {/* Description */}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="template-description" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
          Description
        </label>
        <textarea
          id="template-description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={500}
          placeholder="Describe the purpose of this template..."
          rows={3}
          style={{ width: '100%' }}
        />
      </div>

      {/* Max Participants */}
      <div style={{ marginBottom: '1rem' }}>
        <label htmlFor="template-max-participants" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
          Max Participants *
        </label>
        <input
          id="template-max-participants"
          type="number"
          value={settings.maxParticipants}
          onChange={(e) => updateSetting('maxParticipants', parseInt(e.target.value, 10) || 1)}
          min={1}
          max={10000}
          style={{ width: '100%' }}
        />
        {maxParticipantsError && (
          <p style={{ color: 'red', fontSize: '0.8rem', margin: '4px 0 0' }}>{maxParticipantsError}</p>
        )}
      </div>

      {/* Toggle Settings */}
      <fieldset style={{ border: '1px solid #e5e7eb', borderRadius: 4, padding: '1rem', marginBottom: '1rem' }}>
        <legend style={{ fontWeight: 500 }}>Room Settings</legend>

        <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
          <input
            type="checkbox"
            checked={settings.allowGuestAccess}
            onChange={(e) => updateSetting('allowGuestAccess', e.target.checked)}
          />
          Allow Guest Access (magic link)
        </label>

        <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
          <input
            type="checkbox"
            checked={settings.allowRecording}
            onChange={(e) => {
              updateSetting('allowRecording', e.target.checked);
              if (!e.target.checked) {
                updateSetting('allowTranscription', false);
                updateSetting('autoStartRecording', false);
              }
            }}
          />
          Allow Recording
        </label>

        <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8, paddingLeft: 24 }}>
          <input
            type="checkbox"
            checked={settings.autoStartRecording}
            onChange={(e) => updateSetting('autoStartRecording', e.target.checked)}
            disabled={!settings.allowRecording}
          />
          Auto-start Recording
        </label>

        <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
          <input
            type="checkbox"
            checked={settings.allowTranscription}
            onChange={(e) => updateSetting('allowTranscription', e.target.checked)}
            disabled={!settings.allowRecording}
          />
          Allow Transcription
        </label>

        {settings.allowTranscription && (
          <div style={{ paddingLeft: 24 }}>
            <label htmlFor="template-transcription-lang" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
              Default Transcription Language
            </label>
            <select
              id="template-transcription-lang"
              value={settings.defaultTranscriptionLanguage ?? 'en'}
              onChange={(e) => updateSetting('defaultTranscriptionLanguage', e.target.value)}
              style={{ width: '100%' }}
            >
              <option value="en">English</option>
              <option value="ar">Arabic</option>
              <option value="fr">French</option>
              <option value="es">Spanish</option>
            </select>
          </div>
        )}
      </fieldset>

      {error && (
        <p role="alert" style={{ color: 'red', marginBottom: '0.75rem' }}>
          {error}
        </p>
      )}

      <button type="submit" disabled={isLoading || !isValid} style={{ width: '100%', padding: '0.5rem' }}>
        {isLoading ? 'Saving...' : initialValues ? 'Update Template' : 'Create Template'}
      </button>
    </form>
  );
}
