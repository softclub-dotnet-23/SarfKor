import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button, EASE, LINE, Reveal, TXT } from '../ui'
import { BarcodeGlyph } from './HomePage'
import { useBarcodeScanner } from '../../hooks/useBarcodeScanner'

/**
 * Mobile-first scanner.
 *
 * BarcodeDetector is Chromium-only (see types/barcode-detector.d.ts), so support
 * is feature-detected and every unsupported browser — Safari and Firefox, i.e. all
 * iPhones — silently gets the manual keypad instead. The camera is a fast path,
 * never the only path.
 */
export function ScanPage() {
  const navigate = useNavigate()
  const [manual, setManual] = useState('')

  const { videoRef, phase, supported, start } = useBarcodeScanner({
    onDetect: (code) => navigate(`/app/p/${encodeURIComponent(code)}`),
  })

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
              <span className="mb-6 text-[color:var(--app-text-primary)]" style={{ opacity: 0.85 }}>
                <BarcodeGlyph size={38} />
              </span>

              {phase === 'idle' && supported && (
                <>
                  <p className="mb-6 max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                    Разрешите доступ к камере, чтобы найти товар по штрихкоду.
                  </p>
                  <Button onClick={start}>Включить камеру</Button>
                </>
              )}

              {phase === 'starting' && (
                <p className="text-[13.5px]" style={{ color: TXT.rest }}>
                  Запускаем камеру…
                </p>
              )}

              {(phase === 'denied' || phase === 'error') && (
                <>
                  <p className="mb-6 max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                    {phase === 'denied'
                      ? 'Доступ к камере закрыт. Разрешите его в настройках браузера или введите код вручную.'
                      : 'Не удалось получить доступ к камере. Введите код вручную.'}
                  </p>
                  <Button variant="ghost" onClick={start}>
                    Попробовать снова
                  </Button>
                </>
              )}

              {phase === 'no-camera' && (
                <p className="max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                  На этом устройстве не найдена камера. Введите код вручную.
                </p>
              )}

              {phase === 'unsupported' && (
                <p className="max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                  Этот браузер не поддерживает сканирование. Введите код вручную — результат будет
                  тот же.
                </p>
              )}

              {phase === 'insecure' && (
                <p className="max-w-[300px] text-[13.5px]" style={{ color: TXT.secondary }}>
                  Камера работает только по HTTPS (или на localhost) — с этого адреса браузер её не
                  даёт использовать. Введите код вручную.
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
