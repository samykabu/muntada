import { useState, type FormEvent } from 'react';
import { useForgotPasswordMutation } from '../api/authApi';

/** Forgot password page — requests a password reset email. */
export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [forgotPassword, { isLoading }] = useForgotPasswordMutation();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    await forgotPassword({ email });
    setSent(true);
  };

  if (sent) {
    return (
      <div style={{ maxWidth: 400, margin: '2rem auto', padding: '1rem' }}>
        <h1>Check Your Email</h1>
        <p>If an account exists for that email, we've sent a password reset link.</p>
        <a href="/login">Back to Sign In</a>
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
      <p><a href="/login">Back to Sign In</a></p>
    </div>
  );
}
