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
import { AdminLayout } from './admin/AdminLayout'
import { StoreOnboardingPage } from './admin/pages/StoreOnboardingPage'
import { AdminConsoleLayout } from './admin/AdminConsoleLayout'
import { AdminOverviewPage } from './admin/pages/AdminOverviewPage'
import { AdminStoresPage } from './admin/pages/AdminStoresPage'
import { AdminSubscriptionsPage } from './admin/pages/AdminSubscriptionsPage'
import { AdminUsersPage } from './admin/pages/AdminUsersPage'
import { AdminReferencePage } from './admin/pages/AdminReferencePage'
import { AdminAuditLogPage } from './admin/pages/AdminAuditLogPage'
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
            <Route path="/accept-invite" element={<AcceptInvitePage />} />
            {/* Consumer app. Reuses the existing RequireAuth guard rather than
                introducing a second notion of "signed in". */}
            <Route path="/app" element={<RequireAuth />}>
              <Route element={<AppShell />}>
                <Route index element={<HomePage />} />
                <Route path="scan" element={<ScanPage />} />
                {/* Keyed by barcode, not id: the backend has no product-by-id
                    route, so scan/{barcode} is the only way to resolve a product
                    and a bookmarked link has to carry the code itself. */}
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
                <Route element={<AdminConsoleLayout />}>
                  <Route path="overview" element={<AdminOverviewPage />} />
                  <Route path="stores" element={<AdminStoresPage />} />
                  <Route path="subscriptions" element={<AdminSubscriptionsPage />} />
                  <Route path="users" element={<AdminUsersPage />} />
                  <Route path="reference" element={<AdminReferencePage />} />
                  <Route path="audit-log" element={<AdminAuditLogPage />} />
                </Route>
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