import { apiFetch } from './client'

// Lightweight CRM record keyed by phone number. The backend has no
// "list all customers" endpoint — callers can only create a customer or
// look one up by exact phone number.

export interface Customer {
  customerId: number
  phoneNumber: string
  fullName?: string
}

export function createCustomer(phoneNumber: string, fullName?: string) {
  return apiFetch<{ customerId: number }>('/api/customers', {
    method: 'POST',
    body: { phoneNumber, fullName },
  })
}

export interface CustomerLookupResult {
  customerId: number | null
  fullName: string | null
}

export function getCustomerByPhone(phoneNumber: string) {
  return apiFetch<CustomerLookupResult>(`/api/customers/by-phone/${encodeURIComponent(phoneNumber)}`)
}
