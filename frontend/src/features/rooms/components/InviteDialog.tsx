import { useState, type FormEvent } from 'react';
import { useCreateInvitesMutation } from '../api/invitesApi';
import type { RoomInviteType } from '../api/invitesApi';

interface InviteDialogProps {
  tenantId: string;
  occurrenceId: string;
  /** Direct join link for copying to clipboard. */
  joinLink: string;
  /** Whether guest access is enabled for this room. */
  allowGuestAccess: boolean;
  /** Called when the dialog should close. */
  onClose: () => void;
}

/** Dialog for sending room invites via email, copying links, and generating guest links. */
export function InviteDialog({
  tenantId,
  occurrenceId,
  joinLink,
  allowGuestAccess,
  onClose,
}: InviteDialogProps) {
  const [emails, setEmails] = useState('');
  const [copied, setCopied] = useState(false);
  const [guestLinkCopied, setGuestLinkCopied] = useState(false);
  const [createInvites, { isLoading, error }] = useCreateInvitesMutation();

  const guestLink = `${joinLink}?guest=true`;

  const handleSendEmails = async (e: FormEvent) => {
    e.preventDefault();
    const emailList = emails
      .split(/[,;\n]+/)
      .map((s) => s.trim())
      .filter((s) => s.length > 0);

    if (emailList.length === 0) return;

    try {
      await createInvites({
        tenantId,
        occurrenceId,
        invites: emailList.map((email) => ({
          email,
          inviteType: 'Email' as RoomInviteType,
        })),
      }).unwrap();
      setEmails('');
    } catch {
      // Error rendered from RTK Query state
    }
  };

  const handleCopyLink = async () => {
    try {
      await navigator.clipboard.writeText(joinLink);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy link to clipboard:', err);
      alert('Failed to copy link. Please copy it manually.');
    }
  };

  const handleCopyGuestLink = async () => {
    try {
      await navigator.clipboard.writeText(guestLink);
      setGuestLinkCopied(true);
      setTimeout(() => setGuestLinkCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy guest link to clipboard:', err);
      alert('Failed to copy guest link. Please copy it manually.');
    }
  };

  return (
    <div
      role="dialog"
      aria-label="Invite participants"
      style={{
        position: 'fixed',
        inset: 0,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'rgba(0,0,0,0.4)',
        zIndex: 1000,
      }}
    >
      <div
        style={{
          background: '#fff',
          borderRadius: 8,
          padding: '1.5rem',
          maxWidth: 480,
          width: '100%',
          boxShadow: '0 4px 24px rgba(0,0,0,0.15)',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <h2 style={{ margin: 0 }}>Invite Participants</h2>
          <button type="button" onClick={onClose} aria-label="Close dialog" style={{ background: 'none', border: 'none', fontSize: '1.25rem', cursor: 'pointer' }}>
            &times;
          </button>
        </div>

        {/* Email Invites */}
        <form onSubmit={handleSendEmails} style={{ marginBottom: '1.5rem' }}>
          <label htmlFor="invite-emails" style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>
            Email Addresses
          </label>
          <textarea
            id="invite-emails"
            value={emails}
            onChange={(e) => setEmails(e.target.value)}
            placeholder="Enter emails separated by commas or new lines..."
            rows={3}
            style={{ width: '100%', marginBottom: 8 }}
          />
          {error && <p style={{ color: 'red', fontSize: '0.8rem', margin: '0 0 8px' }}>Failed to send invites. Please try again.</p>}
          <button type="submit" disabled={isLoading || emails.trim().length === 0} style={{ width: '100%', padding: '0.5rem' }}>
            {isLoading ? 'Sending...' : 'Send Invites'}
          </button>
        </form>

        {/* Direct Link */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>Direct Join Link</label>
          <div style={{ display: 'flex', gap: 8 }}>
            <input
              type="text"
              value={joinLink}
              readOnly
              style={{ flex: 1, color: '#6b7280', fontSize: '0.875rem' }}
            />
            <button type="button" onClick={handleCopyLink} style={{ whiteSpace: 'nowrap' }}>
              {copied ? 'Copied!' : 'Copy Link'}
            </button>
          </div>
        </div>

        {/* Guest Magic Link */}
        {allowGuestAccess && (
          <div>
            <label style={{ display: 'block', fontWeight: 500, marginBottom: 4 }}>Guest Link (Listen-only)</label>
            <div style={{ display: 'flex', gap: 8 }}>
              <input
                type="text"
                value={guestLink}
                readOnly
                style={{ flex: 1, color: '#6b7280', fontSize: '0.875rem' }}
              />
              <button type="button" onClick={handleCopyGuestLink} style={{ whiteSpace: 'nowrap' }}>
                {guestLinkCopied ? 'Copied!' : 'Copy Guest Link'}
              </button>
            </div>
            <p style={{ fontSize: '0.75rem', color: '#9ca3af', margin: '4px 0 0' }}>
              Guests can join without an account. They have listen-only permissions.
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
