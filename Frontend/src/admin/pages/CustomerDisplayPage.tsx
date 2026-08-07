import { useEffect, useRef, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { LogoMark } from '../../components/Logo'
import { subscribeToCustomerDisplay, type CustomerDisplayState } from '../lib/customerDisplay'
import { CheckIcon } from '../components/icons'

function fmt(n: number) {
  return n.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// Full-screen, no admin chrome (sidebar/header) — meant to run in its own window on a
// second monitor facing the customer, not to be navigated like a normal admin page.
export function CustomerDisplayPage() {
  const [state, setState] = useState<CustomerDisplayState | null>(null)
  const [justCompleted, setJustCompleted] = useState<{ amount: number; currency: string } | null>(null)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    const unsubscribe = subscribeToCustomerDisplay((next) => {
      if (next.completedTotal) {
        setJustCompleted(next.completedTotal)
        if (timerRef.current) clearTimeout(timerRef.current)
        timerRef.current = setTimeout(() => setJustCompleted(null), 4000)
      }
      setState(next)
    })
    return () => {
      unsubscribe()
      if (timerRef.current) clearTimeout(timerRef.current)
    }
  }, [])

  const lines = state?.lines ?? []
  const total = state?.total ?? 0
  const currency = state?.currency ?? 'TJS'

  return (
    <div className="admin-shell flex h-screen w-full flex-col bg-[color:var(--admin-content)] p-10 text-[color:var(--admin-text)]">
      <div className="mb-8 flex items-center gap-3">
        <LogoMark size={36} />
        <span className="text-[26px] font-extrabold tracking-tight">Sarfkor</span>
      </div>

      <AnimatePresence mode="wait">
        {justCompleted ? (
          <motion.div
            key="thanks"
            initial={{ opacity: 0, scale: 0.96 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0 }}
            className="flex flex-1 flex-col items-center justify-center gap-6 text-center"
          >
            <span className="grid h-24 w-24 place-items-center rounded-full bg-[color:var(--admin-success-dim)] text-[color:var(--admin-success)]">
              <CheckIcon width={48} height={48} />
            </span>
            <div className="text-[40px] font-extrabold">Спасибо за покупку!</div>
            <div className="text-[28px] font-bold text-[color:var(--admin-text-secondary)]">
              {fmt(justCompleted.amount)} {justCompleted.currency}
            </div>
          </motion.div>
        ) : (
          <motion.div key="cart" initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="flex flex-1 flex-col">
            {lines.length === 0 ? (
              <div className="flex flex-1 flex-col items-center justify-center gap-2 text-center text-[color:var(--admin-text-tertiary)]">
                <div className="text-[32px] font-bold">Ожидаем сканирование…</div>
                <div className="text-[16px]">Товары появятся здесь по мере сканирования кассиром</div>
              </div>
            ) : (
              <div className="flex flex-1 flex-col gap-3 overflow-y-auto">
                <AnimatePresence initial={false}>
                  {lines.map((line) => (
                    <motion.div
                      key={line.key}
                      initial={{ opacity: 0, x: 20 }}
                      animate={{ opacity: 1, x: 0 }}
                      exit={{ opacity: 0, x: -20 }}
                      className="flex items-center justify-between rounded-2xl bg-[color:var(--admin-hover)] px-6 py-4"
                    >
                      <div className="min-w-0 flex-1">
                        <div className="truncate text-[22px] font-bold">{line.name}</div>
                        <div className="text-[15px] text-[color:var(--admin-text-tertiary)]">
                          {fmt(line.unitPrice)} {currency} × {line.quantity}
                        </div>
                      </div>
                      <div className="shrink-0 text-[24px] font-extrabold">
                        {fmt(line.unitPrice * line.quantity)} {currency}
                      </div>
                    </motion.div>
                  ))}
                </AnimatePresence>
              </div>
            )}

            <div className="mt-6 flex items-center justify-between rounded-2xl bg-[color:var(--admin-accent)] px-8 py-6">
              <span className="text-[24px] font-bold text-[color:var(--admin-accent-fg)]">Итого</span>
              <span className="text-[44px] font-extrabold text-[color:var(--admin-accent-fg)]">
                {fmt(total)} {currency}
              </span>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
