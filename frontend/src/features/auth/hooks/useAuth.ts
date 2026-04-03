import { useAuthContext } from '../context/AuthContext';
import { useLoginMutation, useRegisterMutation, useLogoutMutation } from '../api/authApi';

/** Typed auth hook providing login, register, logout actions with state. */
export function useAuth() {
  const auth = useAuthContext();
  const [loginMutation, loginState] = useLoginMutation();
  const [registerMutation, registerState] = useRegisterMutation();
  const [logoutMutation] = useLogoutMutation();

  const login = async (email: string, password: string) => {
    const result = await loginMutation({ email, password }).unwrap();
    auth.setAuth(result.accessToken, result.userId);
    return result;
  };

  const register = async (email: string, password: string, confirmPassword: string) => {
    return registerMutation({ email, password, confirmPassword }).unwrap();
  };

  const logout = async () => {
    try {
      await logoutMutation().unwrap();
    } finally {
      auth.clearAuth();
    }
  };

  return {
    ...auth,
    login,
    register,
    logout,
    isLoggingIn: loginState.isLoading,
    isRegistering: registerState.isLoading,
  };
}
