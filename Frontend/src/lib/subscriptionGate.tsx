import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { storesApi, type MyStoreSubscriptionStatus } from './api'
import { useAuth } from '../auth/AuthContext'

interface SubscriptionGateContextValue {
  /** True until the first status fetch resolves (or there's no storeId yet). Deliberately treats
   *  "still loading" the same as "operational" everywhere it's read -- flashing every write button
   *  disabled for a moment on every page load would be worse than the rare case where a click
   *  lands half a second before we know the store is actually closed (the write endpoint itself
   *  still enforces the real rule either way; this is only the proactive UI half). */
  loading: boolean
  /** Mirrors the backend's own IStoreAccessAuthorizer.IsOperationalAsync (see
   *  GetMyStoreSubscriptionStatusResult's doc comment) -- true unless we positively know the store
   *  is closed. Defaults true so a failed status fetch never itself blocks the cabinet. */
  isOperational: boolean
  info: MyStoreSubscriptionStatus | null
  refresh: () => void
}

const SubscriptionGateContext = createContext<SubscriptionGateContextValue>({
  loading: true,
  isOperational: true,
  info: null,
  refresh: () => {},
})

/**
 * Fetches the current store's subscription/operational status once per storeId and shares it down
 * the tree -- mounted once in CabinetShell/CashierShell so every page under it (and the shared
 * SubscriptionGateBanner) reads the same answer instead of each page re-fetching it independently.
 *
 * This is what makes "не давай заполнять то, что нельзя отправить" possible: a page checks
 * `isOperational` before opening a create/edit modal, and disables its mutating buttons with a
 * `title` explaining why, instead of only finding out after the user filled in the whole form and
 * hit submit.
 */
export function SubscriptionGateProvider({ children }: { children: ReactNode }) {
  const { storeId } = useAuth()
  const [info, setInfo] = useState<MyStoreSubscriptionStatus | null>(null)
  const [loading, setLoading] = useState(true)
  const [nonce, setNonce] = useState(0)

  useEffect(() => {
    if (!storeId) {
      setInfo(null)
      setLoading(false)
      return
    }
    let cancelled = false
    setLoading(true)
    storesApi
      .getMySubscriptionStatus(storeId)
      .then((res) => {
        if (!cancelled) setInfo(res)
      })
      .catch(() => {
        // Best-effort -- if this fails, every gated action still falls back to the reactive 402
        // path (FormModal/describeError), it just won't have been prevented proactively.
        if (!cancelled) setInfo(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [storeId, nonce])

  const isOperational = info?.outcome === 'Found' ? info.isOperational : true

  return (
    <SubscriptionGateContext.Provider value={{ loading, isOperational, info, refresh: () => setNonce((n) => n + 1) }}>
      {children}
    </SubscriptionGateContext.Provider>
  )
}

export interface SubscriptionGate {
  loading: boolean
  isOperational: boolean
  info: MyStoreSubscriptionStatus | null
  refresh: () => void
  /** From the JWT-derived store role, available synchronously (unlike `info.isOwner`, which waits
   *  on the fetch) -- a co-owner (StoreEmployeeRole.Owner) counts as owner, only 'Cashier' doesn't. */
  isOwner: boolean
}

/** `disabled`/`title` ready to spread onto a mutating trigger button -- `{}` when the store is
 *  operational (or still loading), so a normal button is unaffected either way. */
export function gateButtonProps(gate: SubscriptionGate, blockedText: string): { disabled?: boolean; title?: string } {
  if (gate.loading || gate.isOperational) return {}
  return { disabled: true, title: blockedText }
}

export function useSubscriptionGate(): SubscriptionGate {
  const ctx = useContext(SubscriptionGateContext)
  const { currentStoreRole } = useAuth()
  return { ...ctx, isOwner: currentStoreRole !== 'Cashier' }
}
