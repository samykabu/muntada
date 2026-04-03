import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useForgotPasswordMutation } from '../api/authApi';

/** Forgot password page — requests a password reset email. */
export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [forgotPassword, { isLoading }] = useForgotPasswordMutation();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    try {
      await forgotPassword({ email }).unwrap();
    } catch {
      // Generic response per FR-018 — always show success regardless of outcome
    }
    setSent(true);
  };

  if (sent) {
    return (
      <div style={{ maxWidth: 400, margin: '2rem auto', padding: '1rem' }}>
        <h1>Check Your Email</h1>
        <p>If an account exists for that email, we've sent a password reset link.</p>
        <Link to="/login">Back to Sign In</Link>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 400, margin: '2rem auto', padding: '1rem' }}>
      <h1>Forgot Password</h1>
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="forgot-email">Email</label>
          <input id="forgot-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </div>
        <button type="submit" disabled={isLoading}>{isLoading ? 'Sending...' : 'Send Reset Link'}</button>
      </form>
      <p><Link to="/login">Back to Sign In</Link></p>
    </div>
  );
}
