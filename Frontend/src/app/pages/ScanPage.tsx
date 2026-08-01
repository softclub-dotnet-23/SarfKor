import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, EASE, LINE, Reveal, TXT } from '../ui'
import { BarcodeGlyph } from './HomePage'

/**
 * Mobile-first scanner.
 *
 * BarcodeDetector is Chromium-only (see types/barcode-detector.d.ts), so support
 * is feature-detected and every unsupported browser — Safari and Firefox, i.e. all
 * iPhones — silently gets the manual keypad instead. The camera is a fast path,
 * never the only path.
 */
const FORMATS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'itf']

type Phase = 'idle' | 'starting' | 'live' | 'denied' | 'unsupported'

export function ScanPage() {
  const navigate = useNavigate()
  const videoRef = useRef<HTMLVideoElement>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const rafRef = useRef<number | null>(null)
  const [phase, setPhase] = useState<Phase>('idle')
  const [manual, setManual] = useState('')

  const supported =
    typeof window !== 'undefined' &&
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

  // The camera must be released on unmount, otherwise the indicator light stays
  // on after navigating away.
  useEffect(() => stop, [stop])

  const start = useCallback(async () => {
    if (!supported) {
      setPhase('unsupported')
      return
    }
    setPhase('starting')
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment' },
      })
      streamRef.current = stream
      const video = videoRef.current
      if (!video) return
      video.srcObject = stream
      await video.play()
      setPhase('live')

      const detector = new BarcodeDetector({ formats: FORMATS })
      let last = 0
      const tick = async (ts: number) => {
        rafRef.current = null
        if (!streamRef.current) return
        // ~6 reads/sec: detect() is expensive and running it every frame drops
        // the preview to a slideshow on mid-range phones.
        if (ts - last > 160 && video.readyState >= 2) {
          last = ts
          try {
            const hits = await detector.detect(video)
            const code = hits[0]?.rawValue?.trim()
            if (code) {
              stop()
              navigate(`/app/p/${encodeURIComponent(code)}`)
              return
            }
          } catch {
            /* a single failed frame is not a failed scan */
          }
        }
        rafRef.current = requestAnimationFrame(tick)
      }
      rafRef.current = requestAnimationFrame(tick)
    } catch {
      setPhase('denied')
      stop()
    }
  }, [supported, stop, navigate])

  function submitManual(e: FormEvent) {
    e.preventDefault()
    const code = manual.trim()
    if (code) navigate(`/app/p/${encodeURIComponent(code)}`)
  }

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

      {/* ── VIEWFINDER ───────────────────────────────────────── */}
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
              {/* reticle */}
              <div
                aria-hidden
                className="pointer-events-none absolute left-1/2 top-1/2 h-[36%] w-[74%] -translate-x-1/2 -translate-y-1/2 rounded-xl"
                style={{ boxShadow: '0 0 0 100vmax rgba(0,0,0,0.55)' }}
              />
              <div
                aria-hidden
                className="pointer-events-none absolute left-1/2 top-1/2 h-[36%] w-[74%] -translate-x-1/2 -translate-y-1/2 rounded-xl border"
                style={{ borderColor: 'rgba(255,255,255,0.85)' }}
              />
              <p
                className="absolute inset-x-0 bottom-5 text-center text-[12px]"
                style={{ color: 'rgba(255,255,255,0.75)' }}
              >
                Держите код внутри рамки
              </p>
            </>
          )}

          {phase !== 'live' && (
            <div className="absolute inset-0 flex flex-col items-center justify-center px-8 text-center">
              <span className="mb-6 text-white" style={{ opacity: 0.85 }}>
                <BarcodeGlyph size={38} />
              </span>

              {phase === 'idle' && (
                <>
                  <p className="mb-6 max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                    {supported
                      ? 'Разрешите доступ к камере, чтобы найти товар по штрихкоду.'
                      : 'В этом браузере камера для сканирования недоступна — введите код вручную.'}
                  </p>
                  {supported && <Button onClick={start}>Включить камеру</Button>}
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
                    Доступ к камере закрыт. Разрешите его в настройках браузера или введите код
                    вручную.
                  </p>
                  <Button variant="ghost" onClick={start}>
                    Попробовать снова
                  </Button>
                </>
              )}

              {phase === 'unsupported' && (
                <p className="max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                  Этот браузер не поддерживает сканирование. Введите код вручную — результат будет
                  тот же.
                </p>
              )}
            </div>
          )}
        </div>
      </Reveal>

      {/* ── MANUAL ───────────────────────────────────────────── */}
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
              className="w-full border-0 bg-transparent pb-3 text-[17px] tracking-[0.05em] tabular-nums text-white caret-white placeholder:text-white/25"
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
