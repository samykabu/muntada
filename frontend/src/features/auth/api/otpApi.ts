import { baseApi } from '../../../shared/api/baseApi';

/** RTK Query API slice for OTP authentication endpoints. */
export const otpApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    requestOtp: builder.mutation<{ challengeId: string }, { phoneNumber: string }>({
      query: (body) => ({ url: '/api/v1/identity/auth/otp/challenge', method: 'POST', body }),
    }),
    verifyOtp: builder.mutation<{ accessToken: string; expiresIn: number; userId: string }, { challengeId: string; code: string }>({
      query: (body) => ({ url: '/api/v1/identity/auth/otp/verify', method: 'POST', body }),
    }),
  }),
});

export const { useRequestOtpMutation, useVerifyOtpMutation } = otpApi;
