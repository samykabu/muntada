import { useGetCurrentPlanQuery, useGetAvailablePlansQuery, useUpgradePlanMutation, useDowngradePlanMutation } from '../api/planApi';
import type { PlanDefinitionResponse, PlanLimits } from '../api/planApi';

interface PlanComparisonProps {
  tenantId: string;
}

const featureLabels: Record<keyof PlanLimits, string> = {
  maxRooms: 'Rooms',
  maxParticipantsPerRoom: 'Participants / Room',
  storageGb: 'Storage (GB)',
  recordingHours: 'Recording Hours',
};

/** Displays the current plan and available plans for comparison and upgrade/downgrade. */
export function PlanComparison({ tenantId }: PlanComparisonProps) {
  const { data: currentPlan, isLoading: loadingCurrent } = useGetCurrentPlanQuery(tenantId);
  const { data: plans, isLoading: loadingPlans } = useGetAvailablePlansQuery();
  const [upgradePlan, { isLoading: upgrading }] = useUpgradePlanMutation();
  const [downgradePlan, { isLoading: downgrading }] = useDowngradePlanMutation();

  if (loadingCurrent || loadingPlans) return <p>Loading plans...</p>;
  if (!currentPlan || !plans) return <p>Unable to load plan information.</p>;

  const isCurrentPlan = (plan: PlanDefinitionResponse) => plan.id === currentPlan.plan.id;

  const handleAction = async (plan: PlanDefinitionResponse) => {
    if (plan.monthlyPriceUsd > currentPlan.plan.monthlyPriceUsd) {
      await upgradePlan({ tenantId, targetPlanDefinitionId: plan.id });
    } else {
      await downgradePlan({ tenantId, targetPlanDefinitionId: plan.id, effectiveDate: 'immediate' });
    }
  };

  return (
    <div>
      <h3>Plans</h3>

      {/* Plan cards */}
      <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', marginBottom: '1.5rem' }}>
        {plans.map((plan) => {
          const isCurrent = isCurrentPlan(plan);
          return (
            <div
              key={plan.id}
              style={{
                border: isCurrent ? '2px solid #3b82f6' : '1px solid #e5e7eb',
                borderRadius: 8,
                padding: '1rem',
                flex: '1 1 200px',
                maxWidth: 260,
                backgroundColor: isCurrent ? '#eff6ff' : undefined,
              }}
            >
              <h4 style={{ margin: '0 0 0.5rem' }}>{plan.name}</h4>
              <p style={{ fontSize: '1.25rem', fontWeight: 700, margin: '0 0 0.75rem' }}>
                ${plan.monthlyPriceUsd}<span style={{ fontSize: '0.75rem', fontWeight: 400 }}> / mo</span>
              </p>
              <ul style={{ padding: '0 0 0 1.2rem', margin: '0 0 0.75rem', fontSize: '0.875rem' }}>
                {plan.features.map((f) => <li key={f}>{f}</li>)}
              </ul>
              {isCurrent ? (
                <span style={{ fontWeight: 600, color: '#3b82f6' }}>Current Plan</span>
              ) : (
                <button
                  type="button"
                  onClick={() => handleAction(plan)}
                  disabled={upgrading || downgrading}
                >
                  {plan.monthlyPriceUsd > currentPlan.plan.monthlyPriceUsd ? 'Upgrade' : 'Downgrade'}
                </button>
              )}
            </div>
          );
        })}
      </div>

      {/* Feature comparison table */}
      <h4>Feature Comparison</h4>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem' }}>
        <thead>
          <tr style={{ borderBottom: '2px solid #e5e7eb', textAlign: 'left' }}>
            <th style={{ padding: '0.5rem' }}>Feature</th>
            {plans.map((p) => (
              <th key={p.id} style={{ padding: '0.5rem', fontWeight: isCurrentPlan(p) ? 700 : 400 }}>{p.name}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {(Object.keys(featureLabels) as Array<keyof PlanLimits>).map((key) => (
            <tr key={key} style={{ borderBottom: '1px solid #f3f4f6' }}>
              <td style={{ padding: '0.5rem' }}>{featureLabels[key]}</td>
              {plans.map((p) => (
                <td key={p.id} style={{ padding: '0.5rem', fontWeight: isCurrentPlan(p) ? 600 : 400 }}>
                  {p.limits[key]?.toLocaleString() ?? '-'}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
