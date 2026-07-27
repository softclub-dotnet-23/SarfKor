import { apiFetch } from './client'

export interface ScanResultStore {
  storeId: number
  storeName: string
  price: number
  currency: string
  distanceKm?: number
}

export interface ScanBarcodeResult {
  productId: number
  productName: string
  stores: ScanResultStore[]
}

// Only exact-barcode lookup exists on the backend today — there is no
// name/keyword product search endpoint (see Backend audit notes).
export function scanBarcode(barcode: string, lat?: number, lng?: number) {
  return apiFetch<ScanBarcodeResult>(`/api/products/scan/${encodeURIComponent(barcode)}`, {
    query: { lat, lng },
    auth: false,
  })
}

export function getTopSellingProducts(storeId?: number, limit = 10) {
  return apiFetch<{ products: { productId: number; productName: string; totalQuantity: number }[] }>(
    '/api/products/top-selling',
    { query: { storeId, limit }, auth: false },
  )
}

// Fire-and-forget analytics — works for anonymous shoppers too (UserId is
// resolved server-side from the JWT if present, null otherwise).
export function recordScan(productId: number, storeId?: number) {
  return apiFetch<{ outcome: 'Recorded' | 'ProductNotFound' }>('/api/scans', {
    method: 'POST',
    body: { productId, storeId },
    auth: false,
  })
}

export interface StoreBasket {
  storeId: number
  storeName: string
  totalPrice: number
  currency: string
  distanceKm?: number
}

export function compareBasket(productIds: number[], lat?: number, lng?: number) {
  return apiFetch<{ stores: StoreBasket[] }>('/api/products/compare-basket', {
    query: { productIds, lat, lng },
    auth: false,
  })
}
