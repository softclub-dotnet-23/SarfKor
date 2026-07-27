import { apiFetch } from './client'

export interface Review {
  reviewId: number
  userId: string
  storeId?: number
  rating: number
  comment: string
  createdAt: string
}

export function getReviews(productId: number) {
  return apiFetch<{ reviews: Review[] }>(`/api/products/${productId}/reviews`, { auth: false })
}

export function submitReview(productId: number, rating: number, comment: string, storeId?: number) {
  return apiFetch<{ reviewId: number }>(`/api/products/${productId}/reviews`, {
    method: 'POST',
    body: { storeId, rating, comment },
  })
}

export function replyToReview(reviewId: number, message: string) {
  return apiFetch<{ outcome: string; replyId?: number }>(`/api/reviews/${reviewId}/reply`, {
    method: 'POST',
    body: { message },
  })
}
