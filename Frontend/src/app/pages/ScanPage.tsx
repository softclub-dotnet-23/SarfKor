import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, EASE, LINE, Reveal, TXT } from '../ui'
import { BarcodeGlyph } from './HomePage'

/**
 * Production-ready barcode scanner.
 *
 * Detection strategy (in order of preference):
 *   1. BarcodeDetector (Chrome/Chromium/Edge) — native, ~6 fps
 *   2. @zxing/browser BrowserMultiFormatReader — Safari/Firefox fallback
 *   3. Manual keypad — always available alongside the camera path
 *
 * Extras:
 *   - rear/front camera toggle (always rendered when camera is live)
 *   - torch (flashlight) when the device/track supports it
 *   - proper cleanup on unmount so the camera indicator light goes off
 */

const EAN_FORMATS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'itf']

type Phase = 'idle' | 'starting' | 'live' | 'denied' | 'error'

const HAS_MEDIA = typeof navigator !== 'undefined' && !!navigator.mediaDevices?.getUserMedia
const HAS_NATIVE = typeof window !== 'undefined' && 'BarcodeDetector' in window && HAS_MEDIA
const HAS_CAMERA = HAS_NATIVE || HAS_MEDIA

// ── ZXing lazy import ─────────────────────────────────────────────────────────
// ~300 kB; keep out of the initial bundle with a dynamic import.

type ZXingControls = { stop: () => void }

let zxingPromise: Promise<typeof import('@zxing/browser')> | null = null
function getZxingModule() {
  if (!zxingPromise) zxingPromise = import('@zxing/browser')
  return zxingPromise
}

// ── icon helpers ──────────────────────────────────────────────────────────────

function FlipIcon() {
  return (
    <svg width={18} height={18} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" aria-hidden>
      <path d="M1 4v6h6" /><path d="M23 20v-6h-6" />
      <path d="M20.49 9A9 9 0 005.64 5.64L1 10m22 4-4.64 4.36A9 9 0 013.51 15" />
    </svg>
  )
}

