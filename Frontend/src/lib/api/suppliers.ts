import { apiFetch } from './client'

export interface Supplier {
  supplierId: number
  name: string
  contactPhone: string | null
  contactEmail: string | null
}

// Suppliers are scoped to the store that created them — the backend checks the caller is that
// store's owner/employee on every action.
export function getSuppliers(storeId: number) {
  return apiFetch<{ suppliers: Supplier[] }>('/api/suppliers', { query: { storeId } })
}

export function createSupplier(storeId: number, name: string, contactPhone?: string, contactEmail?: string) {
  return apiFetch<{ supplierId: number }>('/api/suppliers', {
    method: 'POST',
    body: { storeId, name, contactPhone, contactEmail },
  })
}

export function updateSupplier(supplierId: number, name: string, contactPhone?: string, contactEmail?: string) {
  return apiFetch<{ outcome: string }>(`/api/suppliers/${supplierId}`, {
    method: 'PUT',
    body: { name, contactPhone, contactEmail },
  })
}

export function deleteSupplier(supplierId: number) {
  return apiFetch<{ outcome: string }>(`/api/suppliers/${supplierId}`, { method: 'DELETE' })
}
