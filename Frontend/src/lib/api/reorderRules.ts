import { apiFetch } from './client'

export interface CreateReorderRuleResult {
  outcome: 'Created' | 'StoreNotFound' | 'Forbidden'
  reorderRuleId?: number
}

export function createReorderRule(
  storeId: number,
  productId: number,
  thresholdQuantity: number,
  reorderQuantity: number,
  preferredSupplierId?: number,
) {
  return apiFetch<CreateReorderRuleResult>('/api/reorder-rules', {
    method: 'POST',
    body: { storeId, productId, thresholdQuantity, reorderQuantity, preferredSupplierId },
  })
}