function TorchIcon({ on }: { on: boolean }) {
  return (
    <svg width={18} height={18} viewBox="0 0 24 24" fill={on ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" aria-hidden>
      <path d="M18 6c0-2.21-1.79-4-4-4H10C7.79 2 6 3.79 6 6v2l3 3v4.5a2.5 2.5 0 005 0V11l3-3z" />
    </svg>
  )
}

// ── component ─────────────────────────────────────────────────────────────────

export function ScanPage() {
  const navigate = useNavigate()
  const videoRef = useRef<HTMLVideoElement>(null)

  // Native (BarcodeDetector) path
  const streamRef = useRef<MediaStream | null>(null)
  const rafRef = useRef<number | null>(null)

  // ZXing path
  const zxingControlsRef = useRef<ZXingControls | null>(null)

  const [phase, setPhase] = useState<Phase>('idle')
  const [manual, setManual] = useState('')
  const [facing, setFacing] = useState<'environment' | 'user'>('environment')
  const [torch, setTorch] = useState(false)
  const [torchAvailable, setTorchAvailable] = useState(false)

  // ── cleanup ───────────────────────────────────────────────────────────────

  const stop = useCallback(() => {
    if (rafRef.current !== null) {
      cancelAnimationFrame(rafRef.current)
      rafRef.current = null
    }
    streamRef.current?.getTracks().forEach((t) => t.stop())
    streamRef.current = null

    if (zxingControlsRef.current) {
      try { zxingControlsRef.current.stop() } catch { /* */ }
      zxingControlsRef.current = null
    }

    setTorch(false)
    setTorchAvailable(false)
  }, [])

  useEffect(() => stop, [stop])

  // ── torch toggle ──────────────────────────────────────────────────────────

  const toggleTorch = useCallback(async () => {
    // Stream may be owned by us (native path) or ZXing (fallback path)
    const stream = streamRef.current ?? (videoRef.current?.srcObject as MediaStream | null)
    const track = stream?.getVideoTracks()[0]
    if (!track) return
    const next = !torch
    try {
      if (HAS_NATIVE) {
        // Native path: use applyConstraints directly
        await (track as MediaStreamTrack & { applyConstraints(c: object): Promise<void> })
          .applyConstraints({ advanced: [{ torch: next } as MediaTrackConstraintSet] })
      } else {
        // ZXing path: use ZXing's utility
        const { BrowserCodeReader } = await getZxingModule()
        await BrowserCodeReader.mediaStreamSetTorch(track, next)
      }
      setTorch(next)
    } catch { /* torch unsupported on this device */ }
  }, [torch])

  // ── check torch support after stream is live ──────────────────────────────

  function checkTorch(stream: MediaStream) {
    const track = stream.getVideoTracks()[0]
    if (!track) return
    const caps = track.getCapabilities?.() as (MediaTrackCapabilities & { torch?: boolean }) | undefined
    if (caps?.torch) { setTorchAvailable(true); return }
    // ZXing static helper as secondary check
    getZxingModule().then(({ BrowserCodeReader }) => {
      if (BrowserCodeReader.mediaStreamIsTorchCompatibleTrack(track)) setTorchAvailable(true)
    }).catch(() => { /* */ })
  }

  // ── native BarcodeDetector path ───────────────────────────────────────────

  async function startNative(facingMode: 'environment' | 'user') {
    const stream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode, width: { ideal: 1280 }, height: { ideal: 720 } },
    })
    streamRef.current = stream
    const video = videoRef.current!
    video.srcObject = stream
    await video.play()
    setPhase('live')
    checkTorch(stream)

    const detector = new BarcodeDetector({ formats: EAN_FORMATS })
    let last = 0
    const tick = async (ts: number) => {
      rafRef.current = null
      if (!streamRef.current) return
      if (ts - last > 160 && video.readyState >= 2) {
        last = ts
        try {
          const hits = await detector.detect(video)
          const code = hits[0]?.rawValue?.trim()
          if (code) { stop(); navigate(`/app/p/${encodeURIComponent(code)}`); return }
        } catch { /* single failed frame */ }
      }
      rafRef.current = requestAnimationFrame(tick)
    }
    rafRef.current = requestAnimationFrame(tick)
  }

  // ── ZXing fallback path ───────────────────────────────────────────────────

  async function startZxing(facingMode: 'environment' | 'user') {
    const { BrowserMultiFormatReader, BrowserCodeReader } = await getZxingModule()
    const reader = new BrowserMultiFormatReader()

    const controls = await reader.decodeFromConstraints(
      { video: { facingMode, width: { ideal: 1280 }, height: { ideal: 720 } } },
      videoRef.current!,
      (result, err) => {
        if (result) {
          const code = result.getText().trim()
          if (code) {
            controls.stop()
            navigate(`/app/p/${encodeURIComponent(code)}`)
          }
        }
        // NotFoundException per frame is normal — ignore it
        void err
      },
    )

    zxingControlsRef.current = controls
    setPhase('live')

    // Check torch on ZXing's stream (attached to the video element by ZXing)
    const stream = videoRef.current?.srcObject as MediaStream | null
    if (stream && BrowserCodeReader.mediaStreamIsTorchCompatible(stream)) {
      setTorchAvailable(true)
    }
  }

  // ── start (dispatches to native or ZXing) ─────────────────────────────────

  const start = useCallback(async (facingMode: 'environment' | 'user') => {
    stop()
    setPhase('starting')
    try {
      if (HAS_NATIVE) {
        await startNative(facingMode)
      } else {
        await startZxing(facingMode)
      }
    } catch (err) {
      const isNotAllowed =
        err instanceof DOMException &&
        (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError')
      setPhase(isNotAllowed ? 'denied' : 'error')
      stop()
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [stop, navigate])

  // ── flip camera ───────────────────────────────────────────────────────────

  const flip = useCallback(async () => {
    const next: 'environment' | 'user' = facing === 'environment' ? 'user' : 'environment'
    setFacing(next)
    if (phase === 'live' || phase === 'starting') await start(next)
  }, [facing, phase, start])

  // ── manual submit ─────────────────────────────────────────────────────────

  function submitManual(e: FormEvent) {
    e.preventDefault()
    const code = manual.trim()
    if (code) navigate(`/app/p/${encodeURIComponent(code)}`)
  }

  // ── render ────────────────────────────────────────────────────────────────

  return (
    <>
      <Reveal>
        <p
          className="mb-5 text-[10px] font-bold uppercase tracking-[0.28em]"
          style={{ color: TXT.rest }}
        >
          Сканирование
        </p>
        <h1 className="mb-10 text-[clamp(30px,4.6vw,46px)] font-extrabold leading-[1.04] tracking-[-0.04em]">
          Наведите на штрихкод
        </h1>
      </Reveal>

      {/* ── VIEWFINDER ───────────────────────────────────────────────── */}
      <Reveal i={1}>
        <div
          className="relative mb-8 aspect-[4/5] w-full overflow-hidden rounded-2xl border sm:aspect-[16/10]"
          style={{ borderColor: LINE, background: '#050505' }}
        >
          <video
            ref={videoRef}
            playsInline
            muted
            className="h-full w-full object-cover"
            style={{ opacity: phase === 'live' ? 1 : 0, transition: 'opacity .6s' }}
          />

          {phase === 'live' && (
            <>
              {/* dim vignette around target zone */}
              <div
                aria-hidden
                className="pointer-events-none absolute left-1/2 top-1/2 h-[36%] w-[74%] -translate-x-1/2 -translate-y-1/2 rounded-xl"
                style={{ boxShadow: '0 0 0 100vmax rgba(0,0,0,0.52)' }}
              />
              {/* corner bracket overlay */}
              <CornerFrame />
              <p
                className="absolute inset-x-0 bottom-5 text-center text-[12px]"
                style={{ color: 'rgba(255,255,255,0.7)' }}
              >
                Держите код внутри рамки
              </p>

              {/* camera controls — top-right */}
              <div className="absolute right-3 top-3 flex flex-col gap-2">
                <button
                  onClick={flip}
                  aria-label="Сменить камеру"
                  className="grid h-9 w-9 place-items-center rounded-full text-white transition-opacity hover:opacity-80"
                  style={{ background: 'rgba(0,0,0,0.5)', backdropFilter: 'blur(6px)' }}
                >
                  <FlipIcon />
                </button>
                {torchAvailable && (
                  <button
                    onClick={toggleTorch}
                    aria-label={torch ? 'Выключить фонарик' : 'Включить фонарик'}
                    className="grid h-9 w-9 place-items-center rounded-full transition-colors"
                    style={{
                      background: torch ? 'rgba(255,255,255,0.9)' : 'rgba(0,0,0,0.5)',
                      backdropFilter: 'blur(6px)',
                      color: torch ? '#000' : '#fff',
                    }}
                  >
                    <TorchIcon on={torch} />
                  </button>
                )}
              </div>
            </>
          )}

          {phase !== 'live' && (
            <div className="absolute inset-0 flex flex-col items-center justify-center px-8 text-center">
              <span className="mb-6" style={{ color: 'var(--app-text-primary)', opacity: 0.75 }}>
                <BarcodeGlyph size={38} />
              </span>

              {phase === 'idle' && (
                <>
                  <p className="mb-6 max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                    {HAS_CAMERA
                      ? 'Разрешите доступ к камере, чтобы найти товар по штрихкоду.'
                      : 'Камера недоступна — введите код вручную.'}
                  </p>
                  {HAS_CAMERA && <Button onClick={() => start(facing)}>Включить камеру</Button>}
                </>
              )}

              {phase === 'starting' && (
                <p className="text-[13.5px]" style={{ color: TXT.rest }}>
                  Запускаем камеру…
                </p>
              )}

              {phase === 'denied' && (
                <>
                  <p className="mb-6 max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                    Доступ к камере закрыт. Разрешите его в настройках браузера или введите код вручную.
                  </p>
                  <Button variant="ghost" onClick={() => start(facing)}>
                    Попробовать снова
                  </Button>
                </>
              )}

              {phase === 'error' && (
                <>
                  <p className="mb-6 max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                    Не удалось запустить камеру. Попробуйте снова или введите код вручную.
                  </p>
                  <Button variant="ghost" onClick={() => start(facing)}>
                    Попробовать снова
                  </Button>
                </>
              )}
            </div>
          )}
        </div>
      </Reveal>

      {/* ── MANUAL ENTRY ────────────────────────────────────────────── */}
      <Reveal i={2}>
        <form onSubmit={submitManual} className="flex items-end gap-4">
          <label className="flex-1">
            <span
              className="mb-2 block text-[10px] font-bold uppercase tracking-[0.16em]"
              style={{ color: TXT.rest }}
            >
              Ввести код вручную
            </span>
            <input
              value={manual}
              onChange={(e) => setManual(e.target.value.replace(/[^\d]/g, ''))}
              inputMode="numeric"
              autoComplete="off"
              placeholder="4780016470012"
              className="w-full border-0 bg-transparent pb-3 text-[17px] tracking-[0.05em] tabular-nums text-[color:var(--app-text-primary)] caret-[color:var(--app-text-primary)] placeholder:text-[color:var(--app-text-rest)]"
              style={{ borderBottom: `1px solid ${LINE}`, transition: `border-color .5s ${EASE}` }}
            />
          </label>
          <Button type="submit" disabled={!manual.trim()}>
            Найти
          </Button>
        </form>
      </Reveal>
    </>
  )
}

/** Four-corner bracket framing the scan target zone. */
function CornerFrame() {
  const S = 22
  const W = 2.5
  const stroke = 'rgba(255,255,255,0.88)'
  return (
    <svg
      aria-hidden
      className="pointer-events-none absolute left-1/2 top-1/2 h-[36%] w-[74%] -translate-x-1/2 -translate-y-1/2"
      viewBox="0 0 100 100"
      preserveAspectRatio="none"
    >
      <polyline points={`${S},${W/2} ${W/2},${W/2} ${W/2},${S}`}             fill="none" stroke={stroke} strokeWidth={W} />
      <polyline points={`${100-S},${W/2} ${100-W/2},${W/2} ${100-W/2},${S}`} fill="none" stroke={stroke} strokeWidth={W} />
      <polyline points={`${S},${100-W/2} ${W/2},${100-W/2} ${W/2},${100-S}`} fill="none" stroke={stroke} strokeWidth={W} />
      <polyline points={`${100-S},${100-W/2} ${100-W/2},${100-W/2} ${100-W/2},${100-S}`} fill="none" stroke={stroke} strokeWidth={W} />
    </svg>
  )
}
