import { useEffect, useState } from 'react'

/**
 * Shared presentation pieces for /login and /register. Nothing here touches auth
 * state — it exists so both routes render one composition instead of two.
 *
 * Everything moves on a 30–70s cycle. At that speed the panel never reads as
 * "animated", only as never quite still, which is the difference between a
 * background that feels alive and one that feels busy.
 */

const REDUCED = () =>
  typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches

/** Injected once: these cycles are far outside Tailwind's animation scale. */
export function AtmosphereKeyframes() {
  return (
    <style>{`
      @keyframes sk-drift-a { 0%,100%{transform:translate3d(-6%,-4%,0) scale(1)} 50%{transform:translate3d(6%,5%,0) scale(1.15)} }
      @keyframes sk-drift-b { 0%,100%{transform:translate3d(5%,6%,0) scale(1.1)} 50%{transform:translate3d(-5%,-6%,0) scale(.95)} }
      @keyframes sk-rise { 0%{transform:translate3d(0,0,0);opacity:0} 12%{opacity:.5} 88%{opacity:.5} 100%{transform:translate3d(14px,-160px,0);opacity:0} }
      @keyframes sk-sheen { 0%,100%{opacity:.05} 50%{opacity:.14} }
      @media (prefers-reduced-motion: reduce) {
        [data-atmos] * { animation: none !important; }
      }
    `}</style>
  )
}

/** Deterministic — a fresh scatter on every render would flicker across re-renders. */
const MOTES = Array.from({ length: 16 }, (_, i) => ({
  left: `${(i * 37) % 94 + 3}%`,
  top: `${(i * 61) % 82 + 9}%`,
  size: i % 5 === 0 ? 2.5 : 1.5,
  dur: 34 + ((i * 7) % 26),
  delay: -(i * 4.3) % 30,
}))

export function Atmosphere() {
  return (
    <div data-atmos aria-hidden className="pointer-events-none absolute inset-0 overflow-hidden">
      {/* two counter-drifting key lights */}
      <div
        className="absolute -left-[20%] -top-[25%] h-[85vmax] w-[85vmax] rounded-full"
        style={{
          background: 'radial-gradient(circle,rgba(255,255,255,.10),transparent 62%)',
          filter: 'blur(60px)',
          animation: 'sk-drift-a 58s cubic-bezier(.4,0,.6,1) infinite',
        }}
      />
      <div
        className="absolute -bottom-[30%] -right-[15%] h-[70vmax] w-[70vmax] rounded-full"
        style={{
          background: 'radial-gradient(circle,rgba(255,255,255,.06),transparent 60%)',
          filter: 'blur(70px)',
          animation: 'sk-drift-b 71s cubic-bezier(.4,0,.6,1) infinite',
        }}
      />
      {/* glass sheen across the panel */}
      <div
        className="absolute inset-0"
        style={{
          background: 'linear-gradient(115deg,transparent 30%,rgba(255,255,255,.5) 50%,transparent 70%)',
          mixBlendMode: 'overlay',
          animation: 'sk-sheen 44s ease-in-out infinite',
        }}
      />
      {/* motes */}
      {MOTES.map((m, i) => (
        <span
          key={i}
          className="absolute rounded-full bg-white"
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
      {/* film grain — data URI so there is no request, and it tiles */}
      <div
        className="absolute inset-0 opacity-[0.16] mix-blend-overlay"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='140' height='140'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.85' numOctaves='3'/%3E%3C/filter%3E%3Crect width='140' height='140' filter='url(%23n)' opacity='.5'/%3E%3C/svg%3E\")",
        }}
      />
      {/* vignette */}
      <div
        className="absolute inset-0"
        style={{ background: 'radial-gradient(120% 90% at 50% 45%,transparent 40%,rgba(0,0,0,.72) 100%)' }}
      />
    </div>
  )
}

/** Counts to `to` once, on view. Purely decorative. */
function useCountUp(to: number, decimals: number, durationMs = 2200) {
  const [v, setV] = useState(REDUCED() ? to : 0)
  useEffect(() => {
    // No "have I started" ref here on purpose: a ref survives StrictMode's
    // mount/unmount/mount, so the first pass would claim it, the cleanup would
    // cancel the animation, and the second — real — pass would early-return and
    // schedule nothing, leaving the counters reading 0 forever. The cleanup below
    // is the only guard needed.
    if (REDUCED()) return
    let raf = 0
    let done = false
    const t0 = performance.now()
    const step = (now: number) => {
      const p = Math.min(1, (now - t0) / durationMs)
      const eased = 1 - Math.pow(1 - p, 3)
      setV(to * eased)
      if (p < 1) raf = requestAnimationFrame(step)
      else done = true
    }
    raf = requestAnimationFrame(step)
    // rAF is suspended whenever the document is not rendered (background tab,
    // restored session). Without this the counters would sit at 0 permanently
    // once the tab came forward, showing "0+ магазинов рядом".
    const guard = setTimeout(() => {
      if (!done) {
        cancelAnimationFrame(raf)
        setV(to)
      }
    }, durationMs + 1200)
    return () => {
      cancelAnimationFrame(raf)
      clearTimeout(guard)
    }
  }, [to, durationMs])
  return decimals > 0 ? v.toFixed(decimals) : Math.round(v).toLocaleString('ru-RU').replace(/,/g, ' ')
}

function Stat({ to, decimals, suffix, label }: { to: number; decimals: number; suffix: string; label: string }) {
  const value = useCountUp(to, decimals)
  return (
    <div>
      <div className="text-[clamp(20px,2vw,26px)] font-extrabold tracking-[-0.03em] text-white tabular-nums">
        {value}
        {suffix}
      </div>
      {/* white/60 = 7.37:1 on black. Anything below 0.50 fails AA here, and the
          previous 0.40 (3.66:1) was one of the unreadable elements reported. */}
      <div className="mt-1 text-[12px] leading-snug text-white/60">{label}</div>
    </div>
  )
}

export function AuthStats() {
  return (
    <div className="grid grid-cols-3 gap-6 border-t border-white/10 pt-8">
      <Stat to={2500} decimals={0} suffix="+" label="магазинов рядом" />
      <Stat to={18.4} decimals={1} suffix="M" label="сомони сэкономлено" />
      <Stat to={1.2} decimals={1} suffix="M+" label="товаров сравнено" />
    </div>
  )
}
