import { useEffect, useId, useRef, useState } from 'react'

interface LineChartProps {
  data: { day: string; value: number }[]
}

export function LineChart({ data }: LineChartProps) {
  const gradientId = useId()
  const lineId = useId()
  const [drawn, setDrawn] = useState(0)
  const wrapRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const el = wrapRef.current
    if (!el) return
    const io = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting) return
        const start = performance.now()
        const duration = 1400
        const tick = (now: number) => {
          const p = Math.min((now - start) / duration, 1)
          setDrawn(1 - Math.pow(1 - p, 3))
          if (p < 1) requestAnimationFrame(tick)
        }
        requestAnimationFrame(tick)
        io.disconnect()
      },
      { threshold: 0.3 },
    )
    io.observe(el)
    return () => io.disconnect()
  }, [])

  const values = data.map((d) => d.value)
  const maxV = Math.max(...values) * 1.15
  const minV = Math.min(...values) * 0.7
  // A brand-new store has zero revenue every day this week, so maxV === minV
  // (both 0) — without this fallback the normalized-position division below
  // is 0/0 = NaN for every point, which React then feeds straight into SVG
  // path/circle coordinates and the whole chart silently fails to render.
  const range = maxV - minV || 1
  const w = 660
  const h = 220
  const px = 42
  const py = 12
  const pts = values.map((v, i) => ({
    x: px + (i / (values.length - 1)) * (w - px * 2),
    y: py + (1 - (v - minV) / range) * (h - py * 2),
  }))

  let pathD = `M ${pts[0].x} ${pts[0].y}`
  for (let i = 0; i < pts.length - 1; i++) {
    const cp = (pts[i + 1].x - pts[i].x) / 2.5
    pathD += ` C ${pts[i].x + cp} ${pts[i].y}, ${pts[i + 1].x - cp} ${pts[i + 1].y}, ${pts[i + 1].x} ${pts[i + 1].y}`
  }
  const areaD = `${pathD} L ${pts[pts.length - 1].x} ${h} L ${pts[0].x} ${h} Z`
  const totalLen = 1400
  const dashOffset = totalLen * (1 - drawn)
  const peakIdx = values.indexOf(Math.max(...values))

  return (
    <div ref={wrapRef} className="relative h-[220px] w-full">
      <svg width="100%" height="100%" viewBox={`0 0 ${w} ${h + 28}`} preserveAspectRatio="none" style={{ overflow: 'visible' }}>
        {[0, 0.25, 0.5, 0.75, 1].map((f) => {
          const y = py + (1 - f) * (h - py * 2)
          const val = Math.round(minV + f * (maxV - minV))
          return (
            <g key={f}>
              <line x1={px} y1={y} x2={w - px} y2={y} stroke="var(--admin-border)" strokeWidth={1} />
              <text x={px - 8} y={y + 4} textAnchor="end" fill="var(--admin-text-tertiary)" fontSize={10}>
                {(val / 1000).toFixed(0)}к
              </text>
            </g>
          )
        })}
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--admin-accent)" stopOpacity={0.22} />
            <stop offset="100%" stopColor="var(--admin-accent)" stopOpacity={0} />
          </linearGradient>
          <linearGradient id={lineId} x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor="var(--admin-accent)" />
            <stop offset="100%" stopColor="var(--admin-text)" />
          </linearGradient>
        </defs>
        <path d={areaD} fill={`url(#${gradientId})`} opacity={drawn} />
        <path
          d={pathD}
          fill="none"
          stroke={`url(#${lineId})`}
          strokeWidth={2.5}
          strokeLinecap="round"
          strokeDasharray={totalLen}
          strokeDashoffset={dashOffset}
        />
        {drawn > 0.8 &&
          pts.map((p, i) => (
            <circle
              key={i}
              cx={p.x}
              cy={p.y}
              r={i === peakIdx ? 5 : 3.5}
              fill="var(--admin-accent)"
              stroke="var(--admin-card)"
              strokeWidth={2}
              opacity={Math.min(1, (drawn - 0.8) * 5)}
            />
          ))}
        {drawn > 0.9 && (
          <g opacity={Math.min(1, (drawn - 0.9) * 10)}>
            <rect x={pts[peakIdx].x - 34} y={pts[peakIdx].y - 34} width={68} height={22} rx={7} fill="var(--admin-accent)" />
            <text x={pts[peakIdx].x} y={pts[peakIdx].y - 19} textAnchor="middle" fill="#fff" fontSize={11} fontWeight={700}>
              {values[peakIdx].toLocaleString('ru-RU')}
            </text>
          </g>
        )}
        {data.map((d, i) => (
          <text key={i} x={pts[i].x} y={h + 22} textAnchor="middle" fill="var(--admin-text-tertiary)" fontSize={11}>
            {d.day}
          </text>
        ))}
      </svg>
    </div>
  )
}
