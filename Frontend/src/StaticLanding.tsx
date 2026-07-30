import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'

// The landing is a static, same-origin document in public/landing. It owns its own
// markup and animations, but deliberately owns *no* auth logic: instead of a second
// copy of the fetch/token/JWT stack it posts intents up here and this bridge runs
// them through the one real implementation (AuthContext -> lib/api). That keeps a
// single source of truth for token storage, refresh-on-expiry and error wording.
interface LandingRequest {
  source: 'sarfkor-landing'
  id: number
  action: 'session' | 'login' | 'register' | 'logout' | 'navigate'
  email?: string
  password?: string
  to?: string
}

// Only these destinations may be requested. The landing is our own document, but
// treating its messages as untrusted input costs nothing and stops a redirect
// primitive existing at all.
const ALLOWED_ROUTES = new Set(['/login', '/register'])

function isLandingRequest(data: unknown): data is LandingRequest {
  if (!data || typeof data !== 'object') return false
  const d = data as Record<string, unknown>
  return (
    d.source === 'sarfkor-landing' &&
    typeof d.id === 'number' &&
    (d.action === 'session' ||
      d.action === 'login' ||
      d.action === 'register' ||
      d.action === 'logout' ||
      d.action === 'navigate')
  )
}

export function StaticLanding() {
  const frameRef = useRef<HTMLIFrameElement>(null)
  const auth = useAuth()
  const navigate = useNavigate()

  // Kept in a ref so the message listener below can stay mounted for the life of the
  // page (re-subscribing on every auth change would drop in-flight replies).
  const authRef = useRef(auth)
  authRef.current = auth
  const navigateRef = useRef(navigate)
  navigateRef.current = navigate

  useEffect(() => {
    async function onMessage(event: MessageEvent) {
      // Only trust our own origin, and only the frame we rendered — a nested or
      // third-party document must not be able to drive login with our credentials.
      if (event.origin !== window.location.origin) return
      if (!frameRef.current || event.source !== frameRef.current.contentWindow) return
      if (!isLandingRequest(event.data)) return

      const { id, action, email, password, to } = event.data
      const reply = (payload: Record<string, unknown>) =>
        frameRef.current?.contentWindow?.postMessage(
          { source: 'sarfkor-app', id, ...payload },
          window.location.origin,
        )

      const a = authRef.current

      if (action === 'session') {
        reply({
          ok: true,
          loading: a.loading,
          user: a.user ? { email: a.user.email, roles: a.user.roles } : null,
        })
        return
      }

      if (action === 'navigate') {
        if (to && ALLOWED_ROUTES.has(to)) {
          reply({ ok: true })
          navigateRef.current(to)
        } else {
          reply({ ok: false, error: 'route not allowed' })
        }
        return
      }

      if (action === 'logout') {
        a.logout()
        reply({ ok: true, user: null })
        return
      }

      if (!email || !password) {
        reply({ ok: false, error: 'Заполните все поля' })
        return
      }

      const result = action === 'login' ? await a.login(email, password) : await a.register(email, password)

      if (!result.ok) {
        reply({ ok: false, error: result.error })
        return
      }

      reply({ ok: true, user: { email, roles: result.roles } })

      // Same post-auth routing the React LoginPage uses, so signing in from the
      // landing lands a partner/admin in the cabinet rather than back on the film.
      const dest = result.roles.includes('Admin')
        ? '/admin/moderation'
        : result.roles.includes('StorePartner')
          ? '/admin'
          : null
      if (dest) navigateRef.current(dest)
    }

    window.addEventListener('message', onMessage)
    return () => window.removeEventListener('message', onMessage)
  }, [])

  // Push session changes down so the landing header reflects login/logout that
  // happened anywhere in the app (including the initial restore, which resolves async).
  useEffect(() => {
    frameRef.current?.contentWindow?.postMessage(
      {
        source: 'sarfkor-app',
        id: 0,
        ok: true,
        loading: auth.loading,
        user: auth.user ? { email: auth.user.email, roles: auth.user.roles } : null,
      },
      window.location.origin,
    )
  }, [auth.user, auth.loading])

  return (
    <iframe
      ref={frameRef}
      src="/landing/index.html"
      title="Sarfkor"
      style={{ display: 'block', width: '100vw', height: '100vh', border: 0 }}
    />
  )
}
