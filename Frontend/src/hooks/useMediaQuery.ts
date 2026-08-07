import { useEffect, useState } from 'react'

// Drives EntityPicker's dropdown-vs-fullscreen-sheet switch (and anything else that needs a real
// breakpoint check in JS, not just a Tailwind class) -- `640px` matches the project's existing
// `sm:` cutoff (see InventoryPage's table/card-list split).
export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(() => (typeof window === 'undefined' ? false : window.matchMedia(query).matches))

  useEffect(() => {
    const mql = window.matchMedia(query)
    const onChange = () => setMatches(mql.matches)
    onChange()
    mql.addEventListener('change', onChange)
    return () => mql.removeEventListener('change', onChange)
  }, [query])

  return matches
}

export function useIsMobile(): boolean {
  return useMediaQuery('(max-width: 640px)')
}
