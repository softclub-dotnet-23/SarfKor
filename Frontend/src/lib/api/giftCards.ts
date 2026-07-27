import { apiFetch } from './client'

// Anonymous, code-based gift cards — not tied to a customerId. The code is
// only ever returned once, at issue time; there is no endpoint to recover it
// afterward, so the caller must show/copy it immediately.

export function issueGiftCard(amount: number, currency: string, expiresAt?: string) {
  return apiFetch<{ giftCardId: number; code: string }>('/api/gift-cards', {
    method: 'POST',
    body: { amount, currency, expiresAt },
  })
}

export type RedeemGiftCardOutcome = 'Redeemed' | 'NotFound' | 'Inactive' | 'Expired' | 'InsufficientBalance'

export function redeemGiftCard(code: string, amount: number) {
  return apiFetch<{ outcome: RedeemGiftCardOutcome; remainingBalance?: number }>(
    `/api/gift-cards/${encodeURIComponent(code)}/redeem`,
    { method: 'POST', body: { amount } },
  )
}

export interface GiftCardBalance {
  found: boolean
  balance: number | null
  currency: string | null
  isActive: boolean | null
  expiresAt: string | null
}

// Public read — no auth required, matches backend (no [Authorize] attribute).
export function getGiftCardBalance(code: string) {
  return apiFetch<GiftCardBalance>(`/api/gift-cards/${encodeURIComponent(code)}`, { auth: false })
}
