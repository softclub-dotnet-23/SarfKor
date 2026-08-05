import { apiFetch } from './client'

// Shared, platform-wide catalog reference data — Product already references these by
// CategoryId/BrandId/TaxRateId. All lists are public (used for dropdowns). Any StorePartner can
// create a new Brand/Category outright (no approval needed); editing/deleting an existing one, and
// all of TaxRate, stays Admin-only since that can affect every other store already referencing it.

export interface Brand {
  brandId: number
  name: string
  productCount: number
}

export function getBrands(search?: string) {
  return apiFetch<{ brands: Brand[] }>('/api/catalog/brands', { auth: false, query: { search } })
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

export interface DuplicateBrand {
  brandId: number
  name: string
  productCount: number
}

export interface DuplicateBrandGroup {
  normalizedKey: string
  brands: DuplicateBrand[]
}

export function getBrandDuplicateCandidates() {
  return apiFetch<{ groups: DuplicateBrandGroup[] }>('/api/catalog/brands/duplicate-candidates')
}

export function mergeBrands(targetBrandId: number, sourceBrandIds: number[]) {
  return apiFetch<{ outcome: string; productsMoved: number }>('/api/catalog/brands/merge', {
    method: 'POST',
    body: { targetBrandId, sourceBrandIds },
  })
}

export interface Category {
  categoryId: number
  name: string
  parentCategoryId?: number
  displayOrder: number
  isHidden: boolean
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

export function updateCategory(categoryId: number, name: string, parentCategoryId: number | undefined, displayOrder: number, isHidden: boolean) {
  return apiFetch<{ outcome: string }>(`/api/catalog/categories/${categoryId}`, {
    method: 'PUT',
    body: { name, parentCategoryId, displayOrder, isHidden },
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
  effectiveFrom?: string
  effectiveTo?: string
}

export function getTaxRates() {
  return apiFetch<{ taxRates: TaxRate[] }>('/api/catalog/tax-rates', { auth: false })
}

export function createTaxRate(name: string, percentage: number, categoryId?: number, effectiveFrom?: string, effectiveTo?: string) {
  return apiFetch<{ taxRateId: number }>('/api/catalog/tax-rates', {
    method: 'POST',
    body: { name, percentage, categoryId, effectiveFrom, effectiveTo },
  })
}

export function updateTaxRate(
  taxRateId: number, name: string, percentage: number, categoryId?: number, effectiveFrom?: string, effectiveTo?: string,
) {
  return apiFetch<{ outcome: string }>(`/api/catalog/tax-rates/${taxRateId}`, {
    method: 'PUT',
    body: { name, percentage, categoryId, effectiveFrom, effectiveTo },
  })
}

export function deleteTaxRate(taxRateId: number) {
  return apiFetch<{ outcome: string }>(`/api/catalog/tax-rates/${taxRateId}`, { method: 'DELETE' })
}
