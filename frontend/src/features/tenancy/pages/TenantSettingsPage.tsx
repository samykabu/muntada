import { useState } from 'react';
import { BrandingEditor } from '../components/BrandingEditor';
import { MemberList } from '../components/MemberList';
import { InviteMemberDialog } from '../components/InviteMemberDialog';
import { PlanComparison } from '../components/PlanComparison';
import { RetentionSettings } from '../components/RetentionSettings';
import { UsageProgressBar } from '../components/UsageProgressBar';
import { useGetUsageQuery } from '../api/planApi';
import { useGetTenantQuery } from '../api/tenantApi';

type Tab = 'branding' | 'members' | 'plan' | 'usage' | 'retention';

const TABS: { key: Tab; label: string }[] = [
  { key: 'branding', label: 'Branding' },
  { key: 'members', label: 'Members' },
  { key: 'plan', label: 'Plan' },
  { key: 'usage', label: 'Usage' },
  { key: 'retention', label: 'Retention' },
];

/**
 * Tenant settings page with a tab-based layout for managing
 * branding, members, plans, usage, and retention policies.
 */
export function TenantSettingsPage() {
  // In a real app, tenantId comes from useTenant() context or route params.
  // For now, read from sessionStorage as a simple approach.
  const tenantId = sessionStorage.getItem('currentTenantId') ?? '';
  const [activeTab, setActiveTab] = useState<Tab>('branding');
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false);

  const { data: tenant } = useGetTenantQuery(tenantId, { skip: !tenantId });
  const { data: usage } = useGetUsageQuery(tenantId, { skip: !tenantId || activeTab !== 'usage' });

  if (!tenantId) {
    return (
      <div style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
        <p>No organization selected. Please create or select an organization first.</p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 800, margin: '2rem auto', padding: '1rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>Organization Settings</h1>

      {/* Tab bar */}
      <div style={{ display: 'flex', gap: 0, borderBottom: '2px solid #e5e7eb', marginBottom: '1.5rem' }}>
        {TABS.map(({ key, label }) => (
          <button
            key={key}
            type="button"
            onClick={() => setActiveTab(key)}
            style={{
              padding: '0.5rem 1rem',
              border: 'none',
              borderBottom: activeTab === key ? '2px solid #3b82f6' : '2px solid transparent',
              background: 'none',
              cursor: 'pointer',
              fontWeight: activeTab === key ? 600 : 400,
              color: activeTab === key ? '#3b82f6' : '#6b7280',
              marginBottom: -2,
            }}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {activeTab === 'branding' && (
        <BrandingEditor
          tenantId={tenantId}
          initialLogoUrl={tenant?.branding?.logoUrl}
          initialPrimaryColor={tenant?.branding?.primaryColor}
          initialSecondaryColor={tenant?.branding?.secondaryColor}
          initialCustomDomain={tenant?.branding?.customDomain}
        />
      )}

      {activeTab === 'members' && (
        <>
          <MemberList tenantId={tenantId} onInvite={() => setInviteDialogOpen(true)} />
          <InviteMemberDialog tenantId={tenantId} open={inviteDialogOpen} onClose={() => setInviteDialogOpen(false)} />
        </>
      )}

      {activeTab === 'plan' && <PlanComparison tenantId={tenantId} />}

      {activeTab === 'usage' && usage && (
        <div>
          <h3>Resource Usage</h3>
          {usage.metrics.map((metric) => (
            <UsageProgressBar
              key={metric.resource}
              label={metric.resource}
              current={metric.current}
              limit={metric.limit}
              unit={metric.unit}
            />
          ))}
        </div>
      )}

      {activeTab === 'retention' && <RetentionSettings tenantId={tenantId} />}
    </div>
  );
}
