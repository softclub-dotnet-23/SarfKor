import { AnimatePresence, motion } from 'framer-motion'
import { useEffect, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { XIcon } from './icons'

// A right-side slide-over for record detail views (store card, user card) -- roomier than
// AdminModal's centered dialog, which suits a multi-tab record better than a small confirm form.
export function SidePanel({ open, onClose, title, subtitle, children }: { open: boolean; onClose: () => void; title: ReactNode; subtitle?: string; children: ReactNode }) {
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
          className="mod-shell fixed inset-0 z-100 flex justify-end bg-black/50 backdrop-blur-sm"
          onClick={onClose}
        >
          <motion.div
            initial={{ x: '100%' }}
            animate={{ x: 0 }}
            exit={{ x: '100%' }}
            transition={{ type: 'spring', stiffness: 320, damping: 34 }}
            onClick={(e) => e.stopPropagation()}
            className="flex h-full w-full max-w-[640px] flex-col bg-[color:var(--mod-panel)] shadow-2xl"
          >
            <div className="flex shrink-0 items-center justify-between gap-3 border-b border-[color:var(--mod-border)] px-6 py-4">
              <div className="min-w-0">
                <h2 className="truncate text-[17px] font-extrabold tracking-tight text-[color:var(--mod-text)]">{title}</h2>
                {subtitle && <p className="truncate text-[12px] text-[color:var(--mod-muted)]">{subtitle}</p>}
              </div>
              <button onClick={onClose} aria-label="Закрыть" className="grid h-8 w-8 shrink-0 place-items-center rounded-full text-[color:var(--mod-faint)] hover:bg-[color:var(--mod-panel2)]">
                <XIcon width={16} height={16} />
              </button>
            </div>
            <div className="flex-1 overflow-y-auto px-6 py-5">{children}</div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>,
    document.body,
  )
}

export function PanelTabs<T extends string>({ tabs, active, onChange }: { tabs: { id: T; label: string }[]; active: T; onChange: (id: T) => void }) {
  return (
    <div className="mb-5 flex flex-wrap gap-1 rounded-xl bg-[color:var(--mod-panel2)] p-1">
      {tabs.map((t) => (
        <button
          key={t.id}
          onClick={() => onChange(t.id)}
          className={`rounded-lg px-3 py-2 text-[12.5px] font-bold transition-colors ${
            active === t.id ? 'bg-[color:var(--mod-accent)] text-white' : 'text-[color:var(--mod-muted)] hover:text-[color:var(--mod-text)]'
          }`}
        >
          {t.label}
        </button>
      ))}
    </div>
  )
}

export function FieldRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-3 border-b border-[color:var(--mod-border)] py-2.5 last:border-0">
      <span className="text-[12.5px] font-medium text-[color:var(--mod-muted)]">{label}</span>
      <span className="truncate text-[12.5px] font-semibold text-[color:var(--mod-text)]">{value}</span>
    </div>
  )
}
