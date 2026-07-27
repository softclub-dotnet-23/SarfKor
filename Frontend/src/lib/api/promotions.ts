import { apiFetch } from './client'

export type PromotionDiscountType = 'PercentageOff' | 'FixedAmountOff' | 'BuyOneGetOne'

export interface Promotion {
  promotionId: number
  productId?: number
  categoryId?: number
  discountType: string
  discountValue: number
  startsAt: string
  endsAt: string
}

export interface CreatePromotionInput {
  productId?: number
  categoryId?: number
  discountType: PromotionDiscountType
  discountValue: number
  /** ISO datetime string */
  startsAt: string
  /** ISO datetime string */
  endsAt: string
}

export interface CreatePromotionResult {
  outcome: 'Created' | 'StoreNotFound' | 'Forbidden'
  promotionId?: number
}

export function createPromotion(storeId: number, input: CreatePromotionInput) {
  return apiFetch<CreatePromotionResult>('/api/promotions', {
    method: 'POST',
    body: { storeId, ...input },
  })
}

export function getActivePromotions(storeId: number) {
  return apiFetch<{ promotions: Promotion[] }>(`/api/stores/${storeId}/promotions/active`, { auth: false })
}
