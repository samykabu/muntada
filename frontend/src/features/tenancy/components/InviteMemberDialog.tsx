import { useState, type FormEvent } from 'react';
import { useInviteMemberMutation } from '../api/memberApi';
import type { MemberRole } from '../api/memberApi';

interface InviteMemberDialogProps {
  tenantId: string;
  open: boolean;
  onClose: () => void;
}

const ROLES: MemberRole[] = ['Owner', 'Admin', 'Member'];

/** Modal dialog for inviting a new member to the tenant. */
export function InviteMemberDialog({ tenantId, open, onClose }: InviteMemberDialogProps) {
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<MemberRole>('Member');
  const [message, setMessage] = useState('');
  const [inviteMember, { isLoading, error, isSuccess }] = useInviteMemberMutation();

  if (!open) return null;

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    await inviteMember({ tenantId, email, role, message: message || undefined });
  };

  const handleClose = () => {
    setEmail('');
    setRole('Member');
    setMessage('');
    onClose();
  };

  return (
    <div
      style={{
        position: 'fixed', inset: 0, backgroundColor: 'rgba(0,0,0,0.5)',
        display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000,
      }}
      onClick={handleClose}
      role="dialog"
      aria-modal="true"
      aria-label="Invite member"
    >
      <div
        style={{ backgroundColor: '#fff', borderRadius: 8, padding: '1.5rem', width: 400, maxWidth: '90vw' }}
        onClick={(e) => e.stopPropagation()}
      >
        <h3 style={{ marginTop: 0 }}>Invite Member</h3>

        {isSuccess ? (
          <div>
            <p style={{ color: 'green' }}>Invitation sent successfully.</p>
            <button type="button" onClick={handleClose}>Close</button>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div style={{ marginBottom: '0.75rem' }}>
              <label htmlFor="invite-email" style={{ display: 'block', marginBottom: 4, fontSize: '0.875rem' }}>Email</label>
              <input
                id="invite-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                style={{ width: '100%' }}
                placeholder="colleague@example.com"
              />
            </div>

            <div style={{ marginBottom: '0.75rem' }}>
              <label htmlFor="invite-role" style={{ display: 'block', marginBottom: 4, fontSize: '0.875rem' }}>Role</label>
              <select id="invite-role" value={role} onChange={(e) => setRole(e.target.value as MemberRole)} style={{ width: '100%' }}>
                {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
              </select>
            </div>

            <div style={{ marginBottom: '0.75rem' }}>
              <label htmlFor="invite-message" style={{ display: 'block', marginBottom: 4, fontSize: '0.875rem' }}>Message (optional)</label>
              <textarea
                id="invite-message"
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                rows={3}
                style={{ width: '100%' }}
                placeholder="Join our team on Muntada!"
              />
            </div>

            {error && <p role="alert" style={{ color: 'red', fontSize: '0.875rem' }}>Failed to send invitation. Please try again.</p>}

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
              <button type="button" onClick={handleClose}>Cancel</button>
              <button type="submit" disabled={isLoading}>{isLoading ? 'Sending...' : 'Send Invite'}</button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
