import { useCallback, useEffect, useMemo, useRef, useState } from 'react'

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
 *
 * Desktop demo note (2026-08-10): the phone build (facingMode: 'environment',
 * whatever resolution the phone defaults to) undersells badly on a laptop's
 * built-in webcam — lower native resolution, weaker autofocus, and a
 * request for a rear camera that plain doesn't exist. Device type now branches
 * the getUserMedia constraints instead of using one fixed request everywhere.
 */
export const DEFAULT_BARCODE_FORMATS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39']

// Same fractions the viewfinder draws its guide box at (BarcodeScannerView) — detection is
// cropped to this same central region instead of the full frame, both for speed (a smaller
// image is cheaper to run BarcodeDetector.detect() on) and to cut down on false positives from
// a second barcode sitting elsewhere in frame. Keep these two files in sync if either changes.
export const SCAN_REGION = { widthFraction: 0.72, heightFraction: 0.5 }

const CAMERA_DEVICE_STORAGE_KEY = 'sarfkor-scanner-camera-id'

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

// No User-Agent Client Hints dependency (Safari/Firefox don't implement navigator.userAgentData
// at all, and this whole scanner is Chromium-only anyway) — a plain UA regex is the same check
// every other "is this a phone" branch in the project already uses.
function isLikelyMobileDevice(): boolean {
  if (typeof navigator === 'undefined') return false
  return /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent)
}

// Plays a short, synthesized beep on a successful detection — no audio asset to ship, and it's
// the one piece of feedback that reads clearly from across a room during a demo, where a subtle
// border-color change might not. Silently no-ops if the Web Audio API is unavailable or the
// context can't start (autoplay policies) — a missing beep is not worth failing a scan over.
function playBeep() {
  try {
    const Ctx = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
    if (!Ctx) return
    const ctx = new Ctx()
    const oscillator = ctx.createOscillator()
    const gain = ctx.createGain()
    oscillator.type = 'sine'
    oscillator.frequency.value = 1400
    gain.gain.setValueAtTime(0.15, ctx.currentTime)
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.12)
    oscillator.connect(gain)
    gain.connect(ctx.destination)
    oscillator.start()
    oscillator.stop(ctx.currentTime + 0.12)
    oscillator.onended = () => ctx.close()
  } catch {
    /* no audio output available -- the visual flash still carries the feedback */
  }
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
  /** Off by default — a demo audience needs the beep, a busy shop floor with several phones
   *  scanning at once may not want every device chirping. */
  beepOnDetect?: boolean
}

export interface UseBarcodeScannerResult {
  videoRef: React.RefObject<HTMLVideoElement | null>
  phase: BarcodeScannerPhase
  /** Static capability check (secure context + BarcodeDetector + getUserMedia) — true even before `start()` is called. */
  supported: boolean
  start: () => Promise<void>
  stop: () => void
  /** Populated once permission has been granted at least once (device labels are blank/generic
   *  before that — a browser privacy restriction, not a bug here). Only worth showing a picker
   *  for when there's more than one. */
  devices: MediaDeviceInfo[]
  selectedDeviceId: string | null
  /** Switches camera — restarts the stream immediately if currently live, and persists the
   *  choice (localStorage) so the next scan on this device remembers it. */
  selectDevice: (deviceId: string) => void
  /** Flips true for ~500ms right after a fresh (non-duplicate) detection — drives the
   *  viewfinder's green-flash feedback. Not the same signal as onDetect firing on a duplicate. */
  justDetected: boolean
}

