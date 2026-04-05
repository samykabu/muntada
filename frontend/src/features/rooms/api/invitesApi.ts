import { baseApi } from '../../../shared/api/baseApi';

// ── Types ─────────────────────────────────────────────────────────────────────

export type RoomInviteStatus = 'Pending' | 'Accepted' | 'Revoked' | 'Expired';
export type RoomInviteType = 'Email' | 'DirectLink' | 'GuestMagicLink';

export interface RoomInviteResponse {
  id: string;
  roomOccurrenceId: string;
  invitedEmail?: string;
  invitedUserId?: string;
  inviteToken: string;
  status: RoomInviteStatus;
  inviteType: RoomInviteType;
  invitedBy: string;
  createdAt: string;
  expiresAt: string;
}

export interface CreateInvitesRequest {
  tenantId: string;
  occurrenceId: string;
  invites: Array<{
    email?: string;
    userId?: string;
    inviteType: RoomInviteType;
  }>;
}

export interface JoinRoomRequest {
  tenantId: string;
  occurrenceId: string;
  inviteToken: string;
  displayName: string;
}

export interface JoinRoomResponse {
  participantId: string;
  occurrenceId: string;
  displayName: string;
  role: string;
  status: string;
}

export interface PaginatedInvites {
  items: RoomInviteResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ── RTK Query API ─────────────────────────────────────────────────────────────

/** RTK Query API slice for room invite endpoints. */
export const invitesApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getInvites: builder.query<PaginatedInvites, {
      tenantId: string;
      occurrenceId: string;
      page?: number;
      pageSize?: number;
      status?: RoomInviteStatus;
    }>({
      query: ({ tenantId, occurrenceId, page = 1, pageSize = 20, status }) => ({
        url: `/api/v1/tenants/${tenantId}/room-occurrences/${occurrenceId}/invites`,
        params: { page, pageSize, status },
      }),
      providesTags: (result, _error, { occurrenceId }) =>
        result
          ? [
              ...result.items.map(({ id }) => ({ type: 'RoomInvite' as const, id })),
              { type: 'RoomInvite', id: `LIST-${occurrenceId}` },
            ]
          : [{ type: 'RoomInvite', id: `LIST-${occurrenceId}` }],
    }),

    createInvites: builder.mutation<RoomInviteResponse[], CreateInvitesRequest>({
      query: ({ tenantId, occurrenceId, invites }) => ({
        url: `/api/v1/tenants/${tenantId}/room-occurrences/${occurrenceId}/invites`,
        method: 'POST',
        body: { invites },
      }),
      invalidatesTags: (_result, _error, { occurrenceId }) => [
        { type: 'RoomInvite', id: `LIST-${occurrenceId}` },
      ],
    }),

    revokeInvite: builder.mutation<void, {
      tenantId: string;
      occurrenceId: string;
      inviteId: string;
    }>({
      query: ({ tenantId, occurrenceId, inviteId }) => ({
        url: `/api/v1/tenants/${tenantId}/room-occurrences/${occurrenceId}/invites/${inviteId}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, { occurrenceId, inviteId }) => [
        { type: 'RoomInvite', id: inviteId },
        { type: 'RoomInvite', id: `LIST-${occurrenceId}` },
      ],
    }),

    joinRoom: builder.mutation<JoinRoomResponse, JoinRoomRequest>({
      query: ({ tenantId, occurrenceId, inviteToken, displayName }) => ({
        url: `/api/v1/tenants/${tenantId}/room-occurrences/${occurrenceId}/invites/join`,
        method: 'POST',
        body: { token: inviteToken, displayName },
      }),
      invalidatesTags: (_result, _error, { occurrenceId }) => [
        { type: 'RoomOccurrence', id: occurrenceId },
      ],
    }),
  }),
});

export const {
  useGetInvitesQuery,
  useCreateInvitesMutation,
  useRevokeInviteMutation,
  useJoinRoomMutation,
} = invitesApi;
