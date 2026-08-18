import { useRef, useState, type FormEvent } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { useAuth } from '../../auth/AuthContext'
import { assistantApi, type AssistantChatMessage, type ProposedAction } from '../../lib/api'
import { describeError } from '../../lib/errorKind'
import { useT } from '../../i18n/translations'
import { ChatIcon, SendIcon, XIcon, CheckIcon, AlertIcon } from './icons'

interface DisplayMessage extends AssistantChatMessage {
  id: number
}

/**
 * Floating assistant chat, available on every admin-cabinet page. Backend enforces every actual
 * restriction (role-gated tools, store ownership) -- this component only decides whether to render
 * the entry point at all (Admin/StorePartner/Cashier), never what the assistant is allowed to see
 * or do.
 */
export function AssistantPanel() {
  const { user, storeId, currentStoreRole } = useAuth()
  const t = useT()
  const [open, setOpen] = useState(false)
  const [messages, setMessages] = useState<DisplayMessage[]>([])
  const [input, setInput] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [proposedAction, setProposedAction] = useState<ProposedAction | null>(null)
  const [confirmBusy, setConfirmBusy] = useState(false)
  const [confirmStatus, setConfirmStatus] = useState('')
  const nextId = useRef(0)
  const scrollRef = useRef<HTMLDivElement>(null)

  const isAdmin = user?.roles.includes('Admin') ?? false
  const isStorePartner = user?.roles.includes('StorePartner') ?? false
  if (!isAdmin && !isStorePartner) return null

  function scrollToBottom() {
    requestAnimationFrame(() => scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' }))
  }

  async function handleSend(e: FormEvent) {
    e.preventDefault()
    const text = input.trim()
    if (!text || busy) return

    const history: AssistantChatMessage[] = messages.map((m) => ({ role: m.role, content: m.content }))
    setMessages((m) => [...m, { id: nextId.current++, role: 'user', content: text }])
    setInput('')
    setError('')
    setConfirmStatus('')
    setBusy(true)
    scrollToBottom()

    try {
      const result = await assistantApi.chat(storeId, history, text)
      if (result.outcome === 'Answered') {
        setMessages((m) => [...m, { id: nextId.current++, role: 'assistant', content: result.replyText ?? '' }])
        setProposedAction(result.proposedAction ?? null)
      } else if (result.outcome === 'StoreNotFound') {
        setError('Магазин не найден.')
      } else {
        setError('Нет доступа к ассистенту.')
      }
    } catch (err) {
      setError(describeError(err, t, { isOwner: currentStoreRole !== 'Cashier' }))
    } finally {
      setBusy(false)
      scrollToBottom()
    }
  }

  async function handleConfirm() {
    if (!proposedAction || confirmBusy) return
    setConfirmBusy(true)
    setConfirmStatus('')
    try {
      const result = await assistantApi.confirmAction(proposedAction.pendingActionId)
      if (result.outcome === 'Confirmed' || result.outcome === 'AlreadyConfirmed') {
        setConfirmStatus(result.summary ?? 'Готово.')
        setProposedAction(null)
      } else if (result.outcome === 'Expired') {
        setConfirmStatus('Предложение устарело — попросите ассистента повторить.')
        setProposedAction(null)
      } else if (result.outcome === 'FeatureDisabled') {
        setConfirmStatus('Действия через ассистента сейчас отключены.')
        setProposedAction(null)
      } else {
        setConfirmStatus(result.summary ?? 'Не удалось выполнить действие.')
      }
    } catch (err) {
      setConfirmStatus(describeError(err, t, { isOwner: currentStoreRole !== 'Cashier' }))
    } finally {
      setConfirmBusy(false)
    }
  }

  return (
    // .admin-shell scopes the --admin-* custom properties this component relies on (see
    // index.css) -- `contents` keeps the wrapper itself out of layout. Redundant when nested
    // inside a page that already has .admin-shell higher up, but this component is also used
    // standalone, so it re-declares its own scope rather than assuming one exists above it.
    <div className="admin-shell contents">
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label={open ? 'Закрыть ассистента' : 'Открыть ассистента'}
        className="fixed bottom-6 right-6 z-header-popover grid h-14 w-14 place-items-center rounded-full bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)] shadow-[0_10px_30px_-6px_rgba(0,0,0,0.35)] transition-transform hover:scale-105 active:scale-95"
      >
        {open ? <XIcon width={22} height={22} /> : <ChatIcon width={22} height={22} />}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: 16, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 12, scale: 0.97 }}
            transition={{ type: 'spring', stiffness: 340, damping: 30 }}
            // --admin-sidebar, not --admin-card: this panel floats directly over arbitrary page
            // content (a data table, a form), not over the page's own uniform background, so it
            // needs a genuinely opaque surface -- --admin-card is a translucent "glass" tone (alpha
            // 0.045 in dark mode) meant for a card sitting IN the page flow, and used here it let
            // the table underneath show straight through the chat window.
            className="fixed bottom-24 right-6 z-header-popover flex h-[520px] w-[380px] max-w-[calc(100vw-2rem)] flex-col overflow-hidden rounded-[20px] bg-[color:var(--admin-sidebar)] shadow-2xl ring-1 ring-[color:var(--admin-border)]"
          >
            <div className="flex items-center justify-between border-b border-[color:var(--admin-border)] px-4 py-3">
              <div>
                <div className="text-[14px] font-bold text-[color:var(--admin-text)]">Ассистент Sarfkor</div>
                <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">
                  {currentStoreRole === 'Cashier' ? 'Справка и остатки' : 'Справка, данные и действия'}
                </div>
              </div>
              <button
                onClick={() => setOpen(false)}
                aria-label="Закрыть"
                className="grid h-8 w-8 place-items-center rounded-full text-[color:var(--admin-text-tertiary)] hover:bg-[color:var(--admin-hover)]"
              >
                <XIcon width={16} height={16} />
              </button>
            </div>

            <div ref={scrollRef} className="flex-1 overflow-y-auto px-4 py-3">
              {messages.length === 0 && (
                <div className="flex h-full flex-col items-center justify-center gap-2 text-center text-[color:var(--admin-text-tertiary)]">
                  <ChatIcon width={28} height={28} />
                  <p className="max-w-[240px] text-[12.5px]">
                    Спросите про работу с кассой и складом — например, «что скоро кончится» или «как оприходовать поставку».
                  </p>
                </div>
              )}
              <div className="flex flex-col gap-2.5">
                {messages.map((m) => (
                  <div key={m.id} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                    <div
                      className={`max-w-[85%] whitespace-pre-wrap rounded-2xl px-3.5 py-2.5 text-[13px] leading-relaxed ${
                        m.role === 'user'
                          ? 'bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)]'
                          : 'bg-[color:var(--admin-hover)] text-[color:var(--admin-text)]'
                      }`}
                    >
                      {m.content}
                    </div>
                  </div>
                ))}
                {busy && (
                  <div className="flex justify-start">
                    <div className="rounded-2xl bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text-tertiary)]">
                      Печатает…
                    </div>
                  </div>
                )}
              </div>

              {proposedAction && (
                <div className="mt-3 rounded-2xl bg-[color:var(--admin-accent-soft)] p-3.5">
                  <div className="mb-2 text-[12.5px] font-semibold text-[color:var(--admin-text)]">{proposedAction.summary}</div>
                  <div className="mb-2.5 text-[11px] text-[color:var(--admin-text-tertiary)]">
                    Ожидает подтверждения — ничего ещё не изменено.
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={handleConfirm}
                      disabled={confirmBusy}
                      className="flex flex-1 items-center justify-center gap-1.5 rounded-xl bg-[color:var(--admin-accent)] py-2 text-[12.5px] font-bold text-[color:var(--admin-accent-fg)] disabled:opacity-50"
                    >
                      <CheckIcon width={13} height={13} />
                      {confirmBusy ? 'Подтверждаем…' : 'Подтвердить'}
                    </button>
                    <button
                      onClick={() => setProposedAction(null)}
                      disabled={confirmBusy}
                      className="rounded-xl bg-[color:var(--admin-card)] px-3.5 py-2 text-[12.5px] font-semibold text-[color:var(--admin-text-secondary)]"
                    >
                      Не сейчас
                    </button>
                  </div>
                </div>
              )}

              {confirmStatus && (
                <div className="mt-2.5 text-[12px] font-medium text-[color:var(--admin-text-secondary)]">{confirmStatus}</div>
              )}

              {error && (
                <div className="mt-2.5 flex items-center gap-2 text-[12px] font-medium text-[color:var(--admin-danger)]">
                  <AlertIcon width={13} height={13} className="shrink-0" />
                  {error}
                </div>
              )}
            </div>

            <form onSubmit={handleSend} className="flex items-center gap-2 border-t border-[color:var(--admin-border)] p-3">
              <input
                value={input}
                onChange={(e) => setInput(e.target.value)}
                placeholder="Спросите что-нибудь…"
                disabled={busy}
                className="min-w-0 flex-1 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)] disabled:opacity-60"
              />
              <button
                type="submit"
                disabled={busy || !input.trim()}
                aria-label="Отправить"
                className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-[color:var(--admin-accent)] text-[color:var(--admin-accent-fg)] disabled:opacity-40"
              >
                <SendIcon width={16} height={16} />
              </button>
            </form>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