export function useBarcodeScanner(options: UseBarcodeScannerOptions): UseBarcodeScannerResult {
  const { onDetect, continuous = false, dedupeWindowMs = 1500, formats = DEFAULT_BARCODE_FORMATS, beepOnDetect = false } = options

  const videoRef = useRef<HTMLVideoElement>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const rafRef = useRef<number | null>(null)
  const canvasRef = useRef<HTMLCanvasElement | null>(null)
  const flashTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const startingRef = useRef(false)

  // Kept in refs (not deps) so the running detect loop and any in-flight start() always use the
  // latest values without needing to restart the camera every time the caller's closure changes.
  const onDetectRef = useRef(onDetect)
  onDetectRef.current = onDetect
  const beepOnDetectRef = useRef(beepOnDetect)
  beepOnDetectRef.current = beepOnDetect

  const [phase, setPhase] = useState<BarcodeScannerPhase>(() => detectStaticPhase() ?? 'idle')
  const [devices, setDevices] = useState<MediaDeviceInfo[]>([])
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(() => {
    try {
      return localStorage.getItem(CAMERA_DEVICE_STORAGE_KEY)
    } catch {
      return null
    }
  })
  const [justDetected, setJustDetected] = useState(false)

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
    if (flashTimeoutRef.current !== null) {
      clearTimeout(flashTimeoutRef.current)
      flashTimeoutRef.current = null
    }
    setJustDetected(false)
  }, [])

  // The camera must be released on unmount — otherwise the browser's recording
  // indicator stays lit after navigating away or the owning component unmounts.
  useEffect(() => stop, [stop])

  // Tries progressively looser constraints instead of one fixed request: 1920x1080 is what
  // actually fixes barcode detection on a laptop's built-in camera (a low-res feed is the #1
  // reason a real barcode just never resolves), but demanding it with `exact` would throw
  // OverconstrainedError on any camera that tops out lower -- `ideal` already degrades
  // gracefully in spec, this explicit two-step retry exists for the rarer case where the
  // *combination* of ideal-1080p + deviceId + advanced focus constraints gets rejected outright
  // by a particular driver, not just downgraded.
  const openStream = useCallback(async (deviceId: string | null): Promise<MediaStream> => {
    const mobile = isLikelyMobileDevice()
    const base: MediaTrackConstraints = {
      width: { ideal: 1920 },
      height: { ideal: 1080 },
      // Ignored per-item, not fatal, on a camera/browser that doesn't support it -- this is
      // exactly what the `advanced` constraint set is for (W3C Media Capture spec).
      advanced: [{ focusMode: 'continuous' } as unknown as MediaTrackConstraintSet],
    }
    if (deviceId) {
      base.deviceId = { exact: deviceId }
    } else if (mobile) {
      // Only ever requested on a phone/tablet -- a laptop has no rear camera, and asking for
      // one there can make getUserMedia reject the whole request instead of just picking
      // whatever camera exists.
      base.facingMode = { ideal: 'environment' }
    }

    try {
      return await navigator.mediaDevices.getUserMedia({ video: base })
    } catch {
      try {
        return await navigator.mediaDevices.getUserMedia({ video: { ...base, width: { ideal: 1280 }, height: { ideal: 720 } } })
      } catch {
        const minimal: MediaTrackConstraints | boolean = deviceId ? { deviceId: { exact: deviceId } } : mobile ? { facingMode: 'environment' } : true
        return navigator.mediaDevices.getUserMedia({ video: minimal === true ? true : minimal })
      }
    }
  }, [])

  const refreshDevices = useCallback(async () => {
    try {
      const all = await navigator.mediaDevices.enumerateDevices()
      setDevices(all.filter((d) => d.kind === 'videoinput'))
    } catch {
      /* device list is a nice-to-have (camera picker) -- never block scanning on it */
    }
  }, [])

  const runDetectionLoop = useCallback(
    (video: HTMLVideoElement, detector: BarcodeDetector) => {
      let canvas = canvasRef.current
      if (!canvas) {
        canvas = document.createElement('canvas')
        canvasRef.current = canvas
      }
      const ctx = canvas.getContext('2d', { willReadFrequently: true })

      let lastReadAt = 0
      let lastCode = ''
      let lastCodeAt = 0
      const tick = async (ts: number) => {
        rafRef.current = null
        if (!streamRef.current) return
        // ~5-6 reads/sec: detect() is expensive and running it every frame drops the
        // preview to a slideshow on a mid-range laptop or phone alike.
        if (ts - lastReadAt > 180 && video.readyState >= 2 && video.videoWidth > 0) {
          lastReadAt = ts
          try {
            // Crop to the same central region the viewfinder highlights (SCAN_REGION) instead
            // of handing the detector the full frame -- smaller image = faster detect(), and a
            // second barcode sitting elsewhere in frame (a shelf, another product) can't trigger
            // a false read.
            const sw = video.videoWidth * SCAN_REGION.widthFraction
            const sh = video.videoHeight * SCAN_REGION.heightFraction
            const sx = (video.videoWidth - sw) / 2
            const sy = (video.videoHeight - sh) / 2
            let source: CanvasImageSource = video
            if (ctx && canvas) {
              canvas.width = sw
              canvas.height = sh
              ctx.drawImage(video, sx, sy, sw, sh, 0, 0, sw, sh)
              source = canvas
            }
            const hits = await detector.detect(source)
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
                if (beepOnDetectRef.current) playBeep()
                setJustDetected(true)
                if (flashTimeoutRef.current !== null) clearTimeout(flashTimeoutRef.current)
                flashTimeoutRef.current = setTimeout(() => setJustDetected(false), 500)
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
    },
    [continuous, dedupeWindowMs, stop],
  )

  // Code review 2026-08-10 finding #2: this used to be `start`, called both directly and (via a
  // stale closure) from `selectDevice`. `start` was a useCallback whose deps included
  // selectedDeviceId, so a fresh version was created on every device change -- but `selectDevice`
  // itself was memoized on `[stop]` only (deliberately, to avoid recreating it and its consumers
  // on every render) and so *never* picked up that fresh `start`, permanently closing over
  // whatever `start` (and therefore whatever selectedDeviceId) existed the very first time
  // `selectDevice` was created. Net effect: picking a different camera from the dropdown updated
  // the displayed label but restarted the stream against the OLD device, not the new one.
  // Splitting the actual "open this specific device" logic out into its own function that takes
  // deviceId as an explicit parameter (instead of reading it from a closure) fixes this at the
  // root -- both `start` and `selectDevice` call it with the device id they actually mean, and
  // neither has to guess whether the other's closure is fresh.
  const startWithDevice = useCallback(
    async (deviceId: string | null) => {
      const staticPhase = detectStaticPhase()
      if (staticPhase) {
        setPhase(staticPhase)
        return
      }
      if (startingRef.current) return
      startingRef.current = true
      setPhase('starting')
      try {
        const stream = await openStream(deviceId)
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

        // Labels are blank until permission is granted at least once -- refresh now that it has
        // been, so a real camera-picker (not "Camera 1"/"Camera 2") can show up.
        void refreshDevices()

        const detector = new BarcodeDetector({ formats })
        runDetectionLoop(video, detector)
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
      } finally {
        startingRef.current = false
      }
    },
    [formats, openStream, refreshDevices, runDetectionLoop, stop],
  )

  const start = useCallback(() => startWithDevice(selectedDeviceId), [startWithDevice, selectedDeviceId])

  const selectDevice = useCallback(
    (deviceId: string) => {
      setSelectedDeviceId(deviceId)
      try {
        localStorage.setItem(CAMERA_DEVICE_STORAGE_KEY, deviceId)
      } catch {
        /* persistence is a convenience, not a requirement */
      }
      // Switching camera mid-scan needs a real restart -- a running MediaStream can't swap its
      // source device in place. Calls startWithDevice directly with the id just picked, not
      // `start()` -- see the comment on startWithDevice for why that distinction is the fix.
      if (streamRef.current) {
        stop()
        setTimeout(() => startWithDevice(deviceId), 0)
      }
    },
    [stop, startWithDevice],
  )

  // Device labels/list can change (a USB camera plugged in mid-session) -- listen instead of
  // only enumerating once at start().
  useEffect(() => {
    if (!supported) return
    navigator.mediaDevices.addEventListener?.('devicechange', refreshDevices)
    return () => navigator.mediaDevices.removeEventListener?.('devicechange', refreshDevices)
  }, [supported, refreshDevices])

  return useMemo(
    () => ({ videoRef, phase, supported, start, stop, devices, selectedDeviceId, selectDevice, justDetected }),
    [phase, supported, start, stop, devices, selectedDeviceId, selectDevice, justDetected],
  )
}
