import { useCallback, useEffect, useRef, useState } from 'react'

/**
 * Camera-scanning logic shared by ScanPage (consumer), PosPage and InventoryPage
 * (StorePartner cabinet) — previously only ScanPage had this, duplicating it into
 * the admin pages would have meant three copies of the same getUserMedia/rAF/
 * BarcodeDetector lifecycle (a direct SRP/DRY violation per CLAUDE.md §2).
 *
 * BarcodeDetector is Chromium-only and getUserMedia requires a secure context
 * (https, or localhost) — both are feature-detected, never assumed. Every
 * unsupported combination (Safari/Firefox, or a plain-http origin like a phone
 * hitting a dev server over a LAN IP) falls back to manual entry, never a
 * broken UI or a console error.
 */
export const DEFAULT_BARCODE_FORMATS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'itf']

export type BarcodeScannerPhase =
  | 'idle'
  | 'starting'
  | 'live'
  | 'denied'
  | 'no-camera'
  | 'insecure'
  | 'unsupported'
  | 'error'

function detectStaticPhase(): BarcodeScannerPhase | null {
  if (typeof window === 'undefined') return 'idle'
  if (!window.isSecureContext) return 'insecure'
  if (!('BarcodeDetector' in window) || !navigator.mediaDevices?.getUserMedia) return 'unsupported'
  return null
}

export interface UseBarcodeScannerOptions {
  /** Called with the raw barcode value on every successful, de-duplicated detection. */
  onDetect: (code: string) => void
  /**
   * false (default): stop the camera after the first hit — for a single scan-and-close
   * flow (ScanPage, the inventory "receipt by barcode" modal).
   * true: keep scanning after a hit — for POS, where a cashier rings up many items in a
   * row without re-opening anything. Either way, repeated reads of the *same* code within
   * dedupeWindowMs are swallowed (detect() runs ~6x/sec, so a code held in frame for a
   * second would otherwise fire a dozen times).
   */
  continuous?: boolean
  dedupeWindowMs?: number
  formats?: string[]
}

export interface UseBarcodeScannerResult {
  videoRef: React.RefObject<HTMLVideoElement | null>
  phase: BarcodeScannerPhase
  /** Static capability check (secure context + BarcodeDetector + getUserMedia) — true even before `start()` is called. */
  supported: boolean
  start: () => Promise<void>
  stop: () => void
}

export function useBarcodeScanner(options: UseBarcodeScannerOptions): UseBarcodeScannerResult {
  const { onDetect, continuous = false, dedupeWindowMs = 1500, formats = DEFAULT_BARCODE_FORMATS } = options

  const videoRef = useRef<HTMLVideoElement>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const rafRef = useRef<number | null>(null)

  // Kept in a ref (not a dep) so the running detect loop always calls the latest
  // callback without needing to restart the camera when the caller's closure changes.
  const onDetectRef = useRef(onDetect)
  onDetectRef.current = onDetect

  const [phase, setPhase] = useState<BarcodeScannerPhase>(() => detectStaticPhase() ?? 'idle')

  const supported =
    typeof window !== 'undefined' &&
    window.isSecureContext &&
    'BarcodeDetector' in window &&
    !!navigator.mediaDevices?.getUserMedia

  const stop = useCallback(() => {
    if (rafRef.current !== null) {
      cancelAnimationFrame(rafRef.current)
      rafRef.current = null
    }
    streamRef.current?.getTracks().forEach((t) => t.stop())
    streamRef.current = null
  }, [])

  // The camera must be released on unmount — otherwise the browser's recording
  // indicator stays lit after navigating away or the owning component unmounts.
  useEffect(() => stop, [stop])

  const start = useCallback(async () => {
    const staticPhase = detectStaticPhase()
    if (staticPhase) {
      setPhase(staticPhase)
      return
    }
    setPhase('starting')
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } })
      streamRef.current = stream
      const video = videoRef.current
      if (!video) {
        stream.getTracks().forEach((t) => t.stop())
        streamRef.current = null
        return
      }
      video.srcObject = stream
      await video.play()
      setPhase('live')

      const detector = new BarcodeDetector({ formats })
      let lastReadAt = 0
      let lastCode = ''
      let lastCodeAt = 0
      const tick = async (ts: number) => {
        rafRef.current = null
        if (!streamRef.current) return
        // ~6 reads/sec: detect() is expensive and running it every frame drops the
        // preview to a slideshow on mid-range phones.
        if (ts - lastReadAt > 160 && video.readyState >= 2) {
          lastReadAt = ts
          try {
            const hits = await detector.detect(video)
            const code = hits[0]?.rawValue?.trim()
            if (code) {
              // A code still visible in frame keeps re-detecting every tick, which
              // keeps refreshing lastCodeAt — so this only re-fires once the code has
              // actually left frame for >dedupeWindowMs and reappeared (a deliberate
              // re-scan), not merely because a fixed timer expired while it sat still.
              const isDuplicate = code === lastCode && ts - lastCodeAt < dedupeWindowMs
              lastCode = code
              lastCodeAt = ts
              if (!isDuplicate) {
                onDetectRef.current(code)
                if (!continuous) {
                  stop()
                  return
                }
              }
            }
          } catch {
            /* a single failed frame is not a failed scan */
          }
        }
        rafRef.current = requestAnimationFrame(tick)
      }
      rafRef.current = requestAnimationFrame(tick)
    } catch (err) {
      if (err instanceof DOMException && (err.name === 'NotFoundError' || err.name === 'DevicesNotFoundError')) {
        setPhase('no-camera')
      } else if (
        err instanceof DOMException &&
        (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError' || err.name === 'SecurityError')
      ) {
        setPhase('denied')
      } else {
        setPhase('error')
      }
      stop()
    }
  }, [continuous, dedupeWindowMs, formats, stop])

  return { videoRef, phase, supported, start, stop }
}
