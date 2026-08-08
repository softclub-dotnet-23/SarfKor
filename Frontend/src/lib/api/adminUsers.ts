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

// "Добавить пользователя" — invites by email into any of the three platform roles. Reuses the
// same StoreEmployeeInvitation mechanism StaffPage's cashier/owner invites run on (see the
// backend entity's own doc comment) — one invitation system, two authorized entry points.
export type InvitedRole = 'User' | 'StorePartner' | 'Admin'
export type UserInvitationStatus = 'Pending' | 'Accepted' | 'Revoked' | 'Expired'

export interface UserInvitationListItem {
  invitationId: number
  email: string
  invitedRole: InvitedRole
  storeId?: number
  storeName?: string
  employeeRole?: 'Owner' | 'Cashier'
  status: UserInvitationStatus
  expiresAt: string
  createdAt: string
  lastSentAt: string
}

export function getUserInvitations() {
  return apiFetch<{ invitations: UserInvitationListItem[] }>('/api/admin/users/invitations')
}

export type CreateUserInvitationOutcome = 'Sent' | 'Forbidden' | 'StoreNotFound'

export function createUserInvitation(email: string, invitedRole: InvitedRole, storeId?: number) {
  return apiFetch<{ outcome: CreateUserInvitationOutcome; invitationId?: number; expiresAt?: string }>('/api/admin/users/invitations', {
    method: 'POST',
    body: { email, invitedRole, storeId },
  })
}

export function resendUserInvitation(invitationId: number) {
  return apiFetch<{ outcome: string }>(`/api/admin/users/invitations/${invitationId}/resend`, { method: 'POST' })
}

export function revokeUserInvitation(invitationId: number) {
  return apiFetch<{ outcome: string }>(`/api/admin/users/invitations/${invitationId}/revoke`, { method: 'POST' })
}

