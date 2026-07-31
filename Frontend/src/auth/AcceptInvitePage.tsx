import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { motion } from 'framer-motion'
import { LogoMark } from '../components/Logo'
import { SunIcon, MoonIcon } from '../components/icons'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { authApi, ApiError } from '../lib/api'
import { useAuth } from './AuthContext'

function EyeToggle({ shown, onClick }: { shown: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={shown ? 'Скрыть пароль' : 'Показать пароль'}
      className="absolute right-3.5 top-1/2 -translate-y-1/2 text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text-secondary)]"
    >
      <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
        {shown ? (
          <>
            <path d="M17.94 17.94A10.94 10.94 0 0 1 12 19c-7 0-11-7-11-7a21.6 21.6 0 0 1 5.06-5.94M9.9 4.24A10.4 10.4 0 0 1 12 4c7 0 11 7 11 7a21.6 21.6 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
            <line x1="1" y1="1" x2="23" y2="23" />
          </>
        ) : (
          <>
            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z" />
            <circle cx="12" cy="12" r="3" />
          </>
        )}
      </svg>
    </button>
  )
}

export function AcceptInvitePage() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const navigate = useNavigate()
  const { applyAuthResult } = useAuth()

  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [accountAlreadyExisted, setAccountAlreadyExisted] = useState(false)
  const [error, setError] = useState('')

  const linkIncomplete = !token

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')

    if (password !== confirmPassword) {
      setError('Пароли не совпадают')
      return
    }

    setLoading(true)
    try {
      const res = await authApi.acceptStoreEmployeeInvitation(token, password)
      if (res.outcome === 'Accepted' && res.auth) {
        await applyAuthResult(res.auth)
        // RequireOwner already bounces a Cashier straight to /admin/pos — no special-casing needed.
        navigate('/admin', { replace: true })
      } else if (res.outcome === 'AccountAlreadyExisted') {
        setAccountAlreadyExisted(true)
      } else {
        setError('Приглашение недействительно или истекло — попросите владельца магазина пригласить вас снова')
      }
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 429
          ? 'Слишком много попыток. Подождите немного и попробуйте снова'
          : 'Приглашение недействительно или истекло — попросите владельца магазина пригласить вас снова',
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="admin-shell flex min-h-screen bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      <div
        className="relative hidden w-[45%] shrink-0 flex-col justify-between overflow-hidden p-12 text-white lg:flex"
        style={{ background: 'linear-gradient(135deg,#0c4a6e,#0369a1,#0ea5e9)' }}
      >
        <div className="pointer-events-none absolute -right-24 -top-24 h-80 w-80 rounded-full bg-white/[0.06]" />
        <div className="pointer-events-none absolute bottom-[-80px] left-10 h-64 w-64 rounded-full bg-white/[0.05]" />

        <Link to="/" className="relative flex items-center gap-2.5">
          <LogoMark size={30} />
          <span className="text-[20px] font-extrabold tracking-tight">Sarfkor</span>
        </Link>

        <div className="relative">
          <div className="mb-4 text-[13px] font-semibold uppercase tracking-wide text-white/60">Partner Cabinet</div>
          <h1 className="mb-4 max-w-sm text-[32px] font-extrabold leading-tight tracking-tight">
            Управляйте магазином из одной панели
          </h1>
          <p className="max-w-sm text-[15px] leading-relaxed text-white/70">
            Касса, склад, сотрудники и отчёты о прибыли — всё в реальном времени, с доступом по ролям для владельца,
            управляющего и кассира.
          </p>
        </div>

        <div className="relative text-[13px] text-white/50">© 2026 Sarfkor. Таджикистан</div>
      </div>

      <div className="flex flex-1 flex-col">
        <div className="flex items-center justify-between p-6">
          <Link to="/" className="flex items-center gap-2.5 lg:hidden">
            <LogoMark size={26} />
            <span className="text-[18px] font-extrabold tracking-tight text-[color:var(--admin-text)]">Sarfkor</span>
          </Link>
          <div className="ml-auto flex items-center gap-3">
            <button
              onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
              aria-label="Переключить тему"
              className="grid h-9 w-9 place-items-center rounded-lg text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)]"
            >
              {isDark ? <SunIcon width={17} height={17} /> : <MoonIcon width={17} height={17} />}
            </button>
            <Link to="/" className="text-[13px] font-medium text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)]">
              На главный сайт
            </Link>
          </div>
        </div>

        <div className="flex flex-1 items-center justify-center px-6 pb-16">
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, ease: [0.16, 1, 0.3, 1] }}
            className="w-full max-w-[380px]"
          >
            {linkIncomplete ? (
              <>
                <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
                  Ссылка неполная
                </h2>
                <p className="mb-7 text-[14px] leading-relaxed text-[color:var(--admin-text-tertiary)]">
                  Откройте ссылку из письма целиком, или попросите владельца магазина пригласить вас снова.
                </p>
                <Link
                  to="/login"
                  className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98]"
                >
                  Войти
                </Link>
              </>
            ) : accountAlreadyExisted ? (
              <>
                <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
                  Вы уже в команде
                </h2>
                <p className="mb-7 text-[14px] leading-relaxed text-[color:var(--admin-text-tertiary)]">
                  Аккаунт с этим email уже существует — мы добавили вас в магазин. Войдите со своим текущим паролем.
                </p>
                <Link
                  to="/login"
                  className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98]"
                >
                  Войти
                </Link>
              </>
            ) : (
              <>
                <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
                  Приглашение в команду
                </h2>
                <p className="mb-7 text-[14px] text-[color:var(--admin-text-tertiary)]">Задайте пароль, чтобы начать работу</p>

                <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                  <label className="flex flex-col gap-1.5">
                    <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Пароль</span>
                    <div className="relative">
                      <input
                        type={showPassword ? 'text' : 'password'}
                        required
                        minLength={8}
                        autoComplete="new-password"
                        autoFocus
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        placeholder="••••••••"
                        className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-3.5 py-2.5 pr-10 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]"
                      />
                      <EyeToggle shown={showPassword} onClick={() => setShowPassword((v) => !v)} />
                    </div>
                    <span className="text-[11px] text-[color:var(--admin-text-tertiary)]">Минимум 8 символов</span>
                  </label>

                  <label className="flex flex-col gap-1.5">
                    <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Повторите пароль</span>
                    <input
                      type={showPassword ? 'text' : 'password'}
                      required
                      autoComplete="new-password"
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      placeholder="••••••••"
                      className="rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]"
                    />
                  </label>

                  {error && (
                    <div className="rounded-lg bg-[#f8717118] px-3.5 py-2.5 text-[12.5px] font-medium text-[#f87171]">
                      {error}
                    </div>
                  )}

                  <button
                    type="submit"
                    disabled={loading}
                    className="mt-1 flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-60"
                  >
                    {loading ? 'Сохраняем…' : 'Присоединиться'}
                  </button>
                </form>
              </>
            )}
          </motion.div>
        </div>
      </div>
    </div>
  )
}
