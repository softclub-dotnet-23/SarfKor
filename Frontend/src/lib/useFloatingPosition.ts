import { useLayoutEffect, useState, type RefObject } from 'react'

export interface FloatingPosition {
  left: number
  width: number
  maxHeight: number
  /** Exactly one of top/bottom is set — position:fixed anchors from whichever side has room, so
   *  the panel never has to know its own height in advance to decide where to render. */
  top?: number
  bottom?: number
}

/**
 * Shared positioning for every floating panel that portals to document.body instead of rendering
 * `absolute` inside its trigger's own DOM subtree (Select, CategoryPicker, EntityPicker,
 * SectionSelect). Portaling is what stops a modal's `overflow-hidden`/`overflow-y-auto` from
 * clipping the panel; this hook is what keeps it anchored to the trigger once it's no longer a
 * DOM descendant of it, including flipping above the trigger when there isn't enough room below
 * (e.g. a field near the bottom of a modal).
 *
 * Recomputes on open, and on any resize/scroll while open — `scroll` is registered with
 * `capture: true` specifically so it also fires for scrolling *inside* a nested container (a
 * modal's own `overflow-y-auto` body), not just the window itself.
 */
export function useFloatingPosition(triggerRef: RefObject<HTMLElement | null>, open: boolean, gap = 6): FloatingPosition | null {
  const [pos, setPos] = useState<FloatingPosition | null>(null)

  useLayoutEffect(() => {
    if (!open) {
      setPos(null)
      return
    }

    function update() {
      const el = triggerRef.current
      if (!el) return
      const rect = el.getBoundingClientRect()
      const viewportH = window.innerHeight
      const margin = 8
      const spaceBelow = viewportH - rect.bottom - gap - margin
      const spaceAbove = rect.top - gap - margin
      const flip = spaceBelow < 160 && spaceAbove > spaceBelow

      setPos({
        left: rect.left,
        width: rect.width,
        maxHeight: Math.max(120, Math.min(400, flip ? spaceAbove : spaceBelow)),
        ...(flip ? { bottom: viewportH - rect.top + gap } : { top: rect.bottom + gap }),
      })
    }

    update()
    window.addEventListener('resize', update)
    window.addEventListener('scroll', update, true)
    return () => {
      window.removeEventListener('resize', update)
      window.removeEventListener('scroll', update, true)
    }
  }, [open, triggerRef, gap])

  return pos
}
