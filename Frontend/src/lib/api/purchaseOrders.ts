import { apiFetch } from './client'

export interface PurchaseOrderLine {
  productId: number
  quantity: number
  unitCost: number
  currency: string
}

export interface PurchaseOrder {
  purchaseOrderId: number
  supplierId: number
  status: 'Draft' | 'Submitted' | 'Received' | 'Cancelled'
  createdAt: string
  receivedAt?: string
}

export interface CreatePurchaseOrderResult {
  outcome: 'Created' | 'StoreNotFound' | 'Forbidden'
  purchaseOrderId?: number
}

export function createPurchaseOrder(storeId: number, supplierId: number, lines: PurchaseOrderLine[]) {
  return apiFetch<CreatePurchaseOrderResult>('/api/purchase-orders', {
    method: 'POST',
    body: { storeId, supplierId, lines },
  })
}

export interface SubmitPurchaseOrderResult {
  outcome: 'Submitted' | 'NotFound' | 'Forbidden' | 'NotDraft'
}

export function submitPurchaseOrder(purchaseOrderId: number) {
  return apiFetch<SubmitPurchaseOrderResult>(`/api/purchase-orders/${purchaseOrderId}/submit`, {
    method: 'POST',
  })
}

export interface ReceivePurchaseOrderResult {
  outcome: 'Received' | 'NotFound' | 'Forbidden' | 'NotSubmitted'
}

export function receivePurchaseOrder(purchaseOrderId: number) {
  return apiFetch<ReceivePurchaseOrderResult>(`/api/purchase-orders/${purchaseOrderId}/receive`, {
    method: 'POST',
  })
}

export interface GetPurchaseOrdersResult {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  orders?: PurchaseOrder[]
}

export function getPurchaseOrders(storeId: number) {
  return apiFetch<GetPurchaseOrdersResult>(`/api/stores/${storeId}/purchase-orders`)
}
