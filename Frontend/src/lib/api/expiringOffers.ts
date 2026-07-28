import { apiFetch } from './client'

export interface ExpiringOffer {
  offerId: number
  productId: number
  productName: string
  storeId: number
  storeName: string
  originalPrice: number
  discountedPrice: number
  currency: string
  expiresAt: string
  distanceKm?: number
}

export interface PublishExpiringOfferResult {
  outcome: 'Published' | 'StoreNotFound' | 'ProductNotFound' | 'Forbidden'
  offerId?: number
}

export function publishExpiringOffer(
  storeId: number,
  productId: number,
  originalPrice: number,
  discountedPrice: number,
  currency: string,
  expiresAt: string,
) {
  return apiFetch<PublishExpiringOfferResult>('/api/offers', {
    method: 'POST',
    body: { storeId, productId, originalPrice, discountedPrice, currency, expiresAt },
  })
}

export function getExpiringOffersForStore(storeId: number) {
  return apiFetch<{ offers: ExpiringOffer[] }>('/api/offers/expiring', {
    query: { storeId },
    auth: false,
  })
}

// Consumer-facing browse: nearby offers across all stores instead of one store's own list.
export function getExpiringOffersNearby(lat?: number, lng?: number) {
  return apiFetch<{ offers: ExpiringOffer[] }>('/api/offers/expiring', {
    query: { lat, lng },
    auth: false,
  })
}
