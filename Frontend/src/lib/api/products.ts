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

export interface SubmitNewProductRequest {
  barcode: string
  name: string
  categoryId: number
  brandId: number
  countryOfOrigin: string
}

// Submits an unrecognized barcode for Admin moderation (see /admin/moderation) -- does not create
// the Product directly. Any authenticated user can submit; approval is what actually creates it.
export function submitNewProduct(req: SubmitNewProductRequest) {
  return apiFetch<{ outcome: string; productSubmissionId?: number }>('/api/products/submissions', {
    method: 'POST',
    body: req,
  })
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
