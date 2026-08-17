import { apiFetch } from './client'

/**
 * Fire-and-forget report of a render crash to the backend log (Railway has no monitoring
 * dashboard yet — CLAUDE.md §10 — so this is currently the only way a crash that only happened
 * in someone's browser becomes visible to us). Must never throw or reject upward: called from
 * RouteErrorBoundary.componentDidCatch, which is already handling one failure and cannot be
 * allowed to fail a second time because the *reporting* of the first failure broke.
 */
export function reportClientError(message: string, stack: string | undefined, section: string | undefined) {
  apiFetch('/api/client-errors', {
    method: 'POST',
    auth: true,
    body: { message, stack, section, url: window.location.href },
    timeoutMs: 5000,
  }).catch(() => {
    // Best-effort only -- if this itself fails (offline, backend down), there is nothing more
    // useful to do than what componentDidCatch's console.error already did.
  })
}
