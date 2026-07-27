import { apiFetch } from './client'

export interface Supplier {
  supplierId: number
  name: string
  contactPhone?: string
  contactEmail?: string
}

// Suppliers are global (not scoped to a store) — confirmed against the
// backend source, which has no storeId on either endpoint below.
export function createSupplier(name: string, contactPhone?: string, contactEmail?: string) {
  return apiFetch<{ supplierId: number }>('/api/suppliers', {
    method: 'POST',
    body: { name, contactPhone, contactEmail },
  })
}

export function getSuppliers() {
  return apiFetch<{ suppliers: Supplier[] }>('/api/suppliers')
}
