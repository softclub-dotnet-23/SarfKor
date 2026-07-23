import { AnimatePresence, motion } from 'framer-motion'
import { useEffect, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { XIcon } from './icons'

interface AdminModalProps {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
}

export function AdminModal({ open, onClose, title, children }: AdminModalProps) {
  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    document.addEventListener('keydown', onKey)
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = ''
    }
  }, [open, onClose])

  return createPortal(
    <AnimatePresence>
      {open && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="admin-shell fixed inset-0 z-[100] flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm"
          onClick={onClose}
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.94, y: 14 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.96, y: 8 }}
            transition={{ type: 'spring', stiffness: 340, damping: 30 }}
            onClick={(e) => e.stopPropagation()}
            className="relative w-full max-w-md rounded-[22px] bg-[color:var(--admin-card)] p-6 shadow-2xl ring-1 ring-[color:var(--admin-border)]"
          >
            <div className="mb-5 flex items-center justify-between">
              <h3 className="text-[17px] font-bold text-[color:var(--admin-text)]">{title}</h3>
              <button
                onClick={onClose}
                aria-label="Закрыть"
                className="grid h-8 w-8 place-items-center rounded-full text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]"
              >
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
