import { useCallback, useEffect, useRef, useState, type CSSProperties, type ReactNode } from 'react'
import { motion, useReducedMotion } from 'framer-motion'
import { ApiError } from '../lib/api'

/**
 * The consumer app's visual system. Previously permanently monochrome-black
 * (no theme support at all); now themes via the same `.dark`/`.light` class
 * ThemeProvider already flips app-wide, through the `--app-*` tokens in
 * src/index.css (`:root`/`.dark`) rather than the merchant app's `--admin-*`
 * namespace, since this surface has its own distinct visual language.
 *
 * TXT/LINE keep their exact former shape (same keys, same call sites) but now
 * point at CSS custom properties instead of hardcoded hex — every existing
 * `style={{color: TXT.rest}}` across the app re-themes for free. Each step's
 * alpha is still chosen to clear the same WCAG AA floor in both directions
 * (see index.css for the per-theme values).
 */
export const EASE = [0.16, 1, 0.3, 1] as const

export const TXT = {
  primary: 'var(--app-text-primary)',
  secondary: 'var(--app-text-secondary)',
  tertiary: 'var(--app-text-tertiary)',
  rest: 'var(--app-text-rest)',
}

export const LINE = 'var(--app-line)'
export const LINE_SOFT = 'var(--app-line-soft)'

/** Page-level CSS that utilities cannot express. Mounted once by AppShell. */
export function AppStyles() {
  return (
    <style>{`
      .sk-app :where(input,button,a,select,textarea,[tabindex]):focus { outline: none; }
      .sk-app :where(input,button,a,select,textarea,[tabindex]):focus-visible {
        outline: none; border-radius: 8px;
        box-shadow: 0 0 0 2px var(--bg-app), 0 0 0 3.5px var(--app-text-primary);
      }
      @media (forced-colors: active) {
        .sk-app :where(input,button,a,select,textarea,[tabindex]):focus-visible {
          outline: 2px solid Highlight; outline-offset: 2px;
        }
      }
      .sk-app input:-webkit-autofill,
      .sk-app input:-webkit-autofill:focus {
        -webkit-text-fill-color: var(--app-text-primary);
        -webkit-box-shadow: 0 0 0 1000px var(--bg-app) inset;
        caret-color: var(--app-text-primary);
        transition: background-color 9999s ease-out 0s;
      }
      .sk-app ::selection { background: color-mix(in srgb, var(--app-text-primary) 22%, transparent); color: var(--app-text-primary); }
      .sk-scroll::-webkit-scrollbar { width: 0; height: 0; }
      @keyframes sk-shimmer { 100% { transform: translateX(100%); } }
      @keyframes sk-spin { to { transform: rotate(360deg); } }
      @media (prefers-reduced-motion: reduce) {
        .sk-app [data-shimmer]::after { animation: none !important; }
      }
    `}</style>
  )
}

/* ---------------------------------------------------------------- motion --- */

