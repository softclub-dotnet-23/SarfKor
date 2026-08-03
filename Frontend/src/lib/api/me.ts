import { apiFetch, apiUpload } from './client'

export interface UserProfile {
  found: boolean
  displayName?: string
  avatarReference?: string
  preferredLanguage?: string
}

export function getProfile() {
  return apiFetch<UserProfile>('/api/me/profile')
}

export function updateProfile(displayName: string, avatarReference: string | undefined, preferredLanguage: string) {
  return apiFetch<{ userProfileId: number }>('/api/me/profile', {
    method: 'PUT',
    body: { displayName, avatarReference, preferredLanguage },
  })
}

export type ConsentType = 'Geolocation' | 'ReceiptStorage' | 'PaymentData' | 'Marketing'

export interface UserConsent {
  type: ConsentType
  isGranted: boolean
  recordedAt: string
}

export function getConsents() {
  return apiFetch<{ consents: UserConsent[] }>('/api/me/consents')
}

export function recordConsent(type: ConsentType, isGranted: boolean) {
  return apiFetch<{ userConsentId: number }>('/api/me/consents', { method: 'PUT', body: { type, isGranted } })
}

export type SecurityEventType = 'LoginSucceeded' | 'LoginFailed' | 'NewDeviceLogin' | 'PasswordChanged' | 'AnomalousActivity'

export interface SecurityEvent {
  type: SecurityEventType
  ipAddress?: string
  userAgent?: string
  occurredAt: string
}

export function getSecurityEvents() {
  return apiFetch<{ events: SecurityEvent[] }>('/api/me/security-events')
}

export type MyStoreRole = 'Owner' | 'Cashier'

export interface MyStore {
  storeId: number
  name: string
  role: MyStoreRole
}

export function getMyStores() {
  return apiFetch<{ stores: MyStore[] }>('/api/me/stores')
}

export function changePassword(currentPassword: string, newPassword: string) {
  return apiFetch<{ outcome: 'Changed' | 'WrongCurrentPassword' | 'UserNotFound' }>('/api/me/password', {
    method: 'PUT',
    body: { currentPassword, newPassword },
  })
}

export function uploadAvatar(file: File) {
  const form = new FormData()
  form.append('file', file)
  return apiUpload<{ avatarUrl: string }>('/api/me/avatar', form)
}
