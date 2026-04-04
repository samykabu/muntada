import { useEffect, useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { useAcceptInviteMutation } from '../api/memberApi';

/**
 * Simple page that reads a ?token= query parameter and calls the accept-invite API.
 * Typically accessed via an email invitation link.
 */
export function AcceptInvitePage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const tenantId = searchParams.get('tenantId') ?? '';
  const [acceptInvite, { isLoading, isSuccess, error }] = useAcceptInviteMutation();
  const [attempted, setAttempted] = useState(false);

  useEffect(() => {
    if (token && tenantId && !attempted) {
      setAttempted(true);
      acceptInvite({ tenantId, token });
    }
  }, [token, tenantId, attempted, acceptInvite]);

  return (
    <div style={{ maxWidth: 400, margin: '3rem auto', padding: '1rem', fontFamily: 'system-ui, sans-serif', textAlign: 'center' }}>
      <h1>Join Organization</h1>

      {!token && (
        <p style={{ color: 'red' }}>Invalid invitation link. No token was provided.</p>
      )}

      {isLoading && <p>Accepting invitation...</p>}

      {isSuccess && (
        <div>
          <p style={{ color: 'green' }}>You have successfully joined the organization.</p>
          <Link to="/">Go to Dashboard</Link>
        </div>
      )}

      {error && (
        <div>
          <p style={{ color: 'red' }}>Failed to accept the invitation. The link may have expired or already been used.</p>
          <Link to="/">Go to Home</Link>
        </div>
      )}
    </div>
  );
}
