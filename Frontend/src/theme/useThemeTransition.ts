import { flushSync } from 'react-dom'

type ViewTransitionDocument = Document & {
  startViewTransition?: (callback: () => void) => { finished: Promise<void> }
}

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
    transition.finished.finally(() => root.classList.remove('theme-wipe'))
  }

  return { runThemeTransition }
}
