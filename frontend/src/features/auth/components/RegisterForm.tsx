import { useState, type FormEvent } from 'react';

interface RegisterFormProps {
  onSubmit: (email: string, password: string, confirmPassword: string) => Promise<void>;
  isLoading?: boolean;
  error?: string | null;
}

/** Reusable registration form component (Constitution IX: Component Reusability). */
export function RegisterForm({ onSubmit, isLoading, error }: RegisterFormProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    await onSubmit(email, password, confirmPassword);
  };

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label htmlFor="email">Email</label>
        <input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
      </div>
      <div>
        <label htmlFor="password">Password</label>
        <input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={12} />
        <small>Min 12 chars, 1 uppercase, 1 digit, 1 special character</small>
      </div>
      <div>
        <label htmlFor="confirmPassword">Confirm Password</label>
        <input id="confirmPassword" type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required />
      </div>
      {error && <p role="alert" style={{ color: 'red' }}>{error}</p>}
      <button type="submit" disabled={isLoading}>{isLoading ? 'Creating account...' : 'Register'}</button>
    </form>
  );
}