/** Staggered entrance. Index drives the delay so a page reads top-to-bottom. */
export function Reveal({
  i = 0,
  children,
  className,
}: {
  i?: number
  children: ReactNode
  className?: string
}) {
  const reduce = useReducedMotion()
  if (reduce) return <div className={className}>{children}</div>
  return (
    <motion.div
      className={className}
      initial={{ opacity: 0, y: 18 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{
        y: { type: 'spring', stiffness: 84, damping: 22, delay: 0.06 + i * 0.07 },
        opacity: { duration: 0.6, ease: EASE, delay: 0.06 + i * 0.07 },
      }}
    >
      {children}
    </motion.div>
  )
}

/** Editorial heading reveal: the line rises out of its own clipped box. */
export function MaskReveal({
  children,
  delay = 0,
  className,
}: {
  children: ReactNode
  delay?: number
  className?: string
}) {
  const reduce = useReducedMotion()
  if (reduce) return <span className={className}>{children}</span>
  return (
    <span className="block overflow-hidden">
      <motion.span
        className={`block ${className ?? ''}`}
        initial={{ y: '110%' }}
        animate={{ y: '0%' }}
        transition={{ duration: 1.05, ease: EASE, delay }}
      >
        {children}
      </motion.span>
    </span>
  )
}

/**
 * Counts to `to` once on mount.
 *
 * No "have I started" ref on purpose: a ref survives StrictMode's
 * mount/unmount/mount, so the first pass would claim it, the cleanup would cancel
 * the animation, and the second — real — pass would early-return, leaving the
 * figure stuck at zero. The cleanup is the only guard needed.
 */
export function CountUp({ to, decimals = 0 }: { to: number; decimals?: number }) {
  const reduce = useReducedMotion()
  const [v, setV] = useState(reduce ? to : 0)

  useEffect(() => {
    if (reduce) {
      setV(to)
      return
    }
    let raf = 0
    let done = false
    const DUR = 1100
    const t0 = performance.now()
    const step = (now: number) => {
      const p = Math.min(1, (now - t0) / DUR)
      setV(to * (1 - Math.pow(1 - p, 3)))
      if (p < 1) raf = requestAnimationFrame(step)
      else done = true
    }
    raf = requestAnimationFrame(step)
    // rAF is suspended while the document is not rendered, so without this a tab
    // restored from the background would show a permanent zero.
    const guard = setTimeout(() => {
      if (!done) {
        cancelAnimationFrame(raf)
        setV(to)
      }
    }, DUR + 1200)
    return () => {
      cancelAnimationFrame(raf)
      clearTimeout(guard)
    }
  }, [to, reduce])

  return <>{fmt(v, decimals)}</>
}

/* ------------------------------------------------------------ formatting --- */

export function fmt(n: number, decimals = 0) {
  return new Intl.NumberFormat('ru-RU', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(n)
}

/** Prices arrive as decimals with a separate ISO currency; TJS reads as "с." */
export function money(n: number, currency = 'TJS') {
  const label = currency === 'TJS' ? 'с.' : currency
  return `${fmt(n, Number.isInteger(n) ? 0 : 2)} ${label}`
}

export function km(d?: number | null) {
  if (d === undefined || d === null) return null
  return d < 1 ? `${Math.round(d * 1000)} м` : `${fmt(d, 1)} км`
}

/* ------------------------------------------------------------------ data --- */

export interface AsyncState<T> {
  data: T | null
  loading: boolean
  error: string
  reload: () => void
}

/**
 * One loading/error contract for every page, so no screen invents its own. The
 * message prefers the backend's own wording (ApiError carries the parsed
 * ProblemDetails title or bare string body) and only falls back when there is none.
 */
export function useAsync<T>(fn: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [nonce, setNonce] = useState(0)
  const [state, setState] = useState<{ data: T | null; loading: boolean; error: string }>({
    data: null,
    loading: true,
    error: '',
  })
  const fnRef = useRef(fn)
  fnRef.current = fn

  useEffect(() => {
    let cancelled = false
    setState((s) => ({ ...s, loading: true, error: '' }))
    fnRef
      .current()
      .then((d) => {
        if (!cancelled) setState({ data: d, loading: false, error: '' })
      })
      .catch((e: unknown) => {
        if (cancelled) return
        const msg =
          e instanceof ApiError
            ? e.status === 0 || e.message.includes('fetch')
              ? 'Не удаётся связаться с сервером'
              : e.message
            : 'Не удалось загрузить данные'
        setState({ data: null, loading: false, error: msg })
      })
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, nonce])

  const reload = useCallback(() => setNonce((n) => n + 1), [])
  return { ...state, reload }
}

/* ------------------------------------------------------------------ bits --- */

/**
 * The one card/panel surface. Before this, HomePage/ScanPage/ProductPage each
 * inlined `rounded-2xl border` with their own one-off border color — usually
 * `LINE`, but a couple of call sites drifted to a stray `rgba(255,255,255,0.16)`.
 * This doesn't force a single padding scale (callers still pass their own
 * `p-*` via `className`, since that legitimately varies by content), it just
 * makes the border/radius impossible to get inconsistent again.
 */
export function Surface({
  children,
  className = '',
  style,
}: {
  children: ReactNode
  className?: string
  style?: CSSProperties
}) {
  return (
    <div className={`rounded-2xl border ${className}`} style={{ borderColor: LINE, ...style }}>
      {children}
    </div>
  )
}

export function Skeleton({ h = 16, w = '100%', className = '' }: { h?: number; w?: string | number; className?: string }) {
  return (
    <span
      data-shimmer
      aria-hidden
      className={`relative block overflow-hidden rounded-md ${className}`}
      style={{ height: h, width: w, background: 'color-mix(in srgb, var(--app-text-primary) 5.5%, transparent)' }}
    >
      <span
        className="absolute inset-0 -translate-x-full"
        style={{
          background:
            'linear-gradient(90deg,transparent,color-mix(in srgb,var(--app-text-primary) 7%,transparent),transparent)',
          animation: 'sk-shimmer 1.6s ease-in-out infinite',
        }}
      />
    </span>
  )
}

export function EmptyState({
  title,
  body,
  action,
}: {
  title: string
  body?: string
  action?: ReactNode
}) {
  return (
    <div className="flex flex-col items-center px-6 py-20 text-center">
      <span
        aria-hidden
        className="mb-6 block h-px w-10"
        style={{ background: 'linear-gradient(90deg,transparent,color-mix(in srgb,var(--app-text-primary) 40%,transparent),transparent)' }}
      />
      <p className="text-[16px] font-semibold tracking-tight text-[color:var(--app-text-primary)]">{title}</p>
      {body && (
        <p className="mt-2.5 max-w-[340px] text-[13.5px] leading-relaxed" style={{ color: TXT.rest }}>
          {body}
        </p>
      )}
      {action && <div className="mt-7">{action}</div>}
    </div>
  )
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div role="alert" className="flex flex-col items-center px-6 py-20 text-center">
      <p className="text-[15px] font-semibold text-[color:var(--app-text-primary)]">Что-то пошло не так</p>
      <p className="mt-2.5 max-w-[340px] text-[13.5px] leading-relaxed" style={{ color: TXT.secondary }}>
        {message}
      </p>
      {onRetry && (
        <button
          onClick={onRetry}
          className="mt-7 rounded-full border px-5 py-2.5 text-[13px] font-semibold text-[color:var(--app-text-primary)] transition-colors duration-300 hover:bg-[color:var(--app-text-primary)] hover:text-[color:var(--bg-app)]"
          style={{ borderColor: 'color-mix(in srgb, var(--app-text-primary) 22%, transparent)' }}
        >
          Попробовать снова
        </button>
      )}
    </div>
  )
}

/** Primary action. Capsule, inverse-of-background, quiet lift — same as the
 *  landing CTA. Primary genuinely inverts polarity per theme (white-on-black
 *  in dark, black-on-white in light), not just a tint shift, so both var()
 *  references are load-bearing here, not decorative. */
export function Button({
  children,
  variant = 'primary',
  className = '',
  ...rest
}: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'ghost' }) {
  const base =
    'inline-flex items-center justify-center gap-2 rounded-[8px] px-5 py-[10px] text-[14px] font-[500] transition-all duration-150 ease-out disabled:opacity-45 disabled:pointer-events-none'
  const skin =
    variant === 'primary'
      ? 'bg-[color:var(--app-text-primary)] text-[color:var(--bg-app)] hover:scale-[1.01] hover:shadow-[0_4px_20px_color-mix(in_srgb,var(--app-text-primary)_20%,transparent)] active:scale-[0.99]'
      : 'text-[color:var(--app-text-secondary)] border border-[color:var(--app-line)] hover:text-[color:var(--app-text-primary)] hover:border-[color:var(--app-line)] active:scale-[0.99]'
  return (
    <button
      {...rest}
      className={`${base} ${skin} ${className}`}
      style={rest.style}
    >
      {children}
    </button>
  )
}

/**
 * `dark` used to mean "black spinner, for use on the (always white) primary
 * Button" — now that primary Button inverts polarity per theme, `dark` means
 * "match the Button's own inverted surface" instead, via var(--bg-app) rather
 * than a literal black. The two call sites (busy state inside a primary
 * Button) stay correct in both themes without the caller doing anything.
 */
export function Spinner({ dark = false }: { dark?: boolean }) {
  const fg = dark ? 'var(--bg-app)' : 'var(--app-text-primary)'
  return (
    <span
      aria-hidden
      className="inline-block h-[15px] w-[15px] shrink-0 rounded-full border-2"
      style={{
        borderColor: `color-mix(in srgb, ${fg} 22%, transparent)`,
        borderTopColor: fg,
        animation: 'sk-spin 0.7s linear infinite',
      }}
    />
  )
}

/** Section heading used across the consumer pages. */
export function SectionTitle({ children, right }: { children: ReactNode; right?: ReactNode }) {
  return (
    <div className="mb-5 flex items-baseline justify-between gap-4">
      <h2
        className="text-[11px] font-bold uppercase tracking-[0.2em]"
        style={{ color: TXT.rest }}
      >
        {children}
      </h2>
      {right}
    </div>
  )
}
