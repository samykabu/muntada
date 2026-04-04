import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { AuthProvider } from './features/auth/context/AuthContext';
import { RegisterPage } from './features/auth/pages/RegisterPage';
import { LoginPage } from './features/auth/pages/LoginPage';
import { OtpLoginPage } from './features/auth/pages/OtpLoginPage';
import { ForgotPasswordPage } from './features/auth/pages/ForgotPasswordPage';
import { ResetPasswordPage } from './features/auth/pages/ResetPasswordPage';
import { TenantProvider } from './features/tenancy/hooks/useTenant';
import { CreateTenantPage } from './features/tenancy/pages/CreateTenantPage';
import { TenantSettingsPage } from './features/tenancy/pages/TenantSettingsPage';
import { UsageDashboardPage } from './features/tenancy/pages/UsageDashboardPage';
import { AcceptInvitePage } from './features/tenancy/pages/AcceptInvitePage';

/**
 * Root application component with React Router and auth routes.
 * Feature modules add their routes as they're built.
 */
function App() {
  return (
    <AuthProvider>
      <TenantProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/login/otp" element={<OtpLoginPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            {/* Tenancy routes */}
            <Route path="/create-tenant" element={<CreateTenantPage />} />
            <Route path="/tenant/settings" element={<TenantSettingsPage />} />
            <Route path="/tenant/usage" element={<UsageDashboardPage />} />
            <Route path="/join-tenant" element={<AcceptInvitePage />} />
          </Routes>
        </BrowserRouter>
      </TenantProvider>
    </AuthProvider>
  );
}

/** Placeholder home page. */
function Home() {
  return (
    <div style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>Muntada</h1>
      <p>Platform is running. <Link to="/login">Sign in</Link> or <Link to="/register">create an account</Link>.</p>
    </div>
  );
}

export default App;
