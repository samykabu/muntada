import { useState } from 'react';
import { useGetMembersQuery, useUpdateRoleMutation, useRemoveMemberMutation } from '../api/memberApi';
import type { MemberRole } from '../api/memberApi';

interface MemberListProps {
  tenantId: string;
  onInvite: () => void;
}

const ROLES: MemberRole[] = ['Owner', 'Admin', 'Member'];
const PAGE_SIZE = 20;

const roleBadgeColors: Record<MemberRole, string> = {
  Owner: '#7c3aed',
  Admin: '#2563eb',
  Member: '#4b5563',
};

/** Table of tenant members with role editing and removal actions. */
export function MemberList({ tenantId, onInvite }: MemberListProps) {
  const [page, setPage] = useState(1);
  const { data, isLoading, error } = useGetMembersQuery({ tenantId, page, pageSize: PAGE_SIZE });
  const [updateRole] = useUpdateRoleMutation();
  const [removeMember] = useRemoveMemberMutation();
  const [confirmRemoveId, setConfirmRemoveId] = useState<string | null>(null);

  const handleRoleChange = async (memberId: string, role: MemberRole) => {
    await updateRole({ tenantId, memberId, role });
  };

  const handleRemove = async (memberId: string) => {
    await removeMember({ tenantId, memberId });
    setConfirmRemoveId(null);
  };

  if (isLoading) return <p>Loading members...</p>;
  if (error) return <p style={{ color: 'red' }}>Failed to load members.</p>;
  if (!data) return null;

  const totalPages = Math.ceil(data.totalCount / PAGE_SIZE);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h3>Members ({data.totalCount})</h3>
        <button type="button" onClick={onInvite}>Invite Member</button>
      </div>

      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr style={{ borderBottom: '2px solid #e5e7eb', textAlign: 'left' }}>
            <th style={{ padding: '0.5rem' }}>Name / Email</th>
            <th style={{ padding: '0.5rem' }}>Role</th>
            <th style={{ padding: '0.5rem' }}>Status</th>
            <th style={{ padding: '0.5rem' }}>Joined</th>
            <th style={{ padding: '0.5rem' }}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {data.items.map((member) => (
            <tr key={member.id} style={{ borderBottom: '1px solid #f3f4f6' }}>
              <td style={{ padding: '0.5rem' }}>
                <div style={{ fontWeight: 500 }}>{member.displayName}</div>
                <div style={{ fontSize: '0.8rem', color: '#6b7280' }}>{member.email}</div>
              </td>
              <td style={{ padding: '0.5rem' }}>
                <span style={{
                  display: 'inline-block',
                  padding: '2px 8px',
                  borderRadius: 12,
                  fontSize: '0.75rem',
                  color: '#fff',
                  backgroundColor: roleBadgeColors[member.role],
                }}>
                  {member.role}
                </span>
              </td>
              <td style={{ padding: '0.5rem' }}>{member.status}</td>
              <td style={{ padding: '0.5rem' }}>{new Date(member.joinedAt).toLocaleDateString()}</td>
              <td style={{ padding: '0.5rem' }}>
                <select
                  value={member.role}
                  onChange={(e) => handleRoleChange(member.id, e.target.value as MemberRole)}
                  style={{ marginRight: 8 }}
                  aria-label={`Change role for ${member.displayName}`}
                >
                  {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
                </select>
                {confirmRemoveId === member.id ? (
                  <>
                    <button type="button" onClick={() => handleRemove(member.id)} style={{ color: 'red', marginRight: 4 }}>Confirm</button>
                    <button type="button" onClick={() => setConfirmRemoveId(null)}>Cancel</button>
                  </>
                ) : (
                  <button type="button" onClick={() => setConfirmRemoveId(member.id)} style={{ color: 'red' }}>Remove</button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {totalPages > 1 && (
        <div style={{ display: 'flex', gap: 8, justifyContent: 'center', marginTop: '1rem' }}>
          <button type="button" disabled={page === 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
          <span>Page {page} of {totalPages}</span>
          <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Next</button>
        </div>
      )}
    </div>
  );
}
