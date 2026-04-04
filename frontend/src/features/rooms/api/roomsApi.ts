import { baseApi } from '../../../shared/api/baseApi';

// ── Enumerations ──────────────────────────────────────────────────────────────

export type RoomOccurrenceStatus = 'Draft' | 'Scheduled' | 'Live' | 'Grace' | 'Ended' | 'Archived';
export type RoomSeriesStatus = 'Active' | 'Ended';
export type ParticipantRole = 'Moderator' | 'Member' | 'Guest';
export type MediaState = 'Muted' | 'Unmuted' | 'Off' | 'On';
export type RecordingStatus = 'Processing' | 'Ready' | 'Failed';
export type RecordingVisibility = 'Private' | 'Shared' | 'Public';
export type TranscriptStatus = 'Processing' | 'Ready' | 'Failed';

// ── Value Objects / Sub-types ─────────────────────────────────────────────────

export interface RoomSettings {
  maxParticipants: number;
  allowGuestAccess: boolean;
  allowRecording: boolean;
  allowTranscription: boolean;
  defaultTranscriptionLanguage?: string;
  autoStartRecording: boolean;
}

export interface ModeratorInfo {
  userId: string;
  displayName?: string;
  assignedAt: string;
  disconnectedAt?: string;
}

export interface Transcript {
  language: string;
  s3Path: string;
  textS3Path: string;
  downloadUrl?: string;
  textDownloadUrl?: string;
  status: TranscriptStatus;
  createdAt: string;
}

// ── Response Types ────────────────────────────────────────────────────────────

