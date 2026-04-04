import { useGetUsageQuery, useGetUsageHistoryQuery } from '../api/planApi';
import { UsageProgressBar } from '../components/UsageProgressBar';

/**
 * Standalone usage dashboard showing current resource consumption
 * and historical usage trends (chart placeholder).
 */
export function UsageDashboardPage() {
  const tenantId = sessionStorage.getItem('currentTenantId') ?? '';
  const { data: usage, isLoading, error } = useGetUsageQuery(tenantId, { skip: !tenantId });
  const { data: history } = useGetUsageHistoryQuery({ tenantId, days: 30 }, { skip: !tenantId });

  if (!tenantId) {
    return (
      <div style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
        <p>No organization selected.</p>
      </div>
    );
  }

  if (isLoading) return <div style={{ padding: '2rem' }}>Loading usage data...</div>;
  if (error || !usage) return <div style={{ padding: '2rem', color: 'red' }}>Failed to load usage data.</div>;

  return (
    <div style={{ maxWidth: 700, margin: '2rem auto', padding: '1rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>Usage Dashboard</h1>

      {/* Current usage */}
      <section style={{ marginBottom: '2rem' }}>
        <h2 style={{ fontSize: '1.125rem', marginBottom: '1rem' }}>Current Usage</h2>
        <UsageProgressBar label="Rooms" current={usage.rooms.current} limit={usage.rooms.limit} unit={usage.rooms.unit} />
        <UsageProgressBar label="Participants" current={usage.participants.current} limit={usage.participants.limit} unit={usage.participants.unit} />
        <UsageProgressBar label="Storage" current={usage.storageGb.current} limit={usage.storageGb.limit} unit={usage.storageGb.unit} />
        <UsageProgressBar label="Recording" current={usage.recordingHours.current} limit={usage.recordingHours.limit} unit={usage.recordingHours.unit} />
        <UsageProgressBar label="API Calls" current={usage.monthlyApiCalls.current} limit={usage.monthlyApiCalls.limit} unit={usage.monthlyApiCalls.unit} />
      </section>

      {/* Historical usage — placeholder for a chart library */}
      <section>
        <h2 style={{ fontSize: '1.125rem', marginBottom: '1rem' }}>Usage History (30 days)</h2>
        {history && history.length > 0 ? (
          <div
            style={{
              border: '1px dashed #d1d5db',
              borderRadius: 8,
              padding: '2rem',
              textAlign: 'center',
              color: '#9ca3af',
            }}
          >
            <p style={{ margin: 0 }}>Chart placeholder &mdash; {history.length} data points available.</p>
            <p style={{ margin: '0.5rem 0 0', fontSize: '0.8rem' }}>
              Integrate a chart library (e.g. Recharts, Chart.js) to visualize trends.
            </p>
            {/* Render raw data as a simple table fallback */}
            <table style={{ width: '100%', marginTop: '1rem', borderCollapse: 'collapse', fontSize: '0.75rem', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid #e5e7eb' }}>
                  <th style={{ padding: 4 }}>Date</th>
                  <th style={{ padding: 4 }}>Rooms</th>
                  <th style={{ padding: 4 }}>Participants</th>
                  <th style={{ padding: 4 }}>Storage (GB)</th>
                  <th style={{ padding: 4 }}>Recording (hrs)</th>
                </tr>
              </thead>
              <tbody>
                {history.slice(0, 7).map((h) => (
                  <tr key={h.date} style={{ borderBottom: '1px solid #f3f4f6' }}>
                    <td style={{ padding: 4 }}>{h.date}</td>
                    <td style={{ padding: 4 }}>{h.rooms}</td>
                    <td style={{ padding: 4 }}>{h.participants}</td>
                    <td style={{ padding: 4 }}>{h.storageGb}</td>
                    <td style={{ padding: 4 }}>{h.recordingHours}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p style={{ color: '#9ca3af' }}>No historical data available yet.</p>
        )}
      </section>
    </div>
  );
}
