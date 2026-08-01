import type { ReactNode } from 'react'
import { motion, useReducedMotion } from 'framer-motion'

// Admin-scoped equivalent of src/app/ui.tsx's Reveal — same easing curve and
// spring shape, kept as a separate copy (not cross-imported) so the two
// surfaces stay decoupled the way they already are, since this component
// carries no color dependency of its own.
const EASE = [0.16, 1, 0.3, 1] as const

export function Reveal({ i = 0, children, className }: { i?: number; children: ReactNode; className?: string }) {
  const reduce = useReducedMotion()
  if (reduce) return <div className={className}>{children}</div>
  return (
    <motion.div
      className={className}
      initial={{ opacity: 0, y: 14 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{
        y: { type: 'spring', stiffness: 90, damping: 22, delay: 0.04 + i * 0.05 },
        opacity: { duration: 0.5, ease: EASE, delay: 0.04 + i * 0.05 },
      }}
    >
      {children}
    </motion.div>
  )
}
