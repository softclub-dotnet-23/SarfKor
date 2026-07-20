import { useEffect, useId, useRef, useState } from 'react'

interface RingChartProps {
  value: number
  label: string
  colorFrom: string
  colorTo: string
  size?: number
}

export function RingChart({ value, label, colorFrom, colorTo, size = 160 }: RingChartProps) {
  const gradId = useId()
  const [pct, setPct] = useState(0)
  const wrapRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const el = wrapRef.current
    if (!el) return
    const io = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting) return
        const start = performance.now()
        const duration = 1200
        const tick = (now: number) => {
          const p = Math.min((now - start) / duration, 1)
          setPct(value * (1 - Math.pow(1 - p, 3)))
          if (p < 1) requestAnimationFrame(tick)
        }
        requestAnimationFrame(tick)
        io.disconnect()
      },
      { threshold: 0.3 },
    )
    io.observe(el)
    return () => io.disconnect()
  }, [value])

  const r = 62
  const stroke = 14
  const circ = 2 * Math.PI * r
  const offset = circ * (1 - pct / 100)

  return (
    <div ref={wrapRef} className="relative" style={{ width: size, height: size }}>
      <svg width={size} height={size} viewBox="0 0 160 160">
        <defs>
          <linearGradient id={gradId} x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stopColor={colorFrom} />
            <stop offset="100%" stopColor={colorTo} />
          </linearGradient>
        </defs>
        <circle cx={80} cy={80} r={r} fill="none" stroke="var(--admin-border)" strokeWidth={stroke} />
        <circle
          cx={80}
          cy={80}
          r={r}
          fill="none"
          stroke={`url(#${gradId})`}
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={circ}
          strokeDashoffset={offset}
          transform="rotate(-90 80 80)"
          style={{ transition: 'stroke-dashoffset 0.3s ease' }}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-[28px] font-extrabold text-[color:var(--admin-text)]">{Math.round(pct)}%</span>
        <span className="mt-0.5 text-[11px] text-[color:var(--admin-text-tertiary)]">{label}</span>
      </div>
    </div>
  )
}
