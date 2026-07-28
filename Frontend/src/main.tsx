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
import { LoginPage } from './auth/LoginPage'
import { AdminLayout } from './admin/AdminLayout'
import { StoreOnboardingPage } from './admin/pages/StoreOnboardingPage'
import { ModerationPage } from './admin/pages/ModerationPage'
import { DashboardPage } from './admin/pages/DashboardPage'
import { PosPage } from './admin/pages/PosPage'
import { InventoryPage } from './admin/pages/InventoryPage'
import { SupplyPage } from './admin/pages/SupplyPage'
import { CustomersPage } from './admin/pages/CustomersPage'
import { MarketingPage } from './admin/pages/MarketingPage'
import { StaffPage } from './admin/pages/StaffPage'
import { ReportsPage } from './admin/pages/ReportsPage'
import { SettingsPage } from './admin/pages/SettingsPage'
import { AppLayout } from './app/AppLayout'
import { ScanPage } from './app/pages/ScanPage'
import { FavoritesPage } from './app/pages/FavoritesPage'
import { ShoppingListsPage } from './app/pages/ShoppingListsPage'
import { PriceAlertsPage } from './app/pages/PriceAlertsPage'
import { ExpiringOffersPage } from './app/pages/ExpiringOffersPage'
import { NotificationsPage } from './app/pages/NotificationsPage'
import { ReceiptsPage } from './app/pages/ReceiptsPage'
import { AccountPage } from './app/pages/AccountPage'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<StaticLanding />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/app" element={<RequireAuth />}>
              <Route element={<AppLayout />}>
                <Route index element={<ScanPage />} />
                <Route path="favorites" element={<FavoritesPage />} />
                <Route path="lists" element={<ShoppingListsPage />} />
                <Route path="expiring" element={<ExpiringOffersPage />} />
                <Route path="alerts" element={<PriceAlertsPage />} />
                <Route path="notifications" element={<NotificationsPage />} />
                <Route path="receipts" element={<ReceiptsPage />} />
                <Route path="account" element={<AccountPage />} />
              </Route>
            </Route>
            <Route path="/admin" element={<RequireAuth />}>
              <Route path="onboarding" element={<StoreOnboardingPage />} />
              <Route element={<RequireAdmin />}>
                <Route path="moderation" element={<ModerationPage />} />
              </Route>
              <Route element={<RequireStore />}>
                <Route element={<AdminLayout />}>
                  <Route index element={<DashboardPage />} />
                  <Route path="pos" element={<PosPage />} />
                  <Route path="inventory" element={<InventoryPage />} />
                  <Route path="supply" element={<SupplyPage />} />
                  <Route path="customers" element={<CustomersPage />} />
                  <Route path="marketing" element={<MarketingPage />} />
                  <Route path="staff" element={<StaffPage />} />
                  <Route path="reports" element={<ReportsPage />} />
                  <Route path="settings" element={<SettingsPage />} />
                </Route>
              </Route>
            </Route>
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  </StrictMode>,
)
