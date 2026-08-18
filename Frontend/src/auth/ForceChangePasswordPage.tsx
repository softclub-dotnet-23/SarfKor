import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import { LogoMark } from '../components/Logo'
import { SunIcon, MoonIcon } from '../components/icons'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { LanguageSwitcher } from '../admin/components/LanguageSwitcher'
import { useT } from '../i18n/translations'
import { meApi } from '../lib/api'
import { describeError } from '../lib/errorKind'
import { useAuth } from './AuthContext'

const inputCls =
  'w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]'

// Reached only through RequireAuth's own redirect (mustChangePassword true) -- a store owner set
// this account's password on its behalf (fresh cashier, or a reset), so the very first thing this
// account does, before any cabinet screen, is pick a password only the cashier themselves knows.
// Deliberately its own page, not a modal over whatever screen the redirect interrupted -- there is
// no "cancel" out of this one.
export function ForceChangePasswordPage() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const t = useT()
  const isDark = theme === 'dark'
  const { user, mustChangePassword, clearMustChangePassword, login, logout } = useAuth()
  const navigate = useNavigate()

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)

  if (!user) return <Navigate to="/login" replace />
  // Reached directly (not via the redirect) once the flag is already clear — nothing to force.
  if (!mustChangePassword && !done) return <Navigate to="/admin" replace />

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    if (newPassword !== confirmPassword) {
      setError(t('changePassword.mismatch'))
      return
    }
    setLoading(true)
    try {
      await meApi.changePassword(currentPassword, newPassword)
      clearMustChangePassword()
      // ChangePasswordCommandHandler revokes every refresh token on success (by design — a
      // password change is exactly when a hijacked session should die), and the still-valid
      // access token keeps carrying the now-stale mustChangePassword claim until it naturally
      // expires (~15 min). clearMustChangePassword() alone only fixes the in-memory state for
      // this tab; a fresh login with the just-set password gets a genuinely new token instead, so
      // a page reload five minutes from now doesn't bounce the cashier back to this exact screen.
      if (user) await login(user.email, newPassword)
      setDone(true)
    } catch (err) {
      setError(describeError(err, t))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="admin-shell flex min-h-screen flex-col bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      <div className="flex items-center justify-between p-6">
        <div className="flex items-center gap-2.5">
          <LogoMark size={26} />
          <span className="text-[18px] font-extrabold tracking-tight">Sarfkor</span>
        </div>
        <div className="flex items-center gap-2">
          <LanguageSwitcher scheme="admin" />
          <button
            onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
            aria-label={t('shell.toggleTheme')}
            className="grid h-9 w-9 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]"
          >
            {isDark ? <SunIcon width={17} height={17} /> : <MoonIcon width={17} height={17} />}
          </button>
          <button
            onClick={logout}
            className="text-[13px] font-medium text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)]"
          >
            {t('changePassword.logout')}
          </button>
        </div>
      </div>

      <div className="flex flex-1 items-center justify-center px-6 pb-16">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5, ease: [0.16, 1, 0.3, 1] }}
          className="w-full max-w-[380px]"
        >
          {done ? (
            <>
              <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight">{t('changePassword.doneTitle')}</h2>
              <p className="mb-7 text-[14px] leading-relaxed text-[color:var(--admin-text-tertiary)]">{t('changePassword.doneBody')}</p>
              <button
                type="button"
                // Client-side navigation, deliberately not a plain <a href> — a full page reload
                // here would re-decode the still-cached access token, whose mustChangePassword
                // claim stays stale until it naturally rotates (see JwtTokenGenerator), and bounce
                // straight back to this same screen. AuthContext's in-memory state (already false,
                // via clearMustChangePassword above) is what RequireAuth actually reads, and only a
                // client-side transition preserves it instead of re-resolving the whole session.
                onClick={() => navigate('/admin', { replace: true })}
                className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98]"
              >
                {t('changePassword.continue')}
              </button>
            </>
          ) : (
            <>
              <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight">{t('changePassword.title')}</h2>
              <p className="mb-7 text-[14px] leading-relaxed text-[color:var(--admin-text-tertiary)]">{t('changePassword.subtitle')}</p>

              <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                <label className="flex flex-col gap-1.5">
                  <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">{t('changePassword.currentLabel')}</span>
                  <input
                    type="password"
                    required
                    autoFocus
                    autoComplete="current-password"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    placeholder="••••••••"
                    className={inputCls}
                  />
                  <span className="text-[11px] text-[color:var(--admin-text-tertiary)]">{t('changePassword.currentHint')}</span>
                </label>

                <label className="flex flex-col gap-1.5">
                  <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">{t('changePassword.newLabel')}</span>
                  <input
                    type="password"
                    required
                    minLength={8}
                    autoComplete="new-password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="••••••••"
                    className={inputCls}
                  />
                  <span className="text-[11px] text-[color:var(--admin-text-tertiary)]">{t('changePassword.newHint')}</span>
                </label>

                <label className="flex flex-col gap-1.5">
                  <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">{t('changePassword.confirmLabel')}</span>
                  <input
                    type="password"
                    required
                    autoComplete="new-password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    placeholder="••••••••"
                    className={inputCls}
                  />
                </label>

                {error && (
                  <div className="rounded-lg bg-[color:var(--admin-danger-dim)] px-3.5 py-2.5 text-[12.5px] font-medium text-[color:var(--admin-danger)]">
                    {error}
                  </div>
                )}

                <button
                  type="submit"
                  disabled={loading}
                  className="mt-1 flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-60"
                >
                  {loading ? t('changePassword.submitBusy') : t('changePassword.submit')}
                </button>
              </form>
            </>
          )}
        </motion.div>
      </div>
    </div>
  )
}
