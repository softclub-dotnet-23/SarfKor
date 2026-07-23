import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import './index.css'
import App from './App.tsx'
import { ThemeProvider } from './theme/ThemeProvider'
import { AuthProvider } from './auth/AuthContext'
import { RequireAuth } from './auth/RequireAuth'
import { RequireStore } from './auth/RequireStore'
import { RequireAdmin } from './auth/RequireAdmin'
import { LoginPage } from './auth/LoginPage'
import { AdminLayout } from './admin/AdminLayout'
import { ConsoleLayout } from './admin/ConsoleLayout'
import { StoreOnboardingPage } from './admin/pages/StoreOnboardingPage'
import { DashboardPage } from './admin/pages/DashboardPage'
import { PosPage } from './admin/pages/PosPage'
import { InventoryPage } from './admin/pages/InventoryPage'
import { StaffPage } from './admin/pages/StaffPage'
import { ReportsPage } from './admin/pages/ReportsPage'
import { SettingsPage } from './admin/pages/SettingsPage'
import { ConsolePage } from './admin/pages/ConsolePage'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            <Route path="/" element={<App />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/admin" element={<RequireAuth />}>
              <Route path="onboarding" element={<StoreOnboardingPage />} />
              <Route element={<RequireStore />}>
                <Route element={<AdminLayout />}>
                  <Route index element={<DashboardPage />} />
                  <Route path="pos" element={<PosPage />} />
                  <Route path="inventory" element={<InventoryPage />} />
                  <Route path="staff" element={<StaffPage />} />
                  <Route path="reports" element={<ReportsPage />} />
                  <Route path="settings" element={<SettingsPage />} />
                </Route>
              </Route>
            </Route>
            <Route path="/console" element={<RequireAuth />}>
              <Route element={<RequireAdmin />}>
                <Route element={<ConsoleLayout />}>
                  <Route index element={<ConsolePage />} />
                </Route>
              </Route>
            </Route>
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  </StrictMode>,
)
