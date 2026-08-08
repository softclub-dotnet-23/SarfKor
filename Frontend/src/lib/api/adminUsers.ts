import { apiFetch } from './client'

export interface AdminUserListItem {
  userId: string
  email?: string
  createdAt: string
  isBlocked: boolean
  roles: string[]
  trustScore?: number
}

export function getUsers(params: { skip?: number; take?: number; search?: string }) {
  return apiFetch<{ users: AdminUserListItem[]; totalCount: number }>('/api/admin/users', { query: params })
}

export interface UserStoreAttachment {
  storeId: number
  storeName: string
  relationship: string
}

export interface AdminUserDetail {
  outcome: 'Found' | 'NotFound'
  userId: string
  email?: string
  createdAt?: string
  isBlocked: boolean
  blockedReason?: string
  blockedAt?: string
  roles: string[]
  trustScore?: number
  priceSubmissionsTotal: number
  priceSubmissionsVerified: number
  reportsAgainstLast90Days: number
  stores: UserStoreAttachment[]
}

export function getUserDetail(userId: string) {
  return apiFetch<AdminUserDetail>(`/api/admin/users/${encodeURIComponent(userId)}`)
}

export function blockUser(userId: string, reason: string) {
  return apiFetch<{ outcome: string }>(`/api/admin/users/${encodeURIComponent(userId)}/block`, {
    method: 'POST',
    body: { reason },
  })
}

export function unblockUser(userId: string, reason: string) {
  return apiFetch<{ outcome: string }>(`/api/admin/users/${encodeURIComponent(userId)}/unblock`, {
    method: 'POST',
    body: { reason },
  })
}

export interface TrustScoreListItem {
  userId: string
  email?: string
  score: number
  updatedAt: string
}

export function getTrustScores(params: { skip?: number; take?: number }) {
  return apiFetch<{ scores: TrustScoreListItem[]; totalCount: number }>('/api/admin/users/trust-scores', { query: params })
}