export interface RoomTemplateResponse {
  id: string;
  tenantId: string;
  name: string;
  description?: string;
  settings: RoomSettings;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface RoomOccurrenceResponse {
  id: string;
  tenantId: string;
  roomSeriesId?: string;
  title: string;
  scheduledAt: string;
  organizerTimeZoneId: string;
  liveStartedAt?: string;
  liveEndedAt?: string;
  status: RoomOccurrenceStatus;
  moderator: ModeratorInfo | null;
  settings: RoomSettings;
  gracePeriodSeconds: number;
  graceStartedAt?: string;
  isCancelled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface RoomSeriesResponse {
  id: string;
  tenantId: string;
  templateId: string;
  title: string;
  recurrenceRule: string;
  organizerTimeZoneId: string;
  startsAt: string;
  endsAt?: string;
  status: RoomSeriesStatus;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface RoomParticipantResponse {
  id: string;
  roomOccurrenceId: string;
  userId?: string;
  displayName: string;
  role: ParticipantRole;
  joinedAt: string;
  leftAt?: string;
  audioState: MediaState;
  videoState: MediaState;
}

export interface RecordingResponse {
  id: string;
  roomOccurrenceId: string;
  tenantId: string;
  s3Path: string;
  downloadUrl?: string;
  liveKitEgressId?: string;
  fileSizeBytes: number;
  durationSeconds: number;
  status: RecordingStatus;
  visibility: RecordingVisibility;
  transcripts: Transcript[];
  createdAt: string;
  updatedAt: string;
}

export interface RoomAnalyticsResponse {
  roomOccurrenceId: string;
  totalParticipants: number;
  peakConcurrent: number;
  durationSeconds: number;
  participantDwellTimes: Array<{
    userId?: string;
    displayName: string;
    dwellSeconds: number;
  }>;
}

// ── Request Types ─────────────────────────────────────────────────────────────

export interface CreateTemplateRequest {
  name: string;
  description?: string;
  settings: RoomSettings;
}

export interface UpdateTemplateRequest {
  tenantId: string;
  templateId: string;
  description?: string;
  settings: RoomSettings;
}

export interface CreateOccurrenceRequest {
  title: string;
  scheduledAt: string;
  organizerTimeZoneId: string;
  moderatorUserId: string;
  templateId?: string;
  settings?: Partial<RoomSettings>;
  gracePeriodSeconds?: number;
}

export interface UpdateOccurrenceRequest {
  tenantId: string;
  occurrenceId: string;
  title?: string;
  scheduledAt?: string;
  moderatorUserId?: string;
  settings?: Partial<RoomSettings>;
  gracePeriodSeconds?: number;
}

export interface CreateSeriesRequest {
  templateId: string;
  title: string;
  recurrenceRule: string;
  organizerTimeZoneId: string;
  startsAt: string;
  endsAt?: string;
  moderatorUserId: string;
}

export interface UpdateSeriesRequest {
  tenantId: string;
  seriesId: string;
  title?: string;
  recurrenceRule?: string;
  endsAt?: string;
}

/** Paginated list response for collection endpoints. */
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ── Query Param Types ─────────────────────────────────────────────────────────

interface TenantScope {
  tenantId: string;
}

interface PaginationParams {
  page?: number;
  pageSize?: number;
}

interface OccurrenceListParams extends TenantScope, PaginationParams {
  status?: RoomOccurrenceStatus;
  from?: string;
  to?: string;
  seriesId?: string;
}

interface SeriesListParams extends TenantScope, PaginationParams {
  status?: RoomSeriesStatus;
}

// ── RTK Query API ─────────────────────────────────────────────────────────────

/** RTK Query API slice for rooms and scheduling endpoints. */
export const roomsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    // ── Templates ───────────────────────────────────────────────────────
    getTemplates: builder.query<PaginatedResponse<RoomTemplateResponse>, TenantScope & PaginationParams>({
      query: ({ tenantId, page = 1, pageSize = 20 }) => ({
        url: `/api/v1/tenants/${tenantId}/room-templates`,
        params: { page, pageSize },
      }),
      providesTags: (result) =>
        result
          ? [
              ...result.items.map(({ id }) => ({ type: 'RoomTemplate' as const, id })),
              { type: 'RoomTemplate', id: 'LIST' },
            ]
          : [{ type: 'RoomTemplate', id: 'LIST' }],
    }),

    getTemplate: builder.query<RoomTemplateResponse, TenantScope & { templateId: string }>({
      query: ({ tenantId, templateId }) => ({
        url: `/api/v1/tenants/${tenantId}/room-templates/${templateId}`,
      }),
      providesTags: (_result, _error, { templateId }) => [{ type: 'RoomTemplate', id: templateId }],
    }),

    createTemplate: builder.mutation<RoomTemplateResponse, TenantScope & CreateTemplateRequest>({
      query: ({ tenantId, ...body }) => ({
        url: `/api/v1/tenants/${tenantId}/room-templates`,
        method: 'POST',
        body,
      }),
      invalidatesTags: [{ type: 'RoomTemplate', id: 'LIST' }],
    }),

    updateTemplate: builder.mutation<RoomTemplateResponse, UpdateTemplateRequest>({
      query: ({ tenantId, templateId, ...body }) => ({
        url: `/api/v1/tenants/${tenantId}/room-templates/${templateId}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: (_result, _error, { templateId }) => [
        { type: 'RoomTemplate', id: templateId },
        { type: 'RoomTemplate', id: 'LIST' },
      ],
    }),

    // ── Occurrences ─────────────────────────────────────────────────────
    getOccurrences: builder.query<PaginatedResponse<RoomOccurrenceResponse>, OccurrenceListParams>({
      query: ({ tenantId, page = 1, pageSize = 20, status, from, to, seriesId }) => ({
        url: `/api/v1/tenants/${tenantId}/room-occurrences`,
        params: { page, pageSize, status, from, to, seriesId },
      }),
      providesTags: (result) =>
        result
          ? [
              ...result.items.map(({ id }) => ({ type: 'RoomOccurrence' as const, id })),
              { type: 'RoomOccurrence', id: 'LIST' },
            ]
          : [{ type: 'RoomOccurrence', id: 'LIST' }],
    }),

    getOccurrence: builder.query<RoomOccurrenceResponse, TenantScope & { occurrenceId: string }>({
      query: ({ tenantId, occurrenceId }) => ({
        url: `/api/v1/tenants/${tenantId}/room-occurrences/${occurrenceId}`,
      }),
      providesTags: (_result, _error, { occurrenceId }) => [{ type: 'RoomOccurrence', id: occurrenceId }],
    }),

    createOccurrence: builder.mutation<RoomOccurrenceResponse, TenantScope & CreateOccurrenceRequest>({
      query: ({ tenantId, ...body }) => ({
        url: `/api/v1/tenants/${tenantId}/room-occurrences`,
        method: 'POST',
        body,
      }),
      invalidatesTags: [{ type: 'RoomOccurrence', id: 'LIST' }],
    }),

    updateOccurrence: builder.mutation<RoomOccurrenceResponse, UpdateOccurrenceRequest>({
      query: ({ tenantId, occurrenceId, ...body }) => ({
        url: `/api/v1/tenants/${tenantId}/room-occurrences/${occurrenceId}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: (_result, _error, { occurrenceId }) => [
        { type: 'RoomOccurrence', id: occurrenceId },
        { type: 'RoomOccurrence', id: 'LIST' },
      ],
    }),

    // ── Series ──────────────────────────────────────────────────────────
    getSeries: builder.query<PaginatedResponse<RoomSeriesResponse>, SeriesListParams>({
      query: ({ tenantId, page = 1, pageSize = 20, status }) => ({
        url: `/api/v1/tenants/${tenantId}/room-series`,
        params: { page, pageSize, status },
      }),
      providesTags: (result) =>
        result
          ? [
              ...result.items.map(({ id }) => ({ type: 'RoomSeries' as const, id })),
              { type: 'RoomSeries', id: 'LIST' },
            ]
          : [{ type: 'RoomSeries', id: 'LIST' }],
    }),

    createSeries: builder.mutation<RoomSeriesResponse, TenantScope & CreateSeriesRequest>({
      query: ({ tenantId, ...body }) => ({
        url: `/api/v1/tenants/${tenantId}/room-series`,
        method: 'POST',
        body,
      }),
      invalidatesTags: [
        { type: 'RoomSeries', id: 'LIST' },
        { type: 'RoomOccurrence', id: 'LIST' },
      ],
    }),

    updateSeries: builder.mutation<RoomSeriesResponse, UpdateSeriesRequest>({
      query: ({ tenantId, seriesId, ...body }) => ({
        url: `/api/v1/tenants/${tenantId}/room-series/${seriesId}`,
        method: 'PATCH',
        body,
      }),
      invalidatesTags: (_result, _error, { seriesId }) => [
        { type: 'RoomSeries', id: seriesId },
        { type: 'RoomSeries', id: 'LIST' },
        { type: 'RoomOccurrence', id: 'LIST' },
      ],
    }),

    endSeries: builder.mutation<void, TenantScope & { seriesId: string }>({
      query: ({ tenantId, seriesId }) => ({
        url: `/api/v1/tenants/${tenantId}/room-series/${seriesId}/end`,
        method: 'POST',
      }),
      invalidatesTags: (_result, _error, { seriesId }) => [
        { type: 'RoomSeries', id: seriesId },
        { type: 'RoomSeries', id: 'LIST' },
      ],
    }),

    // ── Analytics ────────────────────────────────────────────────────────
    getAnalytics: builder.query<RoomAnalyticsResponse, TenantScope & { occurrenceId: string }>({
      query: ({ tenantId, occurrenceId }) => ({
        url: `/api/v1/tenants/${tenantId}/room-occurrences/${occurrenceId}/analytics`,
      }),
      providesTags: (_result, _error, { occurrenceId }) => [{ type: 'RoomAnalytics', id: occurrenceId }],
    }),
  }),
});

export const {
  useGetTemplatesQuery,
  useGetTemplateQuery,
  useCreateTemplateMutation,
  useUpdateTemplateMutation,
  useGetOccurrencesQuery,
  useGetOccurrenceQuery,
  useCreateOccurrenceMutation,
  useUpdateOccurrenceMutation,
  useGetSeriesQuery,
  useCreateSeriesMutation,
  useUpdateSeriesMutation,
  useEndSeriesMutation,
  useGetAnalyticsQuery,
} = roomsApi;
