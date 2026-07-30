import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { LogoMark } from '../components/Logo'
import { SunIcon, MoonIcon } from '../components/icons'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { authApi, ApiError } from '../lib/api'

export function ForgotPasswordPage() {
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'

  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState('')

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await authApi.forgotPassword(email)
    } catch (err) {
      // Network/validation errors only — the backend itself never reveals whether the email
      // exists, so this still doesn't tell an attacker anything new.
      if (err instanceof ApiError && err.status !== 429) {
        setError('Не удалось отправить запрос — проверьте email и попробуйте снова')
        setLoading(false)
        return
      }
      if (err instanceof ApiError && err.status === 429) {
        setError('Слишком много попыток. Подождите немного и попробуйте снова')
        setLoading(false)
        return
      }
    }
    setLoading(false)
    setSent(true)
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
            {sent ? (
              <>
                <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
                  Проверьте почту
                </h2>
                <p className="mb-7 text-[14px] leading-relaxed text-[color:var(--admin-text-tertiary)]">
                  Если такой email зарегистрирован, на него отправлена ссылка для сброса пароля. Ссылка действительна
                  в течение часа.
                </p>
                <Link
                  to="/login"
                  className="flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98]"
                >
                  Вернуться ко входу
                </Link>
              </>
            ) : (
              <>
                <h2 className="mb-1.5 text-[24px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
                  Забыли пароль?
                </h2>
                <p className="mb-7 text-[14px] text-[color:var(--admin-text-tertiary)]">
                  Введите email — мы отправим ссылку для сброса пароля
                </p>

                <form onSubmit={handleSubmit} className="flex flex-col gap-4">
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
                    <div className="rounded-lg bg-[#f8717118] px-3.5 py-2.5 text-[12.5px] font-medium text-[#f87171]">
                      {error}
                    </div>
                  )}

                  <button
                    type="submit"
                    disabled={loading}
                    className="mt-1 flex items-center justify-center gap-2 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-60"
                  >
                    {loading ? 'Отправляем…' : 'Отправить ссылку'}
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
