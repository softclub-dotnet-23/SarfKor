import { apiFetch } from './client'

export interface AuthResult {
  userId: string
  accessToken: string
  refreshToken: string
  expiresAt: string
}

// Never returns tokens directly anymore — every self-registration requires the emailed 6-digit
// code to be confirmed first (see confirmEmail below).
export function register(email: string, password: string) {
  return apiFetch<{ requiresEmailConfirmation: true; email: string }>('/api/auth/register', {
    method: 'POST',
    auth: false,
    body: { email, password },
  })
}

export function confirmEmail(email: string, code: string) {
  return apiFetch<AuthResult>('/api/auth/confirm-email', {
    method: 'POST',
    auth: false,
    body: { email, code },
  })
}

export function login(email: string, password: string) {
  return apiFetch<AuthResult>('/api/auth/login', {
    method: 'POST',
    auth: false,
    body: { email, password },
  })
}

export function refresh(refreshToken: string) {
  return apiFetch<AuthResult>('/api/auth/refresh', {
    method: 'POST',
    auth: false,
    body: { refreshToken },
  })
}

export function forgotPassword(email: string) {
  return apiFetch<void>('/api/auth/forgot-password', {
    method: 'POST',
    auth: false,
    body: { email },
  })
}

export function resetPassword(email: string, token: string, newPassword: string) {
  return apiFetch<void>('/api/auth/reset-password', {
    method: 'POST',
    auth: false,
    body: { email, token, newPassword },
  })
}

export type AcceptStoreEmployeeInvitationOutcome = 'Accepted' | 'AccountAlreadyExisted' | 'InvalidOrExpiredToken' | 'RegistrationFailed'

// `auth` is only present for 'Accepted' (a brand-new account) -- lets the caller log the new
// cashier straight in instead of sending them to /login separately.
export function acceptStoreEmployeeInvitation(token: string, password: string) {
  return apiFetch<{ outcome: AcceptStoreEmployeeInvitationOutcome; auth?: AuthResult }>('/api/auth/accept-invite', {
    method: 'POST',
    auth: false,
    body: { token, password },
  })
}
