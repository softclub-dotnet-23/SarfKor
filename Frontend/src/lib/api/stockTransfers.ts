import { apiFetch } from './client'

export interface StockTransfer {
  stockTransferId: number
  productId: number
  fromStoreId: number
  toStoreId: number
  quantity: number
  status: 'Pending' | 'InTransit' | 'Completed' | 'Cancelled'
  createdAt: string
  completedAt?: string
}

export interface InitiateStockTransferResult {
  outcome: 'Initiated' | 'FromStoreNotFound' | 'ToStoreNotFound' | 'Forbidden' | 'InsufficientStock'
  stockTransferId?: number
}

export function initiateStockTransfer(productId: number, fromStoreId: number, toStoreId: number, quantity: number) {
  return apiFetch<InitiateStockTransferResult>('/api/stock-transfers', {
    method: 'POST',
    body: { productId, fromStoreId, toStoreId, quantity },
  })
}

export interface CompleteStockTransferResult {
  outcome: 'Completed' | 'NotFound' | 'Forbidden' | 'NotInTransit'
}

export function completeStockTransfer(stockTransferId: number) {
  return apiFetch<CompleteStockTransferResult>(`/api/stock-transfers/${stockTransferId}/complete`, {
    method: 'POST',
  })
}

export interface GetStockTransfersResult {
  outcome: 'Found' | 'StoreNotFound' | 'Forbidden'
  transfers?: StockTransfer[]
}

export function getStockTransfers(storeId: number) {
  return apiFetch<GetStockTransfersResult>(`/api/stores/${storeId}/stock-transfers`)
}
