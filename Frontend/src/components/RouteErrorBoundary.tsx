import { Component, type ReactNode } from 'react'
import { useT } from '../i18n/translations'
import { ErrorState } from '../admin/components/ErrorState'

/**
 * A crash inside one routed page must not take the whole shell down with it — before this
 * existed, an uncaught render error anywhere below a shell's <Outlet> (e.g. SettingsPage's
 * AvatarSection throwing because a required provider was missing) unmounted React's entire tree,
 * menu and header included, leaving a blank screen with zero indication anything went wrong. Only
 * a class component can catch a render error (no hook equivalent yet) — this one keeps the actual
 * fallback UI in a normal function component (Fallback below) so it can still use useT()/hooks.
 */
interface Props {
  children: ReactNode
  /** Swap in a shell-native fallback (e.g. the consumer app's own <ErrorState>) instead of the
   *  admin-shell-styled default — same crash-containment behavior, just themed for that surface. */
  fallback?: (onRetry: () => void) => ReactNode
}

export class RouteErrorBoundary extends Component<Props, { error: Error | null }> {
  state: { error: Error | null } = { error: null }

  static getDerivedStateFromError(error: Error) {
    return { error }
  }

  componentDidCatch(error: Error, info: { componentStack?: string | null }) {
    // eslint-disable-next-line no-console
    console.error('RouteErrorBoundary caught:', error, info.componentStack)
  }

  render() {
    if (this.state.error) {
      const retry = () => this.setState({ error: null })
      return this.props.fallback ? this.props.fallback(retry) : <Fallback onRetry={retry} />
    }
    return this.props.children
  }
}

function Fallback({ onRetry }: { onRetry: () => void }) {
  const t = useT()
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center">
      <ErrorState message={t('common.routeCrashed')} onRetry={onRetry} />
    </div>
  )
}
