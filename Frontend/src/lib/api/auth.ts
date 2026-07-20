import { apiFetch } from './client'

export interface AuthResult {
  userId: string
  accessToken: string
  refreshToken: string
  expiresAt: string
}

export function register(email: string, password: string) {
  return apiFetch<AuthResult>('/api/auth/register', {
    method: 'POST',
    auth: false,
    body: { email, password },
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
