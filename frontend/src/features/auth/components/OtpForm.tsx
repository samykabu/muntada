import { useState, type FormEvent } from 'react';

interface OtpFormProps {
  onRequestCode: (phoneNumber: string) => Promise<void>;
  onVerify: (code: string) => Promise<void>;
  isRequesting?: boolean;
  isVerifying?: boolean;
  error?: string | null;
  challengeId?: string | null;
}

/** Reusable OTP form with phone input and 6-digit code verification (Constitution IX). */
export function OtpForm({ onRequestCode, onVerify, isRequesting, isVerifying, error, challengeId }: OtpFormProps) {
  const [phone, setPhone] = useState('');
  const [code, setCode] = useState('');

  const handleRequestCode = async (e: FormEvent) => {
    e.preventDefault();
    await onRequestCode(phone);
  };

  const handleVerify = async (e: FormEvent) => {
    e.preventDefault();
    await onVerify(code);
  };

  if (!challengeId) {
    return (
      <form onSubmit={handleRequestCode}>
        <div>
          <label htmlFor="phone">Phone Number</label>
          <input id="phone" type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+966501234567" required />
          <small>E.164 format (e.g., +966501234567)</small>
        </div>
        {error && <p role="alert" style={{ color: 'red' }}>{error}</p>}
        <button type="submit" disabled={isRequesting}>{isRequesting ? 'Sending...' : 'Send Code'}</button>
      </form>
    );
  }

  return (
    <form onSubmit={handleVerify}>
      <div>
        <label htmlFor="otp-code">Verification Code</label>
        <input id="otp-code" type="text" value={code} onChange={(e) => setCode(e.target.value)} maxLength={6} pattern="[0-9]{6}" required />
        <small>Enter the 6-digit code sent to your phone</small>
      </div>
      {error && <p role="alert" style={{ color: 'red' }}>{error}</p>}
      <button type="submit" disabled={isVerifying}>{isVerifying ? 'Verifying...' : 'Verify'}</button>
    </form>
  );
}
