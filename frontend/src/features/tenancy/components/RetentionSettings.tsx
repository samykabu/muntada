import { useState, useEffect, type FormEvent } from 'react';
import { useGetRetentionQuery, useUpdateRetentionMutation } from '../api/tenantApi';
import type { RetentionSettings as RetentionData } from '../api/tenantApi';

interface RetentionSettingsProps {
  tenantId: string;
}

interface FieldConfig {
  key: keyof RetentionData;
  label: string;
  min: number;
  max: number;
}

const FIELDS: FieldConfig[] = [
  { key: 'recordingRetentionDays', label: 'Recordings', min: 30, max: 3650 },
  { key: 'chatMessageRetentionDays', label: 'Chat Messages', min: 30, max: 3650 },
  { key: 'fileRetentionDays', label: 'Files', min: 30, max: 3650 },
  { key: 'auditLogRetentionDays', label: 'Audit Logs', min: 2555, max: 3650 },
  { key: 'userActivityLogRetentionDays', label: 'Activity', min: 30, max: 3650 },
];

const DEFAULT_VALUES: RetentionData = {
  recordingRetentionDays: 365,
  chatMessageRetentionDays: 365,
  fileRetentionDays: 365,
  auditLogRetentionDays: 2555,
  userActivityLogRetentionDays: 365,
};

/** Form for configuring data retention periods per category. */
export function RetentionSettings({ tenantId }: RetentionSettingsProps) {
  const { data: existing, isLoading: loadingRetention } = useGetRetentionQuery(tenantId);
  const [updateRetention, { isLoading: saving, isSuccess, error }] = useUpdateRetentionMutation();
  const [values, setValues] = useState<RetentionData>(DEFAULT_VALUES);

  useEffect(() => {
    if (existing) setValues(existing);
  }, [existing]);

  const handleChange = (key: keyof RetentionData, raw: string) => {
    const num = parseInt(raw, 10);
    if (!isNaN(num)) {
      setValues((prev) => ({ ...prev, [key]: num }));
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    await updateRetention({ tenantId, retention: values });
  };

  if (loadingRetention) return <p>Loading retention settings...</p>;

  return (
    <form onSubmit={handleSubmit} style={{ maxWidth: 480 }}>
      <h3>Data Retention</h3>
      <p style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '1rem' }}>
        Configure how long each type of data is retained (in days).
      </p>

      {FIELDS.map(({ key, label, min, max }) => (
        <div key={key} style={{ marginBottom: '0.75rem' }}>
          <label htmlFor={`retention-${key}`} style={{ display: 'block', marginBottom: 4, fontSize: '0.875rem', fontWeight: 500 }}>
            {label}
          </label>
          <input
            id={`retention-${key}`}
            type="number"
            value={values[key]}
            onChange={(e) => handleChange(key, e.target.value)}
            min={min}
            max={max}
            required
            style={{ width: 120 }}
          />
          <span style={{ marginLeft: 8, fontSize: '0.75rem', color: '#9ca3af' }}>
            days (min {min} / max {max})
          </span>
        </div>
      ))}

      {error && <p role="alert" style={{ color: 'red', fontSize: '0.875rem' }}>Failed to save retention settings.</p>}
      {isSuccess && <p style={{ color: 'green', fontSize: '0.875rem' }}>Retention settings saved.</p>}

      <button type="submit" disabled={saving} style={{ marginTop: '0.5rem' }}>
        {saving ? 'Saving...' : 'Save Retention Settings'}
      </button>
    </form>
  );
}
