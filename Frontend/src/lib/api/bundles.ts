import { apiFetch } from './client'

export interface BundleItem {
  productId: number
  quantity: number
}

export interface ProductBundle {
  productBundleId: number
  name: string
  bundlePrice: number
  currency: string
  items: BundleItem[]
}

export interface CreateProductBundleResult {
  outcome: 'Created' | 'StoreNotFound' | 'Forbidden'
  productBundleId: number | null
}

export function createProductBundle(
  storeId: number,
  name: string,
  bundlePrice: number,
  currency: string,
  items: BundleItem[],
) {
  return apiFetch<CreateProductBundleResult>('/api/product-bundles', {
    method: 'POST',
    body: { storeId, name, bundlePrice, currency, items },
  })
}

export function getProductBundles(storeId: number) {
  return apiFetch<{ bundles: ProductBundle[] }>(`/api/stores/${storeId}/product-bundles`, { auth: false })
}
