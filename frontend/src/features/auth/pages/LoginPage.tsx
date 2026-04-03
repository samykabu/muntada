import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { LoginForm } from '../components/LoginForm';
import { useAuth } from '../hooks/useAuth';

/** Login page — authenticates user with email + password. */
export function LoginPage() {
  const { login, isLoggingIn } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  const handleSubmit = async (email: string, password: string) => {
    try {
      setError(null);
      await login(email, password);
      navigate('/');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Invalid email or password');
    }
  };

  return (
    <div style={{ maxWidth: 400, margin: '2rem auto', padding: '1rem' }}>
      <h1>Sign In</h1>
      <LoginForm onSubmit={handleSubmit} isLoading={isLoggingIn} error={error} onForgotPassword={() => navigate('/forgot-password')} />
      <p>Don't have an account? <a href="/register">Create one</a></p>
      <p><a href="/login/otp">Sign in with phone</a></p>
    </div>
  );
}
