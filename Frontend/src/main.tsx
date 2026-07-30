import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import './index.css'
import { StaticLanding } from './StaticLanding'
import { ThemeProvider } from './theme/ThemeProvider'
import { AuthProvider } from './auth/AuthContext'
import { RequireAuth } from './auth/RequireAuth'
import { RequireStore } from './auth/RequireStore'
import { RequireAdmin } from './auth/RequireAdmin'
import { RequireOwner } from './auth/RequireOwner'
<<<<<<< HEAD
import { LoginPage, RegisterPage } from './auth/AuthPage'
=======
import { LoginPage } from './auth/LoginPage'
import { ForgotPasswordPage } from './auth/ForgotPasswordPage'
import { ResetPasswordPage } from './auth/ResetPasswordPage'
>>>>>>> e8d0e80ea06cd763aa0d4d1bd503a507a7ec321b
import { AdminLayout } from './admin/AdminLayout'
import { StoreOnboardingPage } from './admin/pages/StoreOnboardingPage'
import { ModerationPage } from './admin/pages/ModerationPage'
import { DashboardPage } from './admin/pages/DashboardPage'
import { PosPage } from './admin/pages/PosPage'
import { InventoryPage } from './admin/pages/InventoryPage'
import { SupplyPage } from './admin/pages/SupplyPage'
import { MarketingPage } from './admin/pages/MarketingPage'
import { StaffPage } from './admin/pages/StaffPage'
import { ReportsPage } from './admin/pages/ReportsPage'
import { SettingsPage } from './admin/pages/SettingsPage'
import { CustomerDisplayPage } from './admin/pages/CustomerDisplayPage'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<StaticLanding />} />
            <Route path="/login" element={<LoginPage />} />
<<<<<<< HEAD
            <Route path="/register" element={<RegisterPage />} />
=======
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
>>>>>>> e8d0e80ea06cd763aa0d4d1bd503a507a7ec321b
            <Route path="/admin" element={<RequireAuth />}>
              <Route path="onboarding" element={<StoreOnboardingPage />} />
              <Route element={<RequireAdmin />}>
                <Route path="moderation" element={<ModerationPage />} />
              </Route>
              <Route element={<RequireStore />}>
                <Route path="pos/display" element={<CustomerDisplayPage />} />
                <Route element={<AdminLayout />}>
                  <Route path="pos" element={<PosPage />} />
                  <Route path="inventory" element={<InventoryPage />} />
                  <Route element={<RequireOwner />}>
                    <Route index element={<DashboardPage />} />
                    <Route path="supply" element={<SupplyPage />} />
                    <Route path="marketing" element={<MarketingPage />} />
                    <Route path="staff" element={<StaffPage />} />
                    <Route path="reports" element={<ReportsPage />} />
                    <Route path="settings" element={<SettingsPage />} />
                  </Route>
                </Route>
              </Route>
            </Route>
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  </StrictMode>,
)
