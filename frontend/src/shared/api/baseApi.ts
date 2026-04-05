import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

/**
 * Base RTK Query API slice for the Muntada backend.
 * All feature-specific API slices should inject endpoints into this base.
 * Base URL is configured from environment variable or defaults to localhost.
 */
export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({
    baseUrl: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
  }),
  tagTypes: ['Tenant', 'Members', 'Plan', 'Usage', 'RoomTemplate', 'RoomOccurrence', 'RoomSeries', 'RoomInvite', 'RoomAnalytics'],
  endpoints: () => ({}),
});
