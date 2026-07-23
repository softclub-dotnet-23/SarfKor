import { useEffect, useRef, useState } from 'react'

/**
 * Animates a displayed number toward `target` whenever it changes — used for
 * KPI tiles that get bumped by a live-update interval, not just an initial
 * mount animation.
 */
export function useLiveCounter(target: number, duration = 900) {
  const [value, setValue] = useState(0)
  const fromRef = useRef(0)
  const rafRef = useRef(0)

  useEffect(() => {
    const from = fromRef.current
    const start = performance.now()
    cancelAnimationFrame(rafRef.current)

    const tick = (now: number) => {
      const p = Math.min((now - start) / duration, 1)
      const eased = 1 - Math.pow(1 - p, 3)
      const next = from + (target - from) * eased
      setValue(next)
      if (p < 1) {
        rafRef.current = requestAnimationFrame(tick)
      } else {
        fromRef.current = target
      }
    }
    rafRef.current = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(rafRef.current)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [target])

  return value
}
