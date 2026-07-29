import { apiFetch } from './client'

export function reportOutOfStock(productId: number, description: string, storeId?: number) {
  return apiFetch<{ reportId: number }>('/api/reports/out-of-stock', {
    method: 'POST',
    body: { productId, storeId, description },
  })
}
