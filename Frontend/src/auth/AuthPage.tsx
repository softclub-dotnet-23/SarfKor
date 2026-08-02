import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate, type Location } from 'react-router-dom'
import { motion, AnimatePresence, useReducedMotion } from 'framer-motion'
import { useAuth } from './AuthContext'
import { useTheme } from '../theme/ThemeProvider'
import { useThemeTransition } from '../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../components/icons'
import { Atmosphere, AtmosphereKeyframes, AuthStats } from './AuthAtmosphere'

type Mode = 'login' | 'register'

/* The landing's reveal easing, reused verbatim so these pages read as the next
   shot of the same film rather than a separate template. */
const EASE = [0.16, 1, 0.3, 1] as const

/* Was a fixed monochrome scale (white at four alphas, contrast-checked against
   a permanently black page). Login/Register now themes like the rest of the
   admin/auth family (ForgotPasswordPage, ResetPasswordPage, AcceptInvitePage
   already do this) — --admin-text-secondary/-tertiary already carry their own
   per-theme AA-checked values, so the roles below just point at them instead
   of re-deriving alphas by hand. */
const TXT = {
  secondary: 'var(--admin-text-secondary)',
  tertiary: 'var(--admin-text-tertiary)',
  rest: 'var(--admin-text-tertiary)',
}

/* Entrance cascade. `reduce` collapses it to the resting state rather than to a
   faster version of itself: the elements are authored at opacity 0, so a visitor
   who has asked for no motion must be handed the finished frame directly instead
   of a shorter animation of it. */
const rise = (i: number, reduce?: boolean | null) =>
  reduce
    ? { initial: false as const, animate: { opacity: 1, y: 0 }, transition: { duration: 0 } }
    : {
        initial: { opacity: 0, y: 24 },
        animate: { opacity: 1, y: 0 },
        transition: {
          y: { type: 'spring' as const, stiffness: 82, damping: 21, delay: 0.15 + i * 0.09 },
          opacity: { duration: 0.7, ease: EASE, delay: 0.15 + i * 0.09 },
        },
      }

const RIGHT_MOTES = Array.from({ length: 8 }, (_, i) => ({
  left: `${((i * 41) % 90) + 5}%`,
  top: `${((i * 53) % 80) + 10}%`,
  size: 1 + (i % 3) * 0.3,
  dur: 42 + ((i * 11) % 28),
  delay: -(i * 5.7) % 35,
}))

/**
 * Page-level CSS that Tailwind utilities cannot express: killing the browser's
 * focus ring without killing keyboard accessibility, and neutralising Chrome's
 * autofill wash.
 */
function AuthStyles() {
  return (
    <style>{`
      /* --- FOCUS -------------------------------------------------------------
         The blue ring is the UA default and has no place in a monochrome film,
         but removing it outright would strand keyboard users. Every control here
         therefore gets an explicit monochrome :focus-visible treatment: the
         inputs draw their own rule + glow (see Field), and everything else takes
         the white hairline ring below. Pointer focus stays quiet, keyboard focus
         is unmistakable. */
      .auth-root :where(input, button, a, [tabindex]):focus { outline: none; }
      .auth-root :where(button, a, [tabindex]):focus-visible {
        outline: none;
        border-radius: 6px;
        box-shadow: 0 0 0 2px var(--admin-content), 0 0 0 3.5px var(--admin-text);
      }
      .auth-root input:focus-visible { outline: none; }

      /* Windows high-contrast throws away box-shadow, so hand the ring back. */
      @media (forced-colors: active) {
        .auth-root :where(input, button, a, [tabindex]):focus-visible {
          outline: 2px solid Highlight;
          outline-offset: 2px;
        }
      }

      /* --- AUTOFILL ----------------------------------------------------------
         Chrome paints autofilled inputs with a pale blue box and near-black text.
         On this page that is both the reported "blue outline" and a contrast
         failure, so the inset shadow repaints the field black and the fill colour
         is forced back to white. The absurd transition delay is the standard trick
         to stop the wash reappearing after the animation restarts. */
      .auth-root input:-webkit-autofill,
      .auth-root input:-webkit-autofill:hover,
      .auth-root input:-webkit-autofill:focus,
      .auth-root input:-webkit-autofill:active {
        -webkit-text-fill-color: var(--admin-text);
        -webkit-box-shadow: 0 0 0 1000px var(--admin-content) inset;
        box-shadow: 0 0 0 1000px var(--admin-content) inset;
        caret-color: var(--admin-text);
        transition: background-color 9999s ease-out 0s;
      }

      .auth-root ::selection { background: color-mix(in srgb, var(--admin-text) 22%, transparent); color: var(--admin-text); }
      .auth-scroll::-webkit-scrollbar { display: none; }
      @keyframes sk-spin { to { transform: rotate(360deg); } }
    `}</style>
  )
}

