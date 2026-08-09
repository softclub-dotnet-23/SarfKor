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

// The backend (Infrastructure/Identity/JwtTokenGenerator) adds role claims via
// System.Security.Claims.ClaimTypes.Role, whose string value is this long XML
// namespace URI, not the short "role" — .NET does not shorten it on
// serialization. Reading only `.role` silently returns [] for every real
// token, which made hasRole('StorePartner') always false and sent users back
// to the onboarding screen right after successfully creating a store.
const ROLE_CLAIM_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

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
  const decoded = decodeJwt(token) as Record<string, unknown> | null
  const role = decoded?.role ?? decoded?.[ROLE_CLAIM_URI]
  if (!role) return []
  return Array.isArray(role) ? (role as string[]) : [role as string]
}

// Only present (as the string "true") when JwtTokenGenerator's own ApplicationUser.MustChangePassword
// was true at the moment this token was minted — absent entirely otherwise. Read on session restore
// (page reload), where AuthResult.mustChangePassword itself isn't available (see AuthContext).
export function mustChangePasswordFromToken(token: string): boolean {
  const decoded = decodeJwt(token) as Record<string, unknown> | null
  return decoded?.mustChangePassword === 'true'
}
