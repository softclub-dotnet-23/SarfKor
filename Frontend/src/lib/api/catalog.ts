import { apiFetch } from './client'

// Shared, platform-wide catalog reference data. Category/Brand/TaxRate are curated centrally
// (Admin) — Product already references them by CategoryId/BrandId/TaxRateId. All lists are public
// (used for dropdowns), writes require the Admin role.

export interface Brand {
  brandId: number
  name: string
}

export function getBrands() {
  return apiFetch<{ brands: Brand[] }>('/api/catalog/brands', { auth: false })
}

export function createBrand(name: string) {
  return apiFetch<{ brandId: number }>('/api/catalog/brands', { method: 'POST', body: { name } })
}

export function updateBrand(brandId: number, name: string) {
  return apiFetch<{ outcome: string }>(`/api/catalog/brands/${brandId}`, { method: 'PUT', body: { name } })
}

export function deleteBrand(brandId: number) {
  return apiFetch<{ outcome: string }>(`/api/catalog/brands/${brandId}`, { method: 'DELETE' })
}

export interface Category {
  categoryId: number
  name: string
  parentCategoryId?: number
}

export function getCategories() {
  return apiFetch<{ categories: Category[] }>('/api/catalog/categories', { auth: false })
}

export function createCategory(name: string, parentCategoryId?: number) {
  return apiFetch<{ outcome: string; categoryId?: number }>('/api/catalog/categories', {
    method: 'POST',
    body: { name, parentCategoryId },
  })
}

export function updateCategory(categoryId: number, name: string, parentCategoryId?: number) {
  return apiFetch<{ outcome: string }>(`/api/catalog/categories/${categoryId}`, {
    method: 'PUT',
    body: { name, parentCategoryId },
  })
}

export function deleteCategory(categoryId: number) {
  return apiFetch<{ outcome: string }>(`/api/catalog/categories/${categoryId}`, { method: 'DELETE' })
}

export interface TaxRate {
  taxRateId: number
  name: string
  percentage: number
  categoryId?: number
}

export function getTaxRates() {
  return apiFetch<{ taxRates: TaxRate[] }>('/api/catalog/tax-rates', { auth: false })
}

export function createTaxRate(name: string, percentage: number, categoryId?: number) {
  return apiFetch<{ taxRateId: number }>('/api/catalog/tax-rates', {
    method: 'POST',
    body: { name, percentage, categoryId },
  })
}

export function updateTaxRate(taxRateId: number, name: string, percentage: number, categoryId?: number) {
  return apiFetch<{ outcome: string }>(`/api/catalog/tax-rates/${taxRateId}`, {
    method: 'PUT',
    body: { name, percentage, categoryId },
  })
}

export function deleteTaxRate(taxRateId: number) {
  return apiFetch<{ outcome: string }>(`/api/catalog/tax-rates/${taxRateId}`, { method: 'DELETE' })
}
