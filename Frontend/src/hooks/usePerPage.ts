import { useEffect, useState } from 'react'

const BREAKPOINTS: [number, number][] = [
  [1024, 4],
  [640, 2],
]

function computePerPage() {
  if (typeof window === 'undefined') return 4
  const width = window.innerWidth
  for (const [minWidth, perPage] of BREAKPOINTS) {
    if (width >= minWidth) return perPage
  }
  return 1
}

export function usePerPage() {
  const [perPage, setPerPage] = useState(computePerPage)

  useEffect(() => {
    const onResize = () => setPerPage(computePerPage())
    window.addEventListener('resize', onResize)
    return () => window.removeEventListener('resize', onResize)
  }, [])

  return perPage
}
