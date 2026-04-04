/** Props for the UsageProgressBar component. */
export interface UsageProgressBarProps {
  label: string;
  current: number;
  limit: number;
  unit: string;
}

/** Returns a color based on usage percentage thresholds. */
function getBarColor(percent: number): string {
  if (percent >= 100) return '#dc2626'; // red
  if (percent >= 95) return '#ea580c';  // orange
  if (percent >= 80) return '#ca8a04';  // yellow
  return '#16a34a';                      // green
}

/**
 * Reusable progress bar showing resource consumption against a limit.
 * Colors shift from green to red as usage approaches the cap.
 */
export function UsageProgressBar({ label, current, limit, unit }: UsageProgressBarProps) {
  const percent = limit > 0 ? Math.min(Math.round((current / limit) * 100), 100) : 0;
  const color = getBarColor(percent);

  return (
    <div style={{ marginBottom: '1rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4, fontSize: '0.875rem' }}>
        <span style={{ fontWeight: 600 }}>{label}</span>
        <span>
          {current.toLocaleString()} / {limit.toLocaleString()} {unit} ({percent}%)
        </span>
      </div>
      <div
        style={{ width: '100%', height: 8, backgroundColor: '#e5e7eb', borderRadius: 4, overflow: 'hidden' }}
        role="progressbar"
        aria-valuenow={current}
        aria-valuemin={0}
        aria-valuemax={limit}
        aria-label={`${label} usage`}
      >
        <div style={{ width: `${percent}%`, height: '100%', backgroundColor: color, borderRadius: 4, transition: 'width 0.3s ease' }} />
      </div>
    </div>
  );
}
