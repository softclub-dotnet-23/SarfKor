import { apiFetch, apiUpload } from './client'

export interface ReceiptLineInput {
  productId?: number
  recognizedName?: string
  quantity: number
  price: number
  currency: string
}

export interface ReceiptLineComparison {
  productId?: number
  receiptPrice: number
  currentPrice?: number
  matches: boolean
}

export type VerifyReceiptOutcome = 'Verified' | 'Mismatched' | 'NotFound' | 'Forbidden' | 'MissingStore' | 'AlreadyProcessed'

export function uploadReceipt(file: File, lines: ReceiptLineInput[], storeId?: number) {
  const formData = new FormData()
  formData.append('file', file)
  if (storeId != null) formData.append('storeId', String(storeId))
  formData.append('linesJson', JSON.stringify(lines))
  return apiUpload<{ receiptId: number }>('/api/receipts/upload', formData)
}

export function verifyReceipt(receiptId: number) {
  return apiFetch<{ outcome: VerifyReceiptOutcome; lines?: ReceiptLineComparison[] }>(`/api/receipts/${receiptId}/verify`, {
    method: 'POST',
  })
}
