import { apiFetch } from './client'

export interface PriceAlert {
  priceAlertId: number
  productId: number
  targetPrice: number
  currency: string
  isActive: boolean
}

export function getPriceAlerts() {
  return apiFetch<{ alerts: PriceAlert[] }>('/api/price-alerts')
}

export function createPriceAlert(productId: number, targetPrice: number, currency = 'TJS') {
  return apiFetch<{ priceAlertId: number }>('/api/price-alerts', {
    method: 'POST',
    body: { productId, targetPrice, currency },
  })
}

// There is no delete on the backend — an alert is switched off, not removed.
export function deactivatePriceAlert(alertId: number) {
  return apiFetch<{ outcome: string }>(`/api/price-alerts/${alertId}/deactivate`, {
    method: 'POST',
  })
}
