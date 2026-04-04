import { baseApi } from '../../../shared/api/baseApi';

/** Member roles within a tenant. */
export type MemberRole = 'Owner' | 'Admin' | 'Member';

/** Member status within a tenant. */
export type MemberStatus = 'Active' | 'Pending' | 'Inactive';

/** Member response from the API. */
export interface MemberResponse {
  id: string;
  userId: string;
  email: string;
  displayName?: string | null;
  role: MemberRole;
  status: MemberStatus;
  joinedAt?: string | null;
}

/** Paginated list of members. */
export interface MemberListResponse {
  items: MemberResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Params for listing members (pagination + optional search). */
export interface GetMembersParams {
  tenantId: string;
  page?: number;
  pageSize?: number;
  search?: string;
}

/** Invite member request. */
export interface InviteMemberRequest {
  tenantId: string;
  email: string;
  role: MemberRole;
  message?: string;
}

/** Accept invite request. */
export interface AcceptInviteRequest {
  tenantId: string;
  token: string;
}

/** Update role request. */
export interface UpdateRoleRequest {
  tenantId: string;
  memberId: string;
  role: MemberRole;
}

/** Remove member request. */
export interface RemoveMemberRequest {
  tenantId: string;
  memberId: string;
}

/** RTK Query API slice for tenant member management endpoints. */
export const memberApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getMembers: builder.query<MemberListResponse, GetMembersParams>({
      query: ({ tenantId, page = 1, pageSize = 20, search }) => ({
        url: `/api/v1/tenants/${tenantId}/members`,
        params: { page, pageSize, ...(search ? { search } : {}) },
      }),
      providesTags: (_result, _error, { tenantId }) => [{ type: 'Members', id: tenantId }],
    }),
    inviteMember: builder.mutation<void, InviteMemberRequest>({
      query: ({ tenantId, ...body }) => ({
        url: `/api/v1/tenants/${tenantId}/members/invite`,
        method: 'POST',
        body,
      }),
      invalidatesTags: (_result, _error, { tenantId }) => [{ type: 'Members', id: tenantId }],
    }),
    acceptInvite: builder.mutation<void, AcceptInviteRequest>({
      query: ({ tenantId, token }) => ({
        url: `/api/v1/tenants/${tenantId}/members/accept`,
        method: 'POST',
        body: { token },
      }),
    }),
    updateRole: builder.mutation<void, UpdateRoleRequest>({
      query: ({ tenantId, memberId, role }) => ({
        url: `/api/v1/tenants/${tenantId}/members/${memberId}/role`,
        method: 'PATCH',
        body: { role },
      }),
      invalidatesTags: (_result, _error, { tenantId }) => [{ type: 'Members', id: tenantId }],
    }),
    removeMember: builder.mutation<void, RemoveMemberRequest>({
      query: ({ tenantId, memberId }) => ({
        url: `/api/v1/tenants/${tenantId}/members/${memberId}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, { tenantId }) => [{ type: 'Members', id: tenantId }],
    }),
  }),
});

export const {
  useGetMembersQuery,
  useInviteMemberMutation,
  useAcceptInviteMutation,
  useUpdateRoleMutation,
  useRemoveMemberMutation,
} = memberApi;
