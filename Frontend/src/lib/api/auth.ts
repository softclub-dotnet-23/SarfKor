import { apiFetch, ApiError } from './client'

export interface AuthResult {
  userId: string
  accessToken: string
  refreshToken: string
  expiresAt: string
  /** True when someone else (a store owner) set this account's password on its behalf — the
   *  frontend must force a change-password screen before anything else. Backend always sends a
   *  real boolean here (defaults to false, never omitted) — not optional. */
  mustChangePassword: boolean
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

export function resetPassword(email: string, code: string, newPassword: string) {
  return apiFetch<void>('/api/auth/reset-password', {
    method: 'POST',
    auth: false,
    body: { email, code, newPassword },
  })
}

export type AcceptStoreEmployeeInvitationOutcome =
  | 'Accepted'
  | 'AccountAlreadyExisted'
  | 'NotFound'
  | 'Expired'
  | 'AlreadyAccepted'
  | 'Revoked'
  | 'PasswordRequired'
  | 'RegistrationFailed'

// `auth` is only present for 'Accepted' (a brand-new account) -- lets the caller log the new
// cashier straight in instead of sending them to /login separately. `password` is omitted
// entirely when the invitee already has an account (GetInvite's requiresPassword said so) --
// the backend never asks to touch an existing account's password from an email-link click.
export function acceptStoreEmployeeInvitation(token: string, displayName: string, password?: string) {
  return apiFetch<{ outcome: AcceptStoreEmployeeInvitationOutcome; auth: AuthResult | null }>('/api/auth/accept-invite', {
    method: 'POST',
    auth: false,
    body: { token, displayName, password },
  })
}

export type GetInviteOutcome = 'Valid' | 'NotFound' | 'Expired' | 'Accepted' | 'Revoked'

export interface InviteInfo {
  outcome: GetInviteOutcome
  /** "User" | "StorePartner" | "Admin" — the Identity role this invite grants. storeName/role
   *  (the Owner/Cashier sub-role) are only set when invitedRole is "StorePartner". */
  invitedRole: 'User' | 'StorePartner' | 'Admin' | null
  storeName: string | null
  email: string | null
  role: 'Owner' | 'Cashier' | null
  /** Backend always sends a real boolean when outcome is 'Valid' -- only actually absent (as
   *  `undefined`) via the local `{ outcome: 'NotFound' }` fallback getInvite() below builds for a
   *  404, which never reads this field. */
  requiresPassword?: boolean
}

// Public, unauthenticated -- backs the /invite/:token page's "who's inviting you to what" panel
// before the visitor commits to anything. A 404 (bad/garbled token) doesn't throw here — it's
// folded into the same InviteInfo shape as NotFound, so the page has one code path for every
// "this link doesn't work" case instead of a try/catch plus a switch.
export async function getInvite(token: string): Promise<InviteInfo> {
  try {
    return await apiFetch<InviteInfo>(`/api/auth/invite/${encodeURIComponent(token)}`, { auth: false })
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) {
      return { outcome: 'NotFound', invitedRole: null, storeName: null, email: null, role: null }
    }
    throw err
  }
}
