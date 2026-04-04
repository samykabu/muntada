import { baseApi } from '../../../shared/api/baseApi';

/** Tenant creation request payload. */
export interface CreateTenantRequest {
  name: string;
  slug: string;
  industry?: string;
  teamSize?: string;
}

/** Tenant response from the API. */
export interface TenantResponse {
  id: string;
  name: string;
  slug: string;
  industry?: string;
  teamSize?: string;
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  customDomain?: string;
  createdAt: string;
}

/** Branding update payload — sent as FormData for logo upload support. */
export interface UpdateBrandingRequest {
  tenantId: string;
  formData: FormData;
}

/** RTK Query API slice for tenant management endpoints. */
export const tenantApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    createTenant: builder.mutation<TenantResponse, CreateTenantRequest>({
      query: (body) => ({ url: '/api/v1/tenants', method: 'POST', body }),
      invalidatesTags: ['Tenant'],
    }),
    getTenant: builder.query<TenantResponse, string>({
      query: (id) => ({ url: `/api/v1/tenants/${id}` }),
      providesTags: (_result, _error, id) => [{ type: 'Tenant', id }],
    }),
    updateBranding: builder.mutation<TenantResponse, UpdateBrandingRequest>({
      query: ({ tenantId, formData }) => ({
        url: `/api/v1/tenants/${tenantId}/branding`,
        method: 'PUT',
        body: formData,
      }),
      invalidatesTags: (_result, _error, { tenantId }) => [{ type: 'Tenant', id: tenantId }],
    }),
    updateRetention: builder.mutation<void, { tenantId: string; retention: RetentionSettings }>({
      query: ({ tenantId, retention }) => ({
        url: `/api/v1/tenants/${tenantId}/retention`,
        method: 'PATCH',
        body: retention,
      }),
      invalidatesTags: (_result, _error, { tenantId }) => [{ type: 'Tenant', id: tenantId }],
    }),
    getRetention: builder.query<RetentionSettings, string>({
      query: (tenantId) => ({ url: `/api/v1/tenants/${tenantId}/retention` }),
      providesTags: (_result, _error, tenantId) => [{ type: 'Tenant', id: tenantId }],
    }),
  }),
});

/** Retention settings for a tenant. */
export interface RetentionSettings {
  recordingRetentionDays: number;
  chatRetentionDays: number;
  fileRetentionDays: number;
  auditLogRetentionDays: number;
  activityRetentionDays: number;
}

export const {
  useCreateTenantMutation,
  useGetTenantQuery,
  useUpdateBrandingMutation,
  useUpdateRetentionMutation,
  useGetRetentionQuery,
} = tenantApi;
