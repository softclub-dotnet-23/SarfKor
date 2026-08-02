import type { BarcodeScannerPhase } from '../../hooks/useBarcodeScanner'
import { CameraIcon } from './icons'

/**
 * Shared viewfinder + status messaging for both admin surfaces that scan a barcode
 * with the camera (PosPage, InventoryPage) — same `--admin-*` visual language in
 * both, so one component instead of duplicating this markup twice.
 */
const PHASE_MESSAGE: Partial<Record<BarcodeScannerPhase, string>> = {
  insecure:
    'Камера работает только по HTTPS (или на localhost) — по обычному http через локальный адрес браузер её не даёт использовать. Введите штрихкод вручную.',
  unsupported: 'Этот браузер не поддерживает сканирование камерой (работает в Chrome/Android). Введите штрихкод вручную.',
  denied: 'Доступ к камере запрещён. Разрешите его в настройках браузера или введите штрихкод вручную.',
  'no-camera': 'На этом устройстве не найдена камера. Введите штрихкод вручную.',
  error: 'Не удалось получить доступ к камере. Введите штрихкод вручную.',
}

interface BarcodeScannerViewProps {
  videoRef: React.RefObject<HTMLVideoElement | null>
  phase: BarcodeScannerPhase
  onStart: () => void
  className?: string
}

export function BarcodeScannerView({ videoRef, phase, onStart, className }: BarcodeScannerViewProps) {
  const live = phase === 'live'
  const canRetry = phase === 'denied' || phase === 'error'
  const message = PHASE_MESSAGE[phase]

  return (
    <div className={`relative overflow-hidden rounded-xl bg-black ${className ?? 'aspect-video w-full'}`}>
      <video
        ref={videoRef}
        playsInline
        muted
        className="h-full w-full object-cover"
        style={{ opacity: live ? 1 : 0, transition: 'opacity .4s' }}
      />

      {live && (
        <div
          aria-hidden
          className="pointer-events-none absolute left-1/2 top-1/2 h-[55%] w-[80%] -translate-x-1/2 -translate-y-1/2 rounded-lg border-2 border-white/80"
        />
      )}

      {!live && (
        <div className="absolute inset-0 flex flex-col items-center justify-center gap-3 px-6 text-center">
          {phase === 'idle' && (
            <button
              type="button"
              onClick={onStart}
              className="flex items-center gap-2 rounded-xl bg-white/10 px-4 py-2.5 text-[13px] font-semibold text-white hover:bg-white/15"
            >
              <CameraIcon width={15} height={15} />
              Включить камеру
            </button>
          )}
          {phase === 'starting' && <p className="text-[12.5px] text-white/75">Запускаем камеру…</p>}
          {message && <p className="max-w-[280px] text-[12px] leading-relaxed text-white/75">{message}</p>}
          {canRetry && (
            <button
              type="button"
              onClick={onStart}
              className="rounded-lg bg-white/10 px-3.5 py-2 text-[12px] font-semibold text-white hover:bg-white/15"
            >
              Попробовать снова
            </button>
          )}
        </div>
      )}
    </div>
  )
}
