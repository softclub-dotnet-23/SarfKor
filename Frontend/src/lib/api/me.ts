import { apiFetch, apiFetchBlob, apiUpload } from './client'

export interface UserProfile {
  found: boolean
  displayName: string | null
  avatarReference: string | null
  preferredLanguage: string | null
}

export function getProfile() {
  return apiFetch<UserProfile>('/api/me/profile')
}

export function updateProfile(displayName: string, avatarReference: string | null | undefined, preferredLanguage: string) {
  return apiFetch<{ userProfileId: number }>('/api/me/profile', {
    method: 'PUT',
    body: { displayName, avatarReference, preferredLanguage },
  })
}

export function changePassword(currentPassword: string, newPassword: string) {
  return apiFetch<void>('/api/me/password', { method: 'POST', body: { currentPassword, newPassword } })
}

export function uploadAvatar(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return apiUpload<{ avatarReference: string }>('/api/me/avatar', formData)
}

/** Object-URL-ready blob of the caller's own avatar, or null if none is set — see useAvatarUrl. */
export function fetchAvatarBlob() {
  return apiFetchBlob('/api/me/avatar')
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
  ipAddress: string | null
  userAgent: string | null
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
