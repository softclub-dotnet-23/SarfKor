import { useState } from 'react'
import { AdminModal } from './AdminModal'
import { Textarea } from './Input'

// Every admin action that disables/blocks/suspends/blocks something requires a mandatory
// written reason (ADMIN_PROMPT.md §2) -- one shared confirm dialog instead of six hand-rolled
// forms, so "no reason, no action" is enforced in exactly one place.
export function ReasonModal({
  open,
  onClose,
  title,
  description,
  confirmLabel,
  danger = false,
  onConfirm,
}: {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  confirmLabel: string
  danger?: boolean
  onConfirm: (reason: string) => Promise<void>
}) {
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  function handleClose() {
    if (busy) return
    setReason('')
    setError('')
    onClose()
  }

  async function handleConfirm() {
    if (!reason.trim() || busy) return
    setBusy(true)
    setError('')
    try {
      await onConfirm(reason.trim())
      setReason('')
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Не удалось выполнить действие')
    } finally {
      setBusy(false)
    }
  }

  return (
    <AdminModal open={open} onClose={handleClose} title={title} scheme="mod">
      {description && <p className="mb-4 text-[13px] leading-relaxed text-[color:var(--mod-muted)]">{description}</p>}
      <label className="mb-1.5 block text-[11.5px] font-semibold uppercase tracking-wide text-[color:var(--mod-faint)]">
        Причина <span className="text-[color:var(--mod-danger)]">*</span>
      </label>
      <Textarea
        scheme="mod"
        rows={3}
        autoFocus
        value={reason}
        onChange={(e) => setReason(e.target.value)}
        placeholder="Обязательно для аудит-лога…"
        className="mb-1"
      />
      {error && <p className="mb-2 text-[12px] font-medium text-[color:var(--mod-danger)]">{error}</p>}
      <div className="mt-4 flex justify-end gap-2">
        <button
          onClick={handleClose}
          disabled={busy}
          className="rounded-xl border border-[color:var(--mod-border)] px-4 py-2.5 text-[13px] font-semibold text-[color:var(--mod-text)] transition-colors hover:bg-[color:var(--mod-panel2)] disabled:opacity-50"
        >
          Отмена
        </button>
        <button
          onClick={handleConfirm}
          disabled={busy || !reason.trim()}
          className={`rounded-xl px-4 py-2.5 text-[13px] font-bold text-white transition-transform hover:brightness-110 active:scale-95 disabled:opacity-50 ${
            danger ? 'bg-[color:var(--mod-danger)]' : 'bg-[color:var(--mod-accent)]'
          }`}
        >
          {busy ? 'Секунду…' : confirmLabel}
        </button>
      </div>
    </AdminModal>
  )
}
