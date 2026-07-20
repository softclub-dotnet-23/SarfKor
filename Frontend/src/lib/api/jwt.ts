// Decodes a JWT payload client-side for UI purposes only (e.g. showing the
// right nav / gating which onboarding screen to show). This is NOT a
// verification step — the backend is the only thing that actually checks
// the signature; a forged token would just get 401s from real endpoints.
export interface DecodedToken {
  sub: string
  email?: string
  role?: string | string[]
  exp: number
}

export function decodeJwt(token: string): DecodedToken | null {
  try {
    const payload = token.split('.')[1]
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(decodeURIComponent(escape(json))) as DecodedToken
  } catch {
    return null
  }
}

export function rolesFromToken(token: string): string[] {
  const decoded = decodeJwt(token)
  const role = decoded?.role
  if (!role) return []
  return Array.isArray(role) ? role : [role]
}
