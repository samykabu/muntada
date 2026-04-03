import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { OtpForm } from '../components/OtpForm';
import { useRequestOtpMutation, useVerifyOtpMutation } from '../api/otpApi';
import { useAuthContext } from '../context/AuthContext';

/** OTP login page — authenticates via phone number and SMS code. */
export function OtpLoginPage() {
  const [challengeId, setChallengeId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [requestOtp, { isLoading: isRequesting }] = useRequestOtpMutation();
  const [verifyOtp, { isLoading: isVerifying }] = useVerifyOtpMutation();
  const auth = useAuthContext();
  const navigate = useNavigate();

  const handleRequestCode = async (phoneNumber: string) => {
    try {
      setError(null);
      const result = await requestOtp({ phoneNumber }).unwrap();
      setChallengeId(result.challengeId);
    } catch {
      setError('Failed to send code');
    }
  };

  const handleVerify = async (code: string) => {
    if (!challengeId) return;
    try {
      setError(null);
      const result = await verifyOtp({ challengeId, code }).unwrap();
      auth.setAuth(result.accessToken, result.userId);
      navigate('/');
    } catch {
      setError('Invalid code');
    }
  };

  return (
    <div style={{ maxWidth: 400, margin: '2rem auto', padding: '1rem' }}>
      <h1>Sign In with Phone</h1>
      <OtpForm onRequestCode={handleRequestCode} onVerify={handleVerify} isRequesting={isRequesting} isVerifying={isVerifying} error={error} challengeId={challengeId} />
      <p><a href="/login">Sign in with email instead</a></p>
    </div>
  );
}
