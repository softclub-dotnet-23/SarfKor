import { lazy, StrictMode, Suspense } from 'react'
import { createRoot, type Root } from 'react-dom/client'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import './index.css'
import { StaticLanding } from './StaticLanding'
import { ThemeProvider } from './theme/ThemeProvider'
import { LanguageProvider } from './i18n/LanguageProvider'
import { AuthProvider } from './auth/AuthContext'
import { RequireAuth } from './auth/RequireAuth'
import { RequireStore } from './auth/RequireStore'
import { RequireAdmin } from './auth/RequireAdmin'
import { RequireOwner } from './auth/RequireOwner'
import { LoginPage, RegisterPage } from './auth/AuthPage'
import { ForgotPasswordPage } from './auth/ForgotPasswordPage'
import { AcceptInvitePage } from './auth/AcceptInvitePage'
import { ForceChangePasswordPage } from './auth/ForceChangePasswordPage'
import { AppShell } from './app/AppShell'
import { CabinetShell } from './admin/cabinet/CabinetShell'
import { CashierShell } from './admin/CashierShell'
import { AdminConsoleLayout } from './admin/AdminConsoleLayout'
import { useAuth } from './auth/AuthContext'

// Consumer pages — split out so landing/auth bundle stays lean
const HomePage = lazy(() => import('./app/pages/HomePage').then((m) => ({ default: m.HomePage })))
const ScanPage = lazy(() => import('./app/pages/ScanPage').then((m) => ({ default: m.ScanPage })))
const ProductPage = lazy(() => import('./app/pages/ProductPage').then((m) => ({ default: m.ProductPage })))
const ListsPage = lazy(() => import('./app/pages/ListsPage').then((m) => ({ default: m.ListsPage })))
const FavoritesPage = lazy(() => import('./app/pages/FavoritesPage').then((m) => ({ default: m.FavoritesPage })))
const AlertsPage = lazy(() => import('./app/pages/AlertsPage').then((m) => ({ default: m.AlertsPage })))
const ProfilePage = lazy(() => import('./app/pages/ProfilePage').then((m) => ({ default: m.ProfilePage })))
const AppSettingsPage = lazy(() => import('./app/pages/SettingsPage').then((m) => ({ default: m.SettingsPage })))

// Admin / StorePartner pages — each is its own chunk
const StoreOnboardingPage = lazy(() => import('./admin/pages/StoreOnboardingPage').then((m) => ({ default: m.StoreOnboardingPage })))
const AdminOverviewPage = lazy(() => import('./admin/pages/AdminOverviewPage').then((m) => ({ default: m.AdminOverviewPage })))
const AdminStoresPage = lazy(() => import('./admin/pages/AdminStoresPage').then((m) => ({ default: m.AdminStoresPage })))
const AdminSubscriptionsPage = lazy(() => import('./admin/pages/AdminSubscriptionsPage').then((m) => ({ default: m.AdminSubscriptionsPage })))
const AdminUsersPage = lazy(() => import('./admin/pages/AdminUsersPage').then((m) => ({ default: m.AdminUsersPage })))
const AdminReferencePage = lazy(() => import('./admin/pages/AdminReferencePage').then((m) => ({ default: m.AdminReferencePage })))
const AdminAuditLogPage = lazy(() => import('./admin/pages/AdminAuditLogPage').then((m) => ({ default: m.AdminAuditLogPage })))
const DashboardPage = lazy(() => import('./admin/pages/DashboardPage').then((m) => ({ default: m.DashboardPage })))
const PosPage = lazy(() => import('./admin/pages/PosPage').then((m) => ({ default: m.PosPage })))
const InventoryPage = lazy(() => import('./admin/pages/InventoryPage').then((m) => ({ default: m.InventoryPage })))
const SupplyPage = lazy(() => import('./admin/pages/SupplyPage').then((m) => ({ default: m.SupplyPage })))
const MarketingPage = lazy(() => import('./admin/pages/MarketingPage').then((m) => ({ default: m.MarketingPage })))
const StaffPage = lazy(() => import('./admin/pages/StaffPage').then((m) => ({ default: m.StaffPage })))
const ReportsPage = lazy(() => import('./admin/pages/ReportsPage').then((m) => ({ default: m.ReportsPage })))
const AdminSettingsPage = lazy(() => import('./admin/pages/SettingsPage').then((m) => ({ default: m.SettingsPage })))
const CustomerDisplayPage = lazy(() => import('./admin/pages/CustomerDisplayPage').then((m) => ({ default: m.CustomerDisplayPage })))

function PageLoader() {
  return (
    <div
      style={{ display: 'grid', placeItems: 'center', minHeight: '100svh', background: 'var(--bg-app, #fff)' }}
    >
      <svg
        width="18"
        height="18"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.5"
        strokeLinecap="round"
        className="animate-spin"
        style={{ color: 'var(--app-text-primary, #111)' }}
        aria-hidden
      >
        <path d="M21 12a9 9 0 1 1-6.219-8.56" />
      </svg>
    </div>
  )
}

// Reuse the existing root on HMR updates to avoid the "createRoot on a
// container that already has a root" warning every time main.tsx changes.
let root: Root
if (import.meta.hot?.data.root) {
  root = import.meta.hot.data.root as Root
} else {
  root = createRoot(document.getElementById('root')!)
  if (import.meta.hot) import.meta.hot.data.root = root
}

// StorePartner cabinet: an Owner gets the full desktop sidebar (CabinetShell,
// from the design-system pass), a Cashier gets the dedicated phone-first shell
// instead (bottom tab bar, no sidebar) — see CashierShell's own comment for why
// that's a deliberate split and not just a restyle (§1: phone is the primary
// work device, and a cashier is standing at a register, not at a desk).
function StorePartnerShell() {
  const { currentStoreRole } = useAuth()
  return currentStoreRole === 'Cashier' ? <CashierShell /> : <CabinetShell />
}

root.render(
  <StrictMode>
    <ThemeProvider>
      <LanguageProvider>
        <BrowserRouter>
          <AuthProvider>
            <Suspense fallback={<PageLoader />}>
              <Routes>
                <Route path="/" element={<StaticLanding />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                <Route path="/accept-invite" element={<AcceptInvitePage />} />
                <Route path="/invite/:token" element={<AcceptInvitePage />} />
                <Route path="/change-password" element={<RequireAuth />}>
                  <Route index element={<ForceChangePasswordPage />} />
                </Route>
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
                    <Route element={<StorePartnerShell />}>
                      <Route path="pos" element={<PosPage />} />
                      <Route path="inventory" element={<InventoryPage />} />
                      <Route element={<RequireOwner />}>
                        <Route index element={<DashboardPage />} />
                        <Route path="supply" element={<SupplyPage />} />
                        <Route path="marketing" element={<MarketingPage />} />
                        <Route path="staff" element={<StaffPage />} />
                        <Route path="reports" element={<ReportsPage />} />
                        <Route path="settings" element={<AdminSettingsPage />} />
                      </Route>
                    </Route>
                  </Route>
                </Route>
              </Routes>
            </Suspense>
          </AuthProvider>
        </BrowserRouter>
      </LanguageProvider>
    </ThemeProvider>
  </StrictMode>,
)
