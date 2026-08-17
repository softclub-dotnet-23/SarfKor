import { apiFetch } from './client'

export interface Review {
  reviewId: number
  userId: string
  storeId: number | null
  rating: number
  comment: string
  createdAt: string
}

export function getReviews(productId: number) {
  return apiFetch<{ reviews: Review[] }>(`/api/products/${productId}/reviews`, { auth: false })
}

export function replyToReview(reviewId: number, message: string) {
  return apiFetch<{ outcome: string; replyId: number | null }>(`/api/reviews/${reviewId}/reply`, {
    method: 'POST',
    body: { message },
  })
}
