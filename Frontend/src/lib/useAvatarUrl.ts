import { useEffect, useState } from 'react'
import { meApi } from './api'

/**
 * Turns the caller's own avatar into an <img>-ready object URL. The backend endpoint requires a
 * Bearer token (per-file access control, not a public asset), so a plain <img src="/api/me/avatar">
 * can't carry the Authorization header itself — this fetches the bytes once and hands back a blob
 * URL instead, revoking it on unmount/change to avoid leaking memory across avatar swaps.
 */
export function useAvatarUrl(hasAvatar: boolean | undefined, refreshKey: unknown = null) {
  const [url, setUrl] = useState<string | null>(null)

  useEffect(() => {
    if (!hasAvatar) {
      setUrl(null)
      return
    }
    let cancelled = false
    let objectUrl: string | null = null
    meApi.fetchAvatarBlob().then((blob) => {
      if (cancelled || !blob) return
      objectUrl = URL.createObjectURL(blob)
      setUrl(objectUrl)
    })
    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
    // refreshKey is intentionally in the dep array purely to force a re-fetch (e.g. right after a
    // new avatar upload) even when hasAvatar was already true and wouldn't otherwise change.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasAvatar, refreshKey])

  return url
}
