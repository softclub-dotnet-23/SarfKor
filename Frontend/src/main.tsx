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
import { LoginPage, RegisterPage } from './auth/AuthPage'
import { ForgotPasswordPage } from './auth/ForgotPasswordPage'
import { ResetPasswordPage } from './auth/ResetPasswordPage'
import { AcceptInvitePage } from './auth/AcceptInvitePage'
import { AppShell } from './app/AppShell'
import { HomePage } from './app/pages/HomePage'
import { ScanPage } from './app/pages/ScanPage'
import { ProductPage } from './app/pages/ProductPage'
import { ListsPage } from './app/pages/ListsPage'
import { FavoritesPage } from './app/pages/FavoritesPage'
import { AlertsPage } from './app/pages/AlertsPage'
import { ProfilePage } from './app/pages/ProfilePage'
import { SettingsPage as AppSettingsPage } from './app/pages/SettingsPage'
import { CabinetShell } from './admin/cabinet/CabinetShell'
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
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            <Route path="/accept-invite" element={<AcceptInvitePage />} />
            {/* Consumer app. Reuses the existing RequireAuth guard rather than
                introducing a second notion of "signed in". */}
            <Route path="/app" element={<RequireAuth />}>
              <Route element={<AppShell />}>
                <Route index element={<HomePage />} />
                <Route path="scan" element={<ScanPage />} />
                {/* Keyed by barcode: scan/{barcode} remains the canonical deep-link
                    because a barcode is stable and bookmarkable; productId is
                    an internal key the consumer surface doesn't need to expose. */}
                <Route path="p/:barcode" element={<ProductPage />} />
                <Route path="lists" element={<ListsPage />} />
                <Route path="favorites" element={<FavoritesPage />} />
                <Route path="alerts" element={<AlertsPage />} />
                <Route path="profile" element={<ProfilePage />} />
                <Route path="settings" element={<AppSettingsPage />} />
              </Route>
            </Route>
            <Route path="/admin" element={<RequireAuth />}>
              <Route path="onboarding" element={<StoreOnboardingPage />} />
              <Route element={<RequireAdmin />}>
                <Route path="moderation" element={<ModerationPage />} />
              </Route>
              <Route element={<RequireStore />}>
                <Route path="pos/display" element={<CustomerDisplayPage />} />
                <Route element={<CabinetShell />}>
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