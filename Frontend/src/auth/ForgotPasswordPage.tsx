import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { LogoMark } from '../components/Logo'
import { SunIcon, MoonIcon } from '../components/icons'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { authApi, ApiError } from '../lib/api'

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

// Two stages in one page instead of a separate /reset-password route: request sends a 6-digit
// code by email (see AuthController.ForgotPassword), and the code is typed in right here — there's
// no link to click and no token to carry across pages, so there's nothing for a second route to do.
type Stage = 'request' | 'reset' | 'done'

export function ForgotPasswordPage() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'

  const [stage, setStage] = useState<Stage>('request')
  const [email, setEmail] = useState('')
  const [code, setCode] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [resent, setResent] = useState(false)
  const [error, setError] = useState('')

  async function handleRequestSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await authApi.forgotPassword(email)
    } catch (err) {
      // Network/validation errors only — the backend itself never reveals whether the email
      // exists, so this still doesn't tell an attacker anything new.
      if (err instanceof ApiError && err.status === 429) {
        setError('Слишком много попыток. Подождите немного и попробуйте снова')
        setLoading(false)
        return
      }
      if (err instanceof ApiError) {
        setError('Не удалось отправить запрос — проверьте email и попробуйте снова')
        setLoading(false)
        return
      }
    }
    setLoading(false)
    setStage('reset')
  }

  async function handleResend() {
    setError('')
    setLoading(true)
    try {
      await authApi.forgotPassword(email)
      setResent(true)
      setTimeout(() => setResent(false), 4000)
    } catch {
      // Same anti-enumeration reasoning as the initial request — no different message here.
    }
    setLoading(false)
  }

  async function handleResetSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')

    if (password !== confirmPassword) {
      setError('Пароли не совпадают')
      return
    }

    setLoading(true)
    try {
      await authApi.resetPassword(email, code, password)
      setStage('done')
    } catch (err) {
      if (err instanceof ApiError && err.status === 429) {
        setError('Слишком много попыток. Подождите немного и попробуйте снова')
      } else if (err instanceof ApiError && err.status === 400 && err.body && typeof err.body === 'object' && 'errors' in err.body) {
        // FluentValidation failures come back as a ValidationProblemDetails object (has `.errors`),
        // distinct from the plain-string "Invalid or expired code." the handler returns for a
        // wrong/expired code — both are 400s, so the shape is what tells them apart.
        setError('Пароль должен содержать минимум 8 символов: заглавную и строчную буквы, цифру и спецсимвол')
      } else {
        setError('Неверный или истёкший код — запросите новый')
      }
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
            key={stage}
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, ease: [0.16, 1, 0.3, 1] }}
            className="w-full max-w-[380px]"
          >
            {stage === 'done' ? (
              <>
                <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
                  Пароль изменён
                </h2>
                <p className="mb-7 text-[14px] leading-relaxed text-[color:var(--admin-text-tertiary)]">
                  Теперь можно войти с новым паролем.
                </p>
                <Link
                  to="/login"
                  className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98]"
                >
                  Войти
                </Link>
              </>
            ) : stage === 'reset' ? (
              <>
                <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
                  Проверьте почту
                </h2>
                <p className="mb-7 text-[14px] leading-relaxed text-[color:var(--admin-text-tertiary)]">
                  Мы отправили код на {email}. Введите его вместе с новым паролем.
                </p>

                <form onSubmit={handleResetSubmit} className="flex flex-col gap-4">
                  <label className="flex flex-col gap-1.5">
                    <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Код из письма</span>
                    <input
                      type="text"
                      inputMode="numeric"
                      pattern="[0-9]{6}"
                      maxLength={6}
                      required
                      autoFocus
                      autoComplete="one-time-code"
                      value={code}
                      onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                      placeholder="000000"
                      className="rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-3.5 py-2.5 text-[13px] tracking-[0.3em] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]"
                    />
                  </label>

                  <label className="flex flex-col gap-1.5">
                    <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Новый пароль</span>
                    <div className="relative">
                      <input
                        type={showPassword ? 'text' : 'password'}
                        required
                        minLength={8}
                        autoComplete="new-password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        placeholder="••••••••"
                        className="w-full rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-3.5 py-2.5 pr-10 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]"
                      />
                      <EyeToggle shown={showPassword} onClick={() => setShowPassword((v) => !v)} />
                    </div>
                    <span className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                      Минимум 8 символов, заглавная и строчная буквы, цифра и спецсимвол
                    </span>
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
                    <div className="rounded-lg bg-[color:var(--admin-danger-dim)] px-3.5 py-2.5 text-[12.5px] font-medium text-[color:var(--admin-danger)]">
                      {error}
                    </div>
                  )}
                  {resent && (
                    <div className="text-[12.5px] font-medium text-[color:var(--admin-text-tertiary)]">Новый код отправлен</div>
                  )}

                  <button
                    type="submit"
                    disabled={loading || code.length !== 6}
                    className="mt-1 flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-60"
                  >
                    {loading ? 'Сохраняем…' : 'Сохранить новый пароль'}
                  </button>
                </form>

                <p className="mt-6 text-center text-[12.5px] text-[color:var(--admin-text-tertiary)]">
                  Не пришёл код?{' '}
                  <button
                    type="button"
                    onClick={handleResend}
                    disabled={loading}
                    className="font-semibold text-[color:var(--admin-accent)] hover:opacity-80 disabled:opacity-50"
                  >
                    Отправить ещё раз
                  </button>
                </p>
              </>
            ) : (
              <>
                <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
                  Забыли пароль?
                </h2>
                <p className="mb-7 text-[14px] text-[color:var(--admin-text-tertiary)]">
                  Введите email — мы отправим код для сброса пароля
                </p>

                <form onSubmit={handleRequestSubmit} className="flex flex-col gap-4">
                  <label className="flex flex-col gap-1.5">
                    <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Email</span>
                    <input
                      type="email"
                      required
                      autoComplete="username"
                      autoFocus
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      placeholder="murod@sarfkor.tj"
                      className="rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]"
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
                    {loading ? 'Отправляем…' : 'Отправить код'}
                  </button>
                </form>

                <p className="mt-6 text-center text-[12.5px] text-[color:var(--admin-text-tertiary)]">
                  Вспомнили пароль?{' '}
                  <Link to="/login" className="font-semibold text-[color:var(--admin-accent)] hover:opacity-80">
                    Войти
                  </Link>
                </p>
              </>
            )}
          </motion.div>
        </div>
      </div>
    </div>
  )
}
