import { Component, type ReactNode } from 'react'
import { useT } from '../i18n/translations'
import { ErrorState } from '../admin/components/ErrorState'
import { reportClientError } from '../lib/api/clientErrors'

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
  /** Human label for whatever this boundary wraps (e.g. "Сотрудники") — turns the generic "не
   *  удалось показать этот раздел" into "не удалось загрузить раздел «Сотрудники»", so a crash
   *  says what broke instead of just that something did. Each shell derives this from the current
   *  route (see CabinetShell/CashierShell/AdminConsoleLayout) since one boundary instance is
   *  reused across every route under it, keyed by pathname. */
  sectionLabel?: string
}

export class RouteErrorBoundary extends Component<Props, { error: Error | null }> {
  state: { error: Error | null } = { error: null }

  static getDerivedStateFromError(error: Error) {
    return { error }
  }

  componentDidCatch(error: Error, info: { componentStack?: string | null }) {
    // eslint-disable-next-line no-console
    console.error('RouteErrorBoundary caught:', error, info.componentStack)
    // Technical detail (message/stack) never reaches the screen (see Fallback below) — this is
    // the one place it's preserved anywhere outside this one browser tab.
    reportClientError(error.message, error.stack, this.props.sectionLabel)
  }

  render() {
    if (this.state.error) {
      const retry = () => this.setState({ error: null })
      return this.props.fallback ? this.props.fallback(retry) : <Fallback onRetry={retry} sectionLabel={this.props.sectionLabel} />
    }
    return this.props.children
  }
}

function Fallback({ onRetry, sectionLabel }: { onRetry: () => void; sectionLabel?: string }) {
  const t = useT()
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center">
      <ErrorState message={sectionLabel ? t('common.routeCrashedNamed', { section: sectionLabel }) : t('common.routeCrashed')} onRetry={onRetry} />
    </div>
  )
}
