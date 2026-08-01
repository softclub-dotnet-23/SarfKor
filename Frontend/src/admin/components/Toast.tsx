import { AnimatePresence, motion } from 'framer-motion'
import type { ReactNode } from 'react'

// Promoted from PosPage's local toast (previously a hardcoded #0f172a/#34d399
// pill) into a token-driven, reusable one. Deliberately stays a dark glass
// pill regardless of the active admin theme — the same high-contrast,
// always-legible treatment Stripe/Linear-style products use for toasts, and
// it borrows the landing's dark-glass material directly rather than
// reinventing a light-mode variant.
const ACCENT = {
  admin: { success: 'var(--admin-success)', danger: 'var(--admin-danger)', neutral: 'var(--admin-accent)' },
  mod: { success: 'var(--mod-ok)', danger: 'var(--mod-danger)', neutral: 'var(--mod-accent)' },
} as const

interface ToastProps {
  open: boolean
  children: ReactNode
  variant?: keyof (typeof ACCENT)['admin']
  scheme?: keyof typeof ACCENT
}

export function Toast({ open, children, variant = 'success', scheme = 'admin' }: ToastProps) {
  const accent = ACCENT[scheme][variant]
  return (
    <AnimatePresence>
      {open && (
        <motion.div
          initial={{ opacity: 0, y: 16, scale: 0.96 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={{ opacity: 0, y: 10, scale: 0.97 }}
          transition={{ type: 'spring', stiffness: 340, damping: 30 }}
          className="pointer-events-none fixed inset-x-0 bottom-6 z-[200] flex justify-center px-4"
        >
          <div
            className="pointer-events-auto flex items-center gap-2.5 rounded-full px-5 py-3 text-[13px] font-semibold text-white shadow-2xl backdrop-blur-xl"
            style={{
              background: 'color-mix(in srgb, #0b0d12 88%, transparent)',
              boxShadow: `0 0 0 1px color-mix(in srgb, ${accent} 45%, transparent), 0 20px 48px -12px rgba(0,0,0,0.55)`,
            }}
          >
            <span aria-hidden className="h-2 w-2 shrink-0 rounded-full" style={{ background: accent }} />
            {children}
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
