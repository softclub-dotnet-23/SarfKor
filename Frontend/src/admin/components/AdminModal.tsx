import { AnimatePresence, motion } from 'framer-motion'
import { useEffect, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { lockBodyScroll, unlockBodyScroll } from '../../lib/scrollLock'
import { XIcon } from './icons'

// Single 'admin' scheme, same as Card/Select/Input/Badge — the StorePartner cabinet and
// the platform-Admin console read the same --admin-* token set (see index.css).
const SCHEMES = {
  admin: {
    shell: 'admin-shell',
    // --admin-sidebar (opaque), not --admin-card (translucent glass) -- see SidePanel.tsx for why.
    panel: 'bg-[color:var(--admin-sidebar)] ring-1 ring-[color:var(--admin-border)]',
    title: 'text-[color:var(--admin-text)]',
    close: 'text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]',
  },
} as const

interface AdminModalProps {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  scheme?: keyof typeof SCHEMES
}

export function AdminModal({ open, onClose, title, children, scheme = 'admin' }: AdminModalProps) {
  const t = SCHEMES[scheme]

  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    document.addEventListener('keydown', onKey)
    lockBodyScroll()
    return () => {
      document.removeEventListener('keydown', onKey)
      unlockBodyScroll()
    }
  }, [open, onClose])

  return createPortal(
    <AnimatePresence>
      {open && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className={`${t.shell} fixed inset-0 z-modal flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm`}
          onClick={onClose}
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.94, y: 14 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.96, y: 8 }}
            transition={{ type: 'spring', stiffness: 340, damping: 30 }}
            onClick={(e) => e.stopPropagation()}
            className={`relative w-full max-w-md rounded-[22px] p-6 shadow-2xl ${t.panel}`}
          >
            <div className="mb-5 flex items-center justify-between">
              <h3 className={`text-[17px] font-bold ${t.title}`}>{title}</h3>
              <button onClick={onClose} aria-label="Закрыть" className={`grid h-8 w-8 place-items-center rounded-full ${t.close}`}>
                <XIcon width={16} height={16} />
              </button>
            </div>
            {children}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>,
    document.body,
  )
}