/**
 * A field drawn as a rule rather than a box: label rides up out of the input on
 * focus or once there is a value, and the focus state is a white rule that grows
 * from the left with a soft bloom. That rule doubles as the keyboard focus
 * indicator, which is why the UA outline can be dropped safely.
 */
function Field({
  label,
  hint,
  action,
  inputRef,
  ...input
}: React.InputHTMLAttributes<HTMLInputElement> & {
  label: string
  hint?: string
  action?: React.ReactNode
  inputRef?: React.Ref<HTMLInputElement>
}) {
  const [focused, setFocused] = useState(false)
  const hasValue = input.value !== undefined && String(input.value).length > 0
  const lifted = focused || hasValue

  return (
    <label className="group relative flex flex-col">
      <span
        className="pointer-events-none absolute left-0 select-none transition-all duration-[550ms]"
        style={{
          top: lifted ? '-2px' : '14px',
          fontSize: lifted ? '10.5px' : '15px',
          letterSpacing: lifted ? '0.15em' : '0.01em',
          textTransform: lifted ? 'uppercase' : 'none',
          fontWeight: lifted ? 700 : 400,
          color: focused ? 'var(--admin-text)' : lifted ? TXT.tertiary : TXT.rest,
          transitionTimingFunction: 'cubic-bezier(0.16,1,0.3,1)',
        }}
      >
        {label}
      </span>
      {action && <span className="absolute right-0 top-0 z-10">{action}</span>}
      <input
        ref={inputRef}
        {...input}
        onFocus={(e) => {
          setFocused(true)
          input.onFocus?.(e)
        }}
        onBlur={(e) => {
          setFocused(false)
          input.onBlur?.(e)
        }}
        className="w-full appearance-none border-0 bg-transparent pb-[13px] pt-[26px] text-[15px] tracking-wide text-[color:var(--admin-text)] caret-[color:var(--admin-text)] disabled:opacity-50"
      />
      <span aria-hidden className="absolute inset-x-0 bottom-0 h-px bg-[color:var(--admin-border)]" />
      <span
        aria-hidden
        className="absolute inset-x-0 bottom-0 h-px origin-left transition-all duration-700"
        style={{
          transform: `scaleX(${focused ? 1 : 0})`,
          background: 'linear-gradient(90deg,var(--admin-text),color-mix(in srgb,var(--admin-text) 55%,transparent))',
          boxShadow: focused ? '0 0 18px color-mix(in srgb,var(--admin-text) 22%,transparent)' : 'none',
          transitionTimingFunction: 'cubic-bezier(0.16,1,0.3,1)',
        }}
      />
      {hint && (
        <span className="mt-2.5 text-[11.5px] tracking-wide" style={{ color: TXT.tertiary }}>
          {hint}
        </span>
      )}
    </label>
  )
}

