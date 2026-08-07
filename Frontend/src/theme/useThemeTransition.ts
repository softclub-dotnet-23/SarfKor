import { flushSync } from 'react-dom'

type ViewTransition = { finished: Promise<void>; skipTransition: () => void }
type ViewTransitionDocument = Document & {
  startViewTransition?: (callback: () => void) => ViewTransition
}

// Module-level, not component state -- theme is a single global concept (one <html> element),
// so "is a transition currently in flight" has to be shared across every call site using this
// hook, not scoped per-component.
let activeTransition: ViewTransition | null = null

/**
 * Runs `apply` (a theme state change) inside a circular View Transition that
 * expands from `origin`'s center — the same effect everywhere theme can be
 * changed (public toggle, admin topbar, settings page), so the "ripple
 * spreading from the button" feel is never button-specific one-off code.
 * Falls back to an instant, unanimated `apply()` when the browser doesn't
 * support View Transitions or the user prefers reduced motion.
 */
export function useThemeTransition() {
  function runThemeTransition(origin: HTMLElement, apply: () => void) {
    const vtDocument = document as ViewTransitionDocument
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches
    if (reduceMotion || !vtDocument.startViewTransition) {
      apply()
      return
    }

    // Toggling again before the previous 0.85s reveal finished used to leave a permanent black
    // wedge stuck in the corner: the browser's own pseudo-elements for the first transition never
    // got torn down while --vt-x/--vt-y were overwritten out from under them by the second click,
    // so the abandoned clip-path kept rendering pinned at (0,0). skipTransition() is the browser's
    // own clean-cancellation API for exactly this — end whatever's in flight before starting a new
    // one, instead of letting two transitions race over the same custom properties.
    activeTransition?.skipTransition()

    const rect = origin.getBoundingClientRect()
    const x = rect.left + rect.width / 2
    const y = rect.top + rect.height / 2
    const root = document.documentElement
    const r = Math.hypot(Math.max(x, window.innerWidth - x), Math.max(y, window.innerHeight - y))
    root.style.setProperty('--vt-x', `${x}px`)
    root.style.setProperty('--vt-y', `${y}px`)
    root.style.setProperty('--vt-r', `${r}px`)
    root.classList.add('theme-wipe')

    const transition = vtDocument.startViewTransition(() => {
      flushSync(apply)
    })
    activeTransition = transition
    transition.finished.finally(() => {
      root.classList.remove('theme-wipe')
      if (activeTransition === transition) activeTransition = null
    })
    // Belt-and-suspenders: the animation is 0.85s, so if .finished still hasn't settled by 2s
    // something went wrong browser-side -- force the wedge gone rather than leave it stuck forever.
    setTimeout(() => root.classList.remove('theme-wipe'), 2000)
  }

  return { runThemeTransition }
}
