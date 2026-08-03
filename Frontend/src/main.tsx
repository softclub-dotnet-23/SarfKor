import { lazy, StrictMode, Suspense } from 'react'
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
import { CabinetShell } from './admin/cabinet/CabinetShell'

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
const ModerationPage = lazy(() => import('./admin/pages/ModerationPage').then((m) => ({ default: m.ModerationPage })))
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

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <BrowserRouter>
        <AuthProvider>
          <Suspense fallback={<PageLoader />}>
            <Routes>
              <Route path="/" element={<StaticLanding />} />
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />
              <Route path="/reset-password" element={<ResetPasswordPage />} />
              <Route path="/accept-invite" element={<AcceptInvitePage />} />

              <Route path="/app" element={<RequireAuth />}>
                <Route element={<AppShell />}>
                  <Route index element={<HomePage />} />
                  <Route path="scan" element={<ScanPage />} />
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
                      <Route path="settings" element={<AdminSettingsPage />} />
                    </Route>
                  </Route>
                </Route>
              </Route>
            </Routes>
          </Suspense>
        </AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  </StrictMode>,
)
