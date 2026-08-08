import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { createPortal } from 'react-dom'
import { lockBodyScroll, unlockBodyScroll } from '../../lib/scrollLock'
import { useT } from '../../i18n/translations'
import { XIcon } from './icons'

// Same 'admin' scheme as every other shared component here — the StorePartner cabinet and
// the platform-Admin console read the same --admin-* token set.
const SCHEMES = {
  admin: {
    shell: 'admin-shell',
    panel: 'bg-[color:var(--admin-card)] ring-1 ring-[color:var(--admin-border)]',
    title: 'text-[color:var(--admin-text)]',
    close: 'text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]',
    border: 'border-[color:var(--admin-border)]',
    cancelBtn: 'border border-[color:var(--admin-border)] text-[color:var(--admin-text)] hover:bg-[color:var(--admin-hover)]',
    submitBtn: 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]',
    error: 'text-[color:var(--admin-danger)]',
  },
} as const

const FOCUSABLE = 'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'

const SIZES = { md: 'max-w-md', lg: 'max-w-2xl' } as const

export interface FormModalProps {
  open: boolean
  onClose: () => void
  title: string
  /** True once the user has typed/changed anything -- gates the "discard changes?" confirm on close. */
  isDirty?: boolean
  /** 'md' (default) for a handful of fields; 'lg' for forms with a wide row-per-line-item layout. */
  size?: keyof typeof SIZES
  /** Throw (with a `.message`) to show a server error without closing the modal or losing input. */
  onSubmit: () => Promise<void>
  submitLabel: string
  submitBusyLabel?: string
  cancelLabel?: string
  scheme?: keyof typeof SCHEMES
  children: ReactNode
}

// One reusable modal for every "create X" / "edit X" form in the project (ADMIN_PROMPT follow-up:
// "убрать постоянно открытые формы добавления, схема кнопка -> модалка, один переиспользуемый
// компонент"). Owns: focus-on-open, focus trap, return-focus-to-trigger, Esc/backdrop-click to
// close (guarded by isDirty), submit orchestration (busy state, server-error display, no
// double-submit). Field markup/validation stays with the caller -- this only owns the shell.
export function FormModal({
  open,
  onClose,
  title,
  isDirty = false,
  onSubmit,
  submitLabel,
  submitBusyLabel,
  cancelLabel,
  scheme = 'admin',
  size = 'md',
  children,
}: FormModalProps) {
  const c = SCHEMES[scheme]
  const t = useT()
  const [busy, setBusy] = useState(false)
  const [serverError, setServerError] = useState('')
  const panelRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLElement | null>(null)

  function requestClose() {
    if (busy) return
    if (isDirty && !window.confirm('Закрыть без сохранения? Введённые данные будут потеряны.')) return
    onClose()
  }

  // Focus the first field on open, remember what had focus so it comes back on close (WCAG 2.4.3 /
  // the "focus returns to the trigger" requirement) -- a plain useEffect(open) would fire after the
  // browser's own focus-follows-click has already moved focus, so this has to run on the open edge.
  useEffect(() => {
    if (!open) return
    triggerRef.current = document.activeElement as HTMLElement | null
    setServerError('')
    const raf = requestAnimationFrame(() => {
      const first = panelRef.current?.querySelector<HTMLElement>(FOCUSABLE)
      first?.focus()
    })
    return () => {
      cancelAnimationFrame(raf)
      triggerRef.current?.focus?.()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  useEffect(() => {
    if (!open) return
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        e.preventDefault()
        requestClose()
        return
      }
      if (e.key !== 'Tab' || !panelRef.current) return
      const focusable = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE)).filter((el) => el.offsetParent !== null)
      if (focusable.length === 0) return
      const first = focusable[0]
      const last = focusable[focusable.length - 1]
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault()
        last.focus()
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault()
        first.focus()
      }
    }
    document.addEventListener('keydown', onKeyDown)
    lockBodyScroll()
    return () => {
      document.removeEventListener('keydown', onKeyDown)
      unlockBodyScroll()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, busy, isDirty])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (busy) return
    setBusy(true)
    setServerError('')
    try {
      await onSubmit()
      onClose()
    } catch (err) {
      setServerError(err instanceof Error ? err.message : 'Не удалось выполнить действие')
    } finally {
      setBusy(false)
    }
  }

  return createPortal(
    <AnimatePresence>
      {open && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className={`${c.shell} fixed inset-0 z-modal flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm`}
          onClick={requestClose}
        >
          <motion.div
            ref={panelRef}
            role="dialog"
            aria-modal="true"
            aria-label={title}
            initial={{ opacity: 0, scale: 0.94, y: 14 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.96, y: 8 }}
            transition={{ type: 'spring', stiffness: 340, damping: 30 }}
            onClick={(e) => e.stopPropagation()}
            className={`relative flex max-h-[90vh] w-full ${SIZES[size]} flex-col overflow-hidden rounded-[22px] shadow-2xl ${c.panel}`}
          >
            <div className={`flex shrink-0 items-center justify-between border-b px-6 py-5 ${c.border}`}>
              <h3 className={`text-[17px] font-bold ${c.title}`}>{title}</h3>
              <button type="button" onClick={requestClose} aria-label={t('common.close')} className={`grid h-8 w-8 place-items-center rounded-full ${c.close}`}>
                <XIcon width={16} height={16} />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="flex min-h-0 flex-1 flex-col">
              <div className="min-h-0 flex-1 overflow-y-auto px-6 py-5">
                {children}
                {serverError && <p className={`mt-3 text-[12.5px] font-medium ${c.error}`}>{serverError}</p>}
              </div>
              <div className={`flex shrink-0 justify-end gap-2 border-t px-6 py-4 ${c.border}`}>
                <button type="button" onClick={requestClose} disabled={busy} className={`rounded-xl px-4 py-2.5 text-[13px] font-semibold transition-colors disabled:opacity-50 ${c.cancelBtn}`}>
                  {cancelLabel ?? t('common.cancel')}
                </button>
                <button
                  type="submit"
                  disabled={busy}
                  className={`rounded-xl px-4 py-2.5 text-[13px] font-bold transition-transform hover:brightness-110 active:scale-95 disabled:opacity-50 disabled:active:scale-100 ${c.submitBtn}`}
                >
                  {busy ? (submitBusyLabel ?? t('common.saving')) : submitLabel}
                </button>
              </div>
            </form>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>,
    document.body,
  )
}

/** Consistent label + field + error-text stack, for callers that don't already have their own. */
export function FormField({
  label,
  error,
  required,
  children,
  scheme = 'admin',
}: {
  label: string
  error?: string
  required?: boolean
  children: ReactNode
  scheme?: keyof typeof SCHEMES
}) {
  const faintClass = 'text-[color:var(--admin-text-tertiary)]'
  const errorClass = SCHEMES[scheme].error
  return (
    <div className="mb-4 last:mb-0">
      <label className={`mb-1.5 block text-[11.5px] font-semibold uppercase tracking-wide ${faintClass}`}>
        {label}
        {required && <span className={errorClass}> *</span>}
      </label>
      {children}
      {error && <p className={`mt-1.5 text-[12px] font-medium ${errorClass}`}>{error}</p>}
    </div>
  )
}
