import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { RegisterForm } from '../components/RegisterForm';
import { useAuth } from '../hooks/useAuth';

/** Registration page — creates a new user account. */
export function RegisterPage() {
  const { register, isRegistering } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  const handleSubmit = async (email: string, password: string, confirmPassword: string) => {
    try {
      setError(null);
      await register(email, password, confirmPassword);
      navigate('/login', { state: { message: 'Account created! Check your email to verify.' } });
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Registration failed');
    }
  };

  return (
    <div style={{ maxWidth: 400, margin: '2rem auto', padding: '1rem' }}>
      <h1>Create Account</h1>
      <RegisterForm onSubmit={handleSubmit} isLoading={isRegistering} error={error} />
      <p>Already have an account? <a href="/login">Sign in</a></p>
    </div>
  );
}
