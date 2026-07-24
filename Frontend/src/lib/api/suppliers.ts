import { apiFetch } from './client'

export interface Supplier {
  supplierId: number
  name: string
  contactPhone?: string
  contactEmail?: string
}

export function getSuppliers() {
  return apiFetch<{ suppliers: Supplier[] }>('/api/suppliers')
}

export function createSupplier(name: string, contactPhone?: string, contactEmail?: string) {
  return apiFetch<{ supplierId: number }>('/api/suppliers', {
    method: 'POST',
    body: { name, contactPhone, contactEmail },
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
