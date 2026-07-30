import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate, type Location } from 'react-router-dom'
import { motion } from 'framer-motion'
import { useAuth } from './AuthContext'

type Mode = 'login' | 'register'

const EASE = [0.16, 1, 0.3, 1] as const

/**
 * Standalone authentication, in the landing's visual language: one permanent
 * monochrome identity, generous whitespace, hairline rules instead of boxes.
 *
 * The auth logic is deliberately untouched — same useAuth().login/register, same
 * `from` restoration, same role-based fallback as before. Only presentation and
 * routing changed: this used to be a dialog over the film, and is now a page of
 * its own at /login and /register.
 */
function AuthPage({ mode }: { mode: Mode }) {
  const { login, register } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const isRegister = mode === 'register'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const emailRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    emailRef.current?.focus()
  }, [])
  // Switching between the two routes should not carry a stale error across.
  useEffect(() => {
    setError('')
    setConfirmPassword('')
  }, [mode])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')

    if (isRegister && password !== confirmPassword) {
      setError('Пароли не совпадают')
      return
    }

    setLoading(true)
    const result = isRegister ? await register(email, password) : await login(email, password)
    setLoading(false)
    if (!result.ok) {
      setError(result.error)
      return
    }
    // Unchanged: a returning visit to a protected page goes back there; a fresh
    // sign-in routes by role, and a plain consumer account just goes home.
    const from = (location.state as { from?: Location })?.from?.pathname
    const fallback = result.roles.includes('Admin')
      ? '/admin/moderation'
      : result.roles.includes('StorePartner')
        ? '/admin'
        : '/'
    navigate(from ?? fallback, { replace: true })
  }

  const field =
    'w-full bg-transparent border-0 border-b border-white/15 pb-3 pt-2 text-[16px] text-white ' +
    'placeholder:text-white/25 outline-none transition-colors duration-300 focus:border-white/70'

  return (
    <div className="relative min-h-screen overflow-hidden bg-black text-white">
      {/* one soft key light, the same device the film uses to carry continuity */}
      <div
        aria-hidden
        className="pointer-events-none absolute left-1/2 top-0 h-[80vmax] w-[80vmax] -translate-x-1/2 -translate-y-1/2 rounded-full"
        style={{ background: 'radial-gradient(circle,rgba(255,255,255,.09),transparent 62%)', filter: 'blur(40px)' }}
      />

      <header className="relative z-10 mx-auto flex max-w-[1200px] items-center justify-between px-6 py-7 sm:px-10">
        <Link to="/" className="flex items-center gap-3">
          <span className="grid h-8 w-8 place-items-center rounded-[10px] bg-white text-[15px] font-extrabold tracking-tight text-black">
            S
          </span>
          <span className="text-[17px] font-bold tracking-tight">Sarfkor</span>
        </Link>
        <Link
          to="/"
          className="text-[13px] font-medium text-white/45 transition-colors duration-300 hover:text-white"
        >
          На главную
        </Link>
      </header>

      <main className="relative z-10 mx-auto flex min-h-[calc(100vh-96px)] max-w-[1200px] items-center px-6 pb-24 sm:px-10">
        <motion.div
          initial={{ opacity: 0, y: 22 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.9, ease: EASE }}
          className="w-full max-w-[440px]"
        >
          <motion.p
            initial={{ opacity: 0, y: 14 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8, ease: EASE, delay: 0.08 }}
            className="mb-6 text-[12px] font-bold uppercase tracking-[0.2em] text-white/35"
          >
            {isRegister ? 'Регистрация' : 'Вход'}
          </motion.p>

          <motion.h1
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.9, ease: EASE, delay: 0.14 }}
            className="mb-5 text-[clamp(34px,5vw,56px)] font-extrabold leading-[0.98] tracking-[-0.035em]"
          >
            {isRegister ? 'Создайте аккаунт' : 'С возвращением'}
          </motion.h1>

          <motion.p
            initial={{ opacity: 0, y: 14 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.9, ease: EASE, delay: 0.2 }}
            className="mb-12 max-w-[380px] text-[16px] leading-relaxed text-white/50"
          >
            {isRegister
              ? 'Несколько секунд — и вы сможете подключить магазин к Sarfkor.'
              : 'Войдите, чтобы вернуться к своему магазину и отчётам.'}
          </motion.p>

          <motion.form
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.9, ease: EASE, delay: 0.26 }}
            onSubmit={handleSubmit}
            className="flex flex-col gap-9"
          >
            <label className="flex flex-col gap-2">
              <span className="text-[11px] font-semibold uppercase tracking-[0.14em] text-white/35">Email</span>
              <input
                ref={emailRef}
                type="email"
                required
                autoComplete="username"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@sarfkor.tj"
                className={field}
              />
            </label>

            <label className="flex flex-col gap-2">
              <span className="flex items-center justify-between text-[11px] font-semibold uppercase tracking-[0.14em] text-white/35">
                Пароль
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="text-[11px] font-semibold normal-case tracking-normal text-white/35 transition-colors duration-300 hover:text-white/80"
                >
                  {showPassword ? 'Скрыть' : 'Показать'}
                </button>
              </span>
              <input
                type={showPassword ? 'text' : 'password'}
                required
                minLength={8}
                autoComplete={isRegister ? 'new-password' : 'current-password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className={field}
              />
              {isRegister && <span className="text-[12px] text-white/30">Минимум 8 символов</span>}
            </label>

            {isRegister && (
              <label className="flex flex-col gap-2">
                <span className="text-[11px] font-semibold uppercase tracking-[0.14em] text-white/35">
                  Повторите пароль
                </span>
                <input
                  type={showPassword ? 'text' : 'password'}
                  required
                  autoComplete="new-password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  placeholder="••••••••"
                  className={field}
                />
              </label>
            )}

            {error && (
              <motion.div
                role="alert"
                initial={{ opacity: 0, y: -6 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.4, ease: EASE }}
                className="border-l-2 border-[#f87171] pl-4 text-[13.5px] font-medium leading-relaxed text-[#f87171]"
              >
                {error}
              </motion.div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="group mt-2 flex items-center justify-center gap-3 rounded-full bg-white py-4 text-[15px] font-bold text-black transition-all duration-500 hover:scale-[1.015] active:scale-[0.985] disabled:opacity-50"
              style={{ transitionTimingFunction: 'cubic-bezier(0.16,1,0.3,1)' }}
            >
              {loading ? 'Подождите…' : isRegister ? 'Создать аккаунт' : 'Войти'}
              {!loading && (
                <span className="transition-transform duration-500 group-hover:translate-x-1" style={{ transitionTimingFunction: 'cubic-bezier(0.16,1,0.3,1)' }}>
                  →
                </span>
              )}
            </button>
          </motion.form>

          <motion.p
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.9, ease: EASE, delay: 0.4 }}
            className="mt-10 text-[13.5px] text-white/40"
          >
            {isRegister ? 'Уже есть аккаунт? ' : 'Ещё нет аккаунта? '}
            <Link
              to={isRegister ? '/login' : '/register'}
              className="font-semibold text-white underline decoration-white/25 underline-offset-[6px] transition-colors duration-300 hover:decoration-white"
            >
              {isRegister ? 'Войти' : 'Зарегистрироваться'}
            </Link>
          </motion.p>
        </motion.div>
      </main>
    </div>
  )
}

export function LoginPage() {
  return <AuthPage mode="login" />
}

export function RegisterPage() {
  return <AuthPage mode="register" />
}