function AuthPage({ mode }: { mode: Mode }) {
  const { login, register, confirmEmail } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const isRegister = mode === 'register'
  const reduce = useReducedMotion()
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'

  // 'confirm' is reached two ways: registering (always needs the code) or logging into an account
  // that registered but never confirmed (login returns requiresEmailConfirmation instead of 401).
  const [stage, setStage] = useState<'credentials' | 'confirm'>('credentials')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [code, setCode] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [success, setSuccess] = useState(false)
  const [resent, setResent] = useState(false)
  const emailRef = useRef<HTMLInputElement>(null)
  const codeRef = useRef<HTMLInputElement>(null)
  const rightRef = useRef<HTMLElement>(null)

  useEffect(() => {
    emailRef.current?.focus()
  }, [])
  useEffect(() => {
    setError('')
    setConfirmPassword('')
    setStage('credentials')
  }, [mode])
  useEffect(() => {
    if (stage === 'confirm') codeRef.current?.focus()
  }, [stage])

  // Pointer parallax, capped at 5px. One listener writing two custom properties;
  // every consumer below is a compositor-only transform reading them.
  useEffect(() => {
    const el = rightRef.current
    if (!el) return
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) return
    if (window.matchMedia?.('(pointer:coarse)').matches) return
    const handler = (e: MouseEvent) => {
      const x = (e.clientX / window.innerWidth - 0.5) * 2
      const y = (e.clientY / window.innerHeight - 0.5) * 2
      el.style.setProperty('--mx', `${(x * 5).toFixed(2)}px`)
      el.style.setProperty('--my', `${(y * 5).toFixed(2)}px`)
    }
    window.addEventListener('mousemove', handler, { passive: true })
    return () => window.removeEventListener('mousemove', handler)
  }, [])

  function routeAfterAuth(roles: string[]) {
    // A returning visit to a protected page goes back there; a fresh sign-in
    // routes by role. A plain User with no StorePartner/Admin role: a brand-new
    // registration has nothing to do yet but create their store, so send them
    // straight into onboarding — an existing account logging in goes to the
    // consumer app (must not change for old accounts that deliberately have no
    // store; before /app existed that destination was the landing page).
    const from = (location.state as { from?: Location })?.from?.pathname
    const fallback = roles.includes('Admin')
      ? '/admin/moderation'
      : roles.includes('StorePartner')
        ? '/admin'
        : isRegister
          ? '/admin/onboarding'
          : '/app'
    navigate(from ?? fallback, { replace: true })
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')

    if (isRegister && password !== confirmPassword) {
      setError('Пароли не совпадают')
      return
    }

    setLoading(true)
    if (isRegister) {
      const result = await register(email, password)
      setLoading(false)
      if (!result.ok) {
        setError(result.error)
        return
      }
      // Register never logs in directly anymore — always needs the emailed code confirmed first.
      setStage('confirm')
      return
    }

    const result = await login(email, password)
    setLoading(false)
    if (!result.ok) {
      setError(result.error)
      // A correct password but an unconfirmed account lands here with a fresh code already
      // sent (see LoginAsync) — same next screen as a brand-new registration.
      if (result.requiresEmailConfirmation) {
        setEmail(result.email ?? email)
        setStage('confirm')
      }
      return
    }
    setSuccess(true)
    routeAfterAuth(result.roles)
  }

  async function handleConfirmSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    const result = await confirmEmail(email, code)
    setLoading(false)
    if (!result.ok) {
      setError(result.error)
      return
    }
    setSuccess(true)
    routeAfterAuth(result.roles)
  }

  async function handleResend() {
    setError('')
    setLoading(true)
    const result = await register(email, password)
    setLoading(false)
    if (!result.ok) {
      setError(result.error)
      return
    }
    setResent(true)
    setTimeout(() => setResent(false), 4000)
  }
  /* ---- /auth ---- */

  const busy = loading || success
  const headlineWords = isRegister ? ['Создайте', 'аккаунт'] : ['С', 'возвращением']

  return (
    <div
      className="auth-root admin-shell overflow-hidden bg-[color:var(--admin-content)] text-[color:var(--admin-text)]"
      /* dvh, not vh: on mobile the URL bar makes 100vh taller than the visible
         area, which would push the CTA off-screen on a page that must not scroll. */
      style={{ height: '100dvh' }}
    >
      <AtmosphereKeyframes />
      <AuthStyles />

      <div className="grid h-full lg:grid-cols-[1.05fr_1fr]">
        {/* ── STORY (preserved) ─────────────────────────────────── */}
        <section className="relative hidden overflow-hidden border-r border-[color:var(--admin-border)] lg:flex lg:flex-col lg:justify-between lg:p-14 xl:p-20">
          <Atmosphere />

          <motion.div {...rise(0, reduce)} className="relative">
            <Link to="/" className="inline-flex items-center gap-3">
              <span className="grid h-9 w-9 place-items-center rounded-[11px] bg-[color:var(--admin-text)] text-[16px] font-extrabold tracking-tight text-[color:var(--admin-content)]">
                S
              </span>
              <span className="text-[18px] font-bold tracking-tight">Sarfkor</span>
            </Link>
          </motion.div>

          <div className="relative max-w-[520px]">
            {['Экономьте время.', 'Экономьте деньги.'].map((line, i) => (
              <motion.h2
                key={line}
                {...rise(1 + i, reduce)}
                className="text-[clamp(32px,3.4vw,52px)] font-extrabold leading-[1.03] tracking-[-0.03em]"
              >
                {line}
              </motion.h2>
            ))}
            <motion.p
              {...rise(3, reduce)}
              className="mt-7 max-w-[400px] text-[16px] leading-relaxed"
              style={{ color: TXT.secondary }}
            >
              Sarfkor находит лучшую цену за секунды — по штрихкоду, во всех магазинах рядом с
              вами.
            </motion.p>
          </div>

          <motion.div {...rise(4, reduce)} className="relative">
            <AuthStats />
          </motion.div>
        </section>

        {/* ── FORM ──────────────────────────────────────────────── */}
        <section ref={rightRef} className="relative flex h-full min-h-0 flex-col">
          {/* desktop atmosphere: two parallax lights, motes, grain, vignette.
              currentColor-based (via the text-[--admin-text] wrapper) so the
              same layers re-tint under either theme, same trick as Atmosphere. */}
          <div
            className="pointer-events-none absolute inset-0 hidden overflow-hidden text-[color:var(--admin-text)] lg:block"
            aria-hidden
          >
            <div
              className="absolute h-[60vmax] w-[60vmax] rounded-full opacity-[0.05]"
              style={{
                top: '-20%',
                right: '-15%',
                background: 'radial-gradient(circle,currentColor,transparent 65%)',
                filter: 'blur(50px)',
                transform: 'translate(var(--mx,0px),var(--my,0px))',
                transition: 'transform .6s cubic-bezier(.16,1,.3,1)',
              }}
            />
            <div
              className="absolute h-[45vmax] w-[45vmax] rounded-full opacity-[0.03]"
              style={{
                bottom: '-25%',
                left: '-10%',
                background: 'radial-gradient(circle,currentColor,transparent 60%)',
                filter: 'blur(60px)',
                transform: 'translate(calc(var(--mx,0px)*-.6),calc(var(--my,0px)*-.6))',
                transition: 'transform .9s cubic-bezier(.16,1,.3,1)',
              }}
            />
            {RIGHT_MOTES.map((m, i) => (
              <span
                key={i}
                className="absolute rounded-full bg-[currentColor]"
                style={{
                  left: m.left,
                  top: m.top,
                  width: m.size,
                  height: m.size,
                  opacity: 0,
                  animation: `sk-rise ${m.dur}s linear ${m.delay}s infinite`,
                }}
              />
            ))}
            <div
              className="absolute inset-0 opacity-[0.06] mix-blend-overlay"
              style={{
                backgroundImage:
                  "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='140' height='140'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.85' numOctaves='3'/%3E%3C/filter%3E%3Crect width='140' height='140' filter='url(%23n)' opacity='.5'/%3E%3C/svg%3E\")",
              }}
            />
            <div
              className="absolute inset-0"
              style={{
                background:
                  'radial-gradient(ellipse 130% 100% at 50% 45%,transparent 45%,rgba(0,0,0,.5) 100%)',
              }}
            />
          </div>

          <div className="pointer-events-none absolute inset-0 lg:hidden">
            <Atmosphere />
          </div>

          {/* top bar */}
          <div className="relative z-10 flex shrink-0 items-center justify-between px-6 py-6 sm:px-10 lg:px-14 xl:px-20">
            <Link to="/" className="flex items-center gap-2.5 lg:hidden">
              <span className="grid h-8 w-8 place-items-center rounded-[10px] bg-[color:var(--admin-text)] text-[15px] font-extrabold tracking-tight text-[color:var(--admin-content)]">
                S
              </span>
              <span className="text-[17px] font-bold tracking-tight">Sarfkor</span>
            </Link>
            <div className="ml-auto flex items-center gap-4">
              <button
                onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
                aria-label="Переключить тему"
                className="grid h-8 w-8 shrink-0 place-items-center rounded-lg transition-colors duration-300 hover:bg-[color:var(--admin-hover)]"
                style={{ color: TXT.tertiary }}
              >
                {isDark ? <SunIcon width={16} height={16} /> : <MoonIcon width={16} height={16} />}
              </button>
              <Link
                to="/"
                className="text-[11.5px] font-semibold uppercase tracking-[0.14em] transition-colors duration-500 hover:text-[color:var(--admin-text)]"
                style={{ color: TXT.tertiary }}
              >
                На главную
              </Link>
            </div>
          </div>

          {/* Centre column. The page itself never scrolls; this is the only element
              allowed to, and only when a very short viewport leaves no choice. */}
          <div
            className="auth-scroll relative flex min-h-0 flex-1 items-center justify-center overflow-y-auto px-6 sm:px-10 lg:px-14 xl:px-20"
            style={{ scrollbarWidth: 'none', paddingBlock: 'clamp(8px,2vh,24px)' }}
          >
            <div
              className="w-full max-w-[380px]"
              style={{
                transform: 'translate(calc(var(--mx,0px)*.25),calc(var(--my,0px)*.25))',
                transition: 'transform .5s cubic-bezier(.16,1,.3,1)',
              }}
            >
              <motion.p
                {...rise(0, reduce)}
                className="text-[10.5px] font-bold uppercase tracking-[0.28em]"
                style={{ color: TXT.tertiary, marginBottom: 'clamp(14px,2.4vh,28px)' }}
              >
                {stage === 'confirm' ? 'Подтверждение' : isRegister ? 'Регистрация' : 'Вход'}
              </motion.p>

              {/* headline, one word at a time — the second line follows the first up */}
              <div style={{ marginBottom: 'clamp(10px,1.8vh,20px)' }}>
                {(stage === 'confirm' ? ['Проверьте', 'почту'] : headlineWords).map((word, i) => (
                  <motion.span
                    key={`${mode}-${stage}-${word}`}
                    initial={reduce ? false : { opacity: 0, y: 30 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={
                      reduce
                        ? { duration: 0 }
                        : {
                            y: { type: 'spring', stiffness: 72, damping: 17, delay: 0.26 + i * 0.15 },
                            opacity: { duration: 0.75, ease: EASE, delay: 0.26 + i * 0.15 },
                          }
                    }
                    className="block font-extrabold leading-[1.06] tracking-[-0.03em]"
                    style={{ fontSize: 'clamp(28px,min(4vw,5.2vh),46px)' }}
                  >
                    {word}
                  </motion.span>
                ))}
              </div>

              <motion.p
                {...rise(3, reduce)}
                className="max-w-[330px] text-[14.5px] leading-[1.65]"
                style={{ color: TXT.secondary, marginBottom: 'clamp(22px,4.6vh,54px)' }}
              >
                {stage === 'confirm' ? (
                  <>
                    Мы отправили код на <strong className="font-semibold text-white">{email}</strong>. Введите его ниже,
                    чтобы подтвердить email и войти.
                  </>
                ) : isRegister ? (
                  'Несколько секунд — и вы сможете подключить магазин к Sarfkor.'
                ) : (
                  'Войдите, чтобы вернуться к своему магазину и отчётам.'
                )}
              </motion.p>

              <form
                onSubmit={handleSubmit}
                className="flex flex-col"
                style={{ gap: 'clamp(18px,3.2vh,34px)' }}
              >
                <motion.div {...rise(4, reduce)}>
                  <Field
                    label="Email"
                    inputRef={emailRef}
                    type="email"
                    required
                    disabled={busy}
                    autoComplete="username"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                </motion.div>

                <motion.div {...rise(5, reduce)}>
                  <Field
                    label="Пароль"
                    hint={isRegister ? 'Минимум 8 символов' : undefined}
                    action={
                      <button
                        type="button"
                        onClick={() => setShowPassword((v) => !v)}
                        className="text-[10.5px] font-bold uppercase tracking-[0.1em] transition-colors duration-300 hover:text-[color:var(--admin-text)]"
                        style={{ color: TXT.tertiary }}
              {stage === 'confirm' ? (
                <form onSubmit={handleConfirmSubmit} className="flex flex-col" style={{ gap: 'clamp(18px,3.2vh,34px)' }}>
                  <motion.div {...rise(4, reduce)}>
                    <Field
                      label="Код из письма"
                      inputRef={codeRef}
                      type="text"
                      inputMode="numeric"
                      pattern="[0-9]{6}"
                      maxLength={6}
                      required
                      disabled={busy}
                      autoComplete="one-time-code"
                      value={code}
                      onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                    />
                  </motion.div>

                  <AnimatePresence initial={false}>
                    {error && (
                      <motion.div
                        role="alert"
                        key={error}
                        initial={{ opacity: 0, y: -6, height: 0 }}
                        animate={{ opacity: 1, y: 0, height: 'auto' }}
                        exit={{ opacity: 0, height: 0 }}
                        transition={{ duration: 0.4, ease: EASE }}
                        style={{ overflow: 'hidden' }}
                      >
                        <span className="block border-l-2 border-white pl-4 text-[13px] font-semibold leading-relaxed text-white">
                          {error}
                        </span>
                      </motion.div>
                    )}
                  </AnimatePresence>

                  <AnimatePresence initial={false}>
                    {resent && (
                      <motion.div
                        initial={{ opacity: 0, y: -6, height: 0 }}
                        animate={{ opacity: 1, y: 0, height: 'auto' }}
                        exit={{ opacity: 0, height: 0 }}
                        transition={{ duration: 0.4, ease: EASE }}
                        style={{ overflow: 'hidden' }}
                      >
                        <span className="block text-[13px] font-medium" style={{ color: TXT.secondary }}>
                          Новый код отправлен
                        </span>
                      </motion.div>
                    )}
                  </AnimatePresence>

                  <motion.div {...rise(6, reduce)} style={{ paddingTop: 'clamp(2px,1.2vh,10px)' }}>
                    <button
                      type="submit"
                      disabled={busy || code.length !== 6}
                      className="group relative w-full overflow-hidden rounded-full bg-white text-[14.5px] font-semibold tracking-wide text-black transition-all duration-700 hover:-translate-y-0.5 hover:shadow-[0_12px_38px_rgba(255,255,255,0.14)] active:translate-y-0 active:scale-[0.985] disabled:pointer-events-none disabled:opacity-90"
                      style={{ paddingBlock: 'clamp(13px,2vh,17px)', transitionTimingFunction: 'cubic-bezier(.16,1,.3,1)' }}
                    >
                      {/* monochrome, per the no-accent-colour rule: the alert reads as
                          an alert through weight, a white rule and position, not hue */}
                      <span className="block border-l-2 border-[color:var(--admin-text)] pl-4 text-[13px] font-semibold leading-relaxed text-[color:var(--admin-text)]">
                        {error}
                      </span>
                    </motion.div>
                  )}
                </AnimatePresence>

                <motion.div {...rise(6, reduce)} style={{ paddingTop: 'clamp(2px,1.2vh,10px)' }}>
                  <button
                    type="submit"
                    disabled={busy}
                    className="group relative w-full overflow-hidden rounded-full bg-[color:var(--admin-text)] text-[14.5px] font-semibold tracking-wide text-[color:var(--admin-content)] transition-all duration-700 hover:-translate-y-0.5 hover:shadow-[0_12px_38px_color-mix(in_srgb,var(--admin-text)_14%,transparent)] active:translate-y-0 active:scale-[0.985] disabled:pointer-events-none disabled:opacity-90"
                    style={{
                      paddingBlock: 'clamp(13px,2vh,17px)',
                      transitionTimingFunction: 'cubic-bezier(.16,1,.3,1)',
                    }}
                  >
                    <span
                      aria-hidden
                      className="pointer-events-none absolute inset-0 rounded-full opacity-0 transition-opacity duration-500 group-hover:opacity-100"
                      style={{
                        background: 'linear-gradient(180deg,color-mix(in srgb,var(--admin-content) 85%,transparent) 0%,transparent 55%)',
                        mixBlendMode: 'overlay',
                      }}
                    />
                    <span className="relative flex items-center justify-center gap-2.5">
                      {busy ? (
                        <>
                          <span
                            aria-hidden
                            className="h-[15px] w-[15px] rounded-full border-2 border-[color:var(--admin-content)]/25 border-t-[color:var(--admin-content)]"
                            style={{ animation: 'sk-spin .7s linear infinite' }}
                          />
                          {success ? 'Готово' : 'Подождите…'}
                        </>
                      ) : (
                        <>
                          {isRegister ? 'Создать аккаунт' : 'Войти'}
                          <span
                            aria-hidden
                            className="transition-transform duration-700 group-hover:translate-x-1.5"
                            style={{ transitionTimingFunction: 'cubic-bezier(.16,1,.3,1)' }}
                          >
                            →
                          </span>
                        </>
                      )}
                    </span>
                  </button>
                </motion.div>
              </form>
                      <span className="relative flex items-center justify-center gap-2.5">
                        {busy ? (
                          <>
                            <span
                              aria-hidden
                              className="h-[15px] w-[15px] rounded-full border-2 border-black/25 border-t-black"
                              style={{ animation: 'sk-spin .7s linear infinite' }}
                            />
                            {success ? 'Готово' : 'Подождите…'}
                          </>
                        ) : (
                          <>
                            Подтвердить
                            <span aria-hidden className="transition-transform duration-700 group-hover:translate-x-1.5">
                              →
                            </span>
                          </>
                        )}
                      </span>
                    </button>
                  </motion.div>
                </form>
              ) : (
                <form
                  onSubmit={handleSubmit}
                  className="flex flex-col"
                  style={{ gap: 'clamp(18px,3.2vh,34px)' }}
                >
                  <motion.div {...rise(4, reduce)}>
                    <Field
                      label="Email"
                      inputRef={emailRef}
                      type="email"
                      required
                      disabled={busy}
                      autoComplete="username"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                    />
                  </motion.div>

                  <motion.div {...rise(5, reduce)}>
                    <Field
                      label="Пароль"
                      hint={isRegister ? 'Минимум 8 символов, заглавная и строчная буквы, цифра и спецсимвол' : undefined}
                      action={
                        <button
                          type="button"
                          onClick={() => setShowPassword((v) => !v)}
                          className="text-[10.5px] font-bold uppercase tracking-[0.1em] transition-colors duration-300 hover:text-white"
                          style={{ color: TXT.tertiary }}
                        >
                          {showPassword ? 'Скрыть' : 'Показать'}
                        </button>
                      }
                      type={showPassword ? 'text' : 'password'}
                      required
                      minLength={8}
                      disabled={busy}
                      autoComplete={isRegister ? 'new-password' : 'current-password'}
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                    />
                    {!isRegister && (
                      <Link
                        to="/forgot-password"
                        className="mt-2.5 block text-right text-[11.5px] font-semibold tracking-wide transition-colors duration-300 hover:text-white"
                        style={{ color: TXT.tertiary }}
                      >
                        Забыли пароль?
                      </Link>
                    )}
                  </motion.div>

                  <AnimatePresence initial={false}>
                    {isRegister && (
                      <motion.div
                        key="confirm"
                        initial={{ opacity: 0, height: 0 }}
                        animate={{ opacity: 1, height: 'auto' }}
                        exit={{ opacity: 0, height: 0 }}
                        transition={{ duration: 0.6, ease: EASE }}
                        style={{ overflow: 'hidden' }}
                      >
                        <Field
                          label="Повторите пароль"
                          type={showPassword ? 'text' : 'password'}
                          required
                          disabled={busy}
                          autoComplete="new-password"
                          value={confirmPassword}
                          onChange={(e) => setConfirmPassword(e.target.value)}
                        />
                      </motion.div>
                    )}
                  </AnimatePresence>

                  <AnimatePresence initial={false}>
                    {error && (
                      <motion.div
                        role="alert"
                        key={error}
                        initial={{ opacity: 0, y: -6, height: 0 }}
                        animate={{ opacity: 1, y: 0, height: 'auto' }}
                        exit={{ opacity: 0, height: 0 }}
                        transition={{ duration: 0.4, ease: EASE }}
                        style={{ overflow: 'hidden' }}
                      >
                        {/* monochrome, per the no-accent-colour rule: the alert reads as
                            an alert through weight, a white rule and position, not hue */}
                        <span className="block border-l-2 border-white pl-4 text-[13px] font-semibold leading-relaxed text-white">
                          {error}
                        </span>
                      </motion.div>
                    )}
                  </AnimatePresence>

                  <motion.div {...rise(6, reduce)} style={{ paddingTop: 'clamp(2px,1.2vh,10px)' }}>
                    <button
                      type="submit"
                      disabled={busy}
                      className="group relative w-full overflow-hidden rounded-full bg-white text-[14.5px] font-semibold tracking-wide text-black transition-all duration-700 hover:-translate-y-0.5 hover:shadow-[0_12px_38px_rgba(255,255,255,0.14)] active:translate-y-0 active:scale-[0.985] disabled:pointer-events-none disabled:opacity-90"
                      style={{
                        paddingBlock: 'clamp(13px,2vh,17px)',
                        transitionTimingFunction: 'cubic-bezier(.16,1,.3,1)',
                      }}
                    >
                      <span
                        aria-hidden
                        className="pointer-events-none absolute inset-0 rounded-full opacity-0 transition-opacity duration-500 group-hover:opacity-100"
                        style={{
                          background: 'linear-gradient(180deg,rgba(255,255,255,.85) 0%,transparent 55%)',
                          mixBlendMode: 'overlay',
                        }}
                      />
                      <span className="relative flex items-center justify-center gap-2.5">
                        {busy ? (
                          <>
                            <span
                              aria-hidden
                              className="h-[15px] w-[15px] rounded-full border-2 border-black/25 border-t-black"
                              style={{ animation: 'sk-spin .7s linear infinite' }}
                            />
                            {success ? 'Готово' : 'Подождите…'}
                          </>
                        ) : (
                          <>
                            {isRegister ? 'Создать аккаунт' : 'Войти'}
                            <span
                              aria-hidden
                              className="transition-transform duration-700 group-hover:translate-x-1.5"
                              style={{ transitionTimingFunction: 'cubic-bezier(.16,1,.3,1)' }}
                            >
                              →
                            </span>
                          </>
                        )}
                      </span>
                    </button>
                  </motion.div>
                </form>
              )}

              <motion.p
                {...rise(7, reduce)}
                className="text-[13px]"
                style={{ color: TXT.tertiary, marginTop: 'clamp(18px,3.6vh,44px)' }}
              >
                {isRegister ? 'Уже есть аккаунт? ' : 'Ещё нет аккаунта? '}
                <Link
                  to={isRegister ? '/login' : '/register'}
                  className="font-semibold text-[color:var(--admin-text)] underline decoration-[color:var(--admin-border)] underline-offset-[6px] transition-all duration-500 hover:decoration-[color:var(--admin-text)]"
                >
                  {isRegister ? 'Войти' : 'Зарегистрироваться'}
                </Link>
              </motion.p>
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}

export function LoginPage() {
  return <AuthPage mode="login" />
}

export function RegisterPage() {
  return <AuthPage mode="register" />
}
