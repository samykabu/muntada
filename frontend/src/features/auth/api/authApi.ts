import { baseApi } from '../../../shared/api/baseApi';

/** RTK Query API slice for authentication endpoints. */
export const authApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    register: builder.mutation<{ userId: string; email: string }, { email: string; password: string; confirmPassword: string }>({
      query: (body) => ({ url: '/api/v1/identity/auth/register', method: 'POST', body }),
    }),
    login: builder.mutation<{ accessToken: string; expiresIn: number; tokenType: string; userId: string }, { email: string; password: string }>({
      query: (body) => ({ url: '/api/v1/identity/auth/login', method: 'POST', body }),
    }),
    verifyEmail: builder.mutation<boolean, { token: string }>({
      query: (body) => ({ url: '/api/v1/identity/auth/verify-email', method: 'POST', body }),
    }),
    resendVerification: builder.mutation<boolean, { email: string }>({
      query: (body) => ({ url: '/api/v1/identity/auth/resend-verification', method: 'POST', body }),
    }),
    refresh: builder.mutation<{ accessToken: string; expiresIn: number }, void>({
      query: () => ({ url: '/api/v1/identity/auth/refresh', method: 'POST' }),
    }),
    logout: builder.mutation<void, void>({
      query: () => ({ url: '/api/v1/identity/auth/logout', method: 'POST' }),
    }),
    forgotPassword: builder.mutation<void, { email: string }>({
      query: (body) => ({ url: '/api/v1/identity/auth/forgot-password', method: 'POST', body }),
    }),
    resetPassword: builder.mutation<void, { token: string; password: string; confirmPassword: string }>({
      query: (body) => ({ url: '/api/v1/identity/auth/reset-password', method: 'POST', body }),
    }),
  }),
});

export const {
  useRegisterMutation,
  useLoginMutation,
  useVerifyEmailMutation,
  useResendVerificationMutation,
  useRefreshMutation,
  useLogoutMutation,
  useForgotPasswordMutation,
  useResetPasswordMutation,
} = authApi;
