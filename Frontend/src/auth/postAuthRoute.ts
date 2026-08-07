// Single source of truth for "where does this account belong after signing in" --
// previously copied (Admin -> console, StorePartner -> cabinet, else -> app/onboarding)
// once in AuthPage.tsx and again, partially, in StaticLanding.tsx.
export function getRoleHomeRoute(roles: string[], opts: { isNewRegistration?: boolean } = {}): string {
  if (roles.includes('Admin')) return '/admin/overview'
  if (roles.includes('StorePartner')) return '/admin'
  return opts.isNewRegistration ? '/admin/onboarding' : '/app'
}

// The platform Admin console (01-06 nav, see AdminConsoleLayout) -- everything
// under here is gated by RequireAdmin, not RequireStore.
const ADMIN_CONSOLE_PATHS = ['/admin/overview', '/admin/stores', '/admin/subscriptions', '/admin/users', '/admin/reference', '/admin/audit-log']

// A deep-link restored from location.state.from (RequireAuth bouncing a signed-out
// visitor to /login) must not override where this role actually belongs -- only a
// deep-link that falls within the account's own section is honored. Without this, a
// stale/incidental link into /app (the consumer app, nobody's "own section" once they
// hold Admin or StorePartner) would win over the role-based destination every time.
export function isDeepLinkAppropriateForRole(pathname: string, roles: string[]): boolean {
  if (roles.includes('Admin')) return ADMIN_CONSOLE_PATHS.some((p) => pathname.startsWith(p))
  if (roles.includes('StorePartner')) return pathname.startsWith('/admin') && !ADMIN_CONSOLE_PATHS.some((p) => pathname.startsWith(p))
  return pathname.startsWith('/app') || pathname === '/admin/onboarding'
}

export function resolvePostAuthRoute(
  roles: string[],
  fromPathname: string | undefined,
  opts: { isNewRegistration?: boolean } = {},
): string {
  if (fromPathname && isDeepLinkAppropriateForRole(fromPathname, roles)) return fromPathname
  return getRoleHomeRoute(roles, opts)
}
