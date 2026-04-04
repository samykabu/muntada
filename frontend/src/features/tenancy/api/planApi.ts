import { baseApi } from '../../../shared/api/baseApi';

/** Plan limits nested under a plan definition. */
export interface PlanLimits {
  maxRooms: number;
  maxParticipantsPerRoom: number;
  storageGb: number;
  recordingHours: number;
}

/** Plan tier information matching backend PlanResponse. */
export interface PlanResponse {
  id: string;
  name: string;
  tier: string;
  limits: PlanLimits;
  monthlyPriceUsd: number;
  features: string[];
}

/** Plan definition response matching backend PlanDefinitionResponse. */
export interface PlanDefinitionResponse {
  id: string;
  name: string;
  tier: string;
  limits: PlanLimits;
  monthlyPriceUsd: number;
  features: string[];
}

/** Current plan with usage context. */
export interface CurrentPlanResponse {
  plan: PlanResponse;
  startedAt: string;
  renewsAt: string;
}

/** A single usage metric with current value and limit. */
export interface UsageMetric {
  resource: string;
  current: number;
  limit: number;
  unit: string;
  percentUsed: number;
  thresholdStatus: string;
}

/** Resource usage for a tenant matching backend response. */
export interface UsageResponse {
  tenantId: string;
  planName: string;
  metrics: UsageMetric[];
}

/** Historical usage snapshot. */
export interface UsageSnapshot {
  date: string;
  rooms: number;
  participants: number;
  storageGb: number;
  recordingHours: number;
}

/** Historical usage response matching backend. */
export interface UsageHistoryResponse {
  fromDate: string;
  toDate: string;
  snapshots: UsageSnapshot[];
}

/** RTK Query API slice for plan and usage endpoints. */
export const planApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getCurrentPlan: builder.query<CurrentPlanResponse, string>({
      query: (tenantId) => ({ url: `/api/v1/tenants/${tenantId}/plan` }),
      providesTags: (_result, _error, tenantId) => [{ type: 'Plan', id: tenantId }],
    }),
    getAvailablePlans: builder.query<PlanDefinitionResponse[], void>({
      query: () => ({ url: '/api/v1/plans/available' }),
    }),
    upgradePlan: builder.mutation<void, { tenantId: string; targetPlanDefinitionId: string }>({
      query: ({ tenantId, targetPlanDefinitionId }) => ({
        url: `/api/v1/tenants/${tenantId}/plan/upgrade`,
        method: 'POST',
        body: { targetPlanDefinitionId },
      }),
      invalidatesTags: (_result, _error, { tenantId }) => [{ type: 'Plan', id: tenantId }],
    }),
    downgradePlan: builder.mutation<void, { tenantId: string; targetPlanDefinitionId: string; effectiveDate: 'immediate' | 'next-billing-cycle' }>({
      query: ({ tenantId, targetPlanDefinitionId, effectiveDate }) => ({
        url: `/api/v1/tenants/${tenantId}/plan/downgrade`,
        method: 'POST',
        body: { targetPlanDefinitionId, effectiveDate },
      }),
      invalidatesTags: (_result, _error, { tenantId }) => [{ type: 'Plan', id: tenantId }],
    }),
    getUsage: builder.query<UsageResponse, string>({
      query: (tenantId) => ({ url: `/api/v1/tenants/${tenantId}/usage` }),
      providesTags: (_result, _error, tenantId) => [{ type: 'Usage', id: tenantId }],
    }),
    getUsageHistory: builder.query<UsageHistoryResponse, { tenantId: string; days?: number }>({
      query: ({ tenantId, days = 30 }) => ({
        url: `/api/v1/tenants/${tenantId}/usage/history`,
        params: { days },
      }),
      providesTags: (_result, _error, { tenantId }) => [{ type: 'Usage', id: tenantId }],
    }),
  }),
});

export const {
  useGetCurrentPlanQuery,
  useGetAvailablePlansQuery,
  useUpgradePlanMutation,
  useDowngradePlanMutation,
  useGetUsageQuery,
  useGetUsageHistoryQuery,
} = planApi;
