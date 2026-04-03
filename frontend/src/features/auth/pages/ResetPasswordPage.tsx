import { useState, type FormEvent } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useResetPasswordMutation } from '../api/authApi';

/** Reset password page — entered via email link with token. */
export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [resetPassword, { isLoading }] = useResetPasswordMutation();
  const navigate = useNavigate();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (password !== confirmPassword) { setError('Passwords do not match'); return; }
    try {
      setError(null);
      await resetPassword({ token, password, confirmPassword }).unwrap();
      navigate('/login', { state: { message: 'Password reset! You can now sign in.' } });
    } catch {
      setError('Reset failed. The link may have expired.');
    }
  };

  return (
    <div style={{ maxWidth: 400, margin: '2rem auto', padding: '1rem' }}>
      <h1>Reset Password</h1>
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="new-password">New Password</label>
          <input id="new-password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={12} />
        </div>
        <div>
          <label htmlFor="confirm-new-password">Confirm Password</label>
          <input id="confirm-new-password" type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required />
        </div>
        {error && <p role="alert" style={{ color: 'red' }}>{error}</p>}
        <button type="submit" disabled={isLoading}>{isLoading ? 'Resetting...' : 'Reset Password'}</button>
      </form>
    </div>
  );
}
