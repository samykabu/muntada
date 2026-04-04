import { baseApi } from '../../../shared/api/baseApi';

/** Plan tier information. */
export interface PlanResponse {
  id: string;
  name: string;
  tier: string;
  maxRooms: number;
  maxParticipantsPerRoom: number;
  storageGb: number;
  recordingHours: number;
  monthlyPriceUsd: number;
  features: string[];
}

/** Current plan with usage context. */
export interface CurrentPlanResponse {
  plan: PlanResponse;
  startedAt: string;
  renewsAt: string;
}

/** Resource usage for a tenant. */
export interface UsageResponse {
  tenantId: string;
  rooms: UsageMetric;
  participants: UsageMetric;
  storageGb: UsageMetric;
  recordingHours: UsageMetric;
  monthlyApiCalls: UsageMetric;
}

/** A single usage metric with current value and limit. */
export interface UsageMetric {
  current: number;
  limit: number;
  unit: string;
}

/** Historical usage data point. */
export interface UsageHistoryPoint {
  date: string;
  rooms: number;
  participants: number;
  storageGb: number;
  recordingHours: number;
}

/** RTK Query API slice for plan and usage endpoints. */
export const planApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getCurrentPlan: builder.query<CurrentPlanResponse, string>({
      query: (tenantId) => ({ url: `/api/v1/tenants/${tenantId}/plan` }),
      providesTags: (_result, _error, tenantId) => [{ type: 'Plan', id: tenantId }],
    }),
    getAvailablePlans: builder.query<PlanResponse[], void>({
      query: () => ({ url: '/api/v1/plans/available' }),
    }),
    upgradePlan: builder.mutation<void, { tenantId: string; planId: string }>({
      query: ({ tenantId, planId }) => ({
        url: `/api/v1/tenants/${tenantId}/plan/upgrade`,
        method: 'POST',
        body: { planId },
      }),
      invalidatesTags: (_result, _error, { tenantId }) => [{ type: 'Plan', id: tenantId }],
    }),
    downgradePlan: builder.mutation<void, { tenantId: string; planId: string; effectiveDate: string }>({
      query: ({ tenantId, planId, effectiveDate }) => ({
        url: `/api/v1/tenants/${tenantId}/plan/downgrade`,
        method: 'POST',
        body: { planId, effectiveDate },
      }),
      invalidatesTags: (_result, _error, { tenantId }) => [{ type: 'Plan', id: tenantId }],
    }),
    getUsage: builder.query<UsageResponse, string>({
      query: (tenantId) => ({ url: `/api/v1/tenants/${tenantId}/usage` }),
      providesTags: (_result, _error, tenantId) => [{ type: 'Usage', id: tenantId }],
    }),
    getUsageHistory: builder.query<UsageHistoryPoint[], { tenantId: string; days?: number }>({
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
