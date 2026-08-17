import { useState } from 'react'
import { SCAN_REGION, type BarcodeScannerPhase } from '../../hooks/useBarcodeScanner'
import { CameraIcon, ChevronDownIcon } from './icons'

/**
 * Shared viewfinder + status messaging for every surface that scans a barcode with the camera
 * (PosPage, InventoryPage, ProductPicker) — same `--admin-*` visual language everywhere instead
 * of duplicating this markup per screen.
 */
const PHASE_MESSAGE: Partial<Record<BarcodeScannerPhase, string>> = {
  insecure:
    'Камера работает только по HTTPS (или на localhost) — по обычному http через локальный адрес браузер её не даёт использовать. Введите штрихкод вручную.',
  unsupported: 'Этот браузер не поддерживает сканирование камерой (работает в Chrome). Введите штрихкод вручную.',
  denied:
    'Доступ к камере запрещён. Нажмите на значок камеры в адресной строке браузера и разрешите доступ, затем нажмите «Попробовать снова» — либо введите штрихкод вручную.',
  'no-camera': 'На этом устройстве не найдена камера. Введите штрихкод вручную.',
  error: 'Не удалось получить доступ к камере. Введите штрихкод вручную.',
}

interface BarcodeScannerViewProps {
  videoRef: React.RefObject<HTMLVideoElement | null>
  phase: BarcodeScannerPhase
  onStart: () => void
  className?: string
  /** Flips true for ~500ms right after a fresh detection — flashes the guide frame green and
   *  is the one piece of feedback visible from across a room during a live demo. */
  justDetected?: boolean
  /** More than one camera (built-in + a plugged-in USB one, common on a demo laptop) — shows a
   *  picker in the corner instead of silently always using whichever one the browser defaults to. */
  devices?: MediaDeviceInfo[]
  selectedDeviceId?: string | null
  onSelectDevice?: (deviceId: string) => void
  /** 'large' (desktop-appropriate: a real chunk of the screen, not a phone-sized rectangle) vs
   *  the default compact size used inside modals/pickers. */
  size?: 'default' | 'large'
}

function CameraSelect({
  devices,
  selectedDeviceId,
  onSelectDevice,
}: {
  devices: MediaDeviceInfo[]
  selectedDeviceId?: string | null
  onSelectDevice: (id: string) => void
}) {
  const [open, setOpen] = useState(false)
  const current = devices.find((d) => d.deviceId === selectedDeviceId) ?? devices[0]

  return (
    <div className="absolute right-2.5 top-2.5 z-10">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-1.5 rounded-lg bg-black/55 px-2.5 py-1.5 text-[11.5px] font-medium text-white backdrop-blur-sm hover:bg-black/70"
      >
        <CameraIcon width={13} height={13} />
        <span className="max-w-[140px] truncate">{current?.label || 'Камера'}</span>
        <ChevronDownIcon width={11} height={11} className={`transition-transform ${open ? 'rotate-180' : ''}`} />
      </button>
      {open && (
        <div className="absolute right-0 top-full mt-1 min-w-[220px] overflow-hidden rounded-lg bg-black/85 py-1 backdrop-blur-sm">
          {devices.map((d, i) => (
            <button
              key={d.deviceId}
              type="button"
              onClick={() => {
                onSelectDevice(d.deviceId)
                setOpen(false)
              }}
              className={`block w-full truncate px-3 py-2 text-left text-[12px] hover:bg-white/10 ${
                d.deviceId === selectedDeviceId ? 'font-semibold text-white' : 'text-white/75'
              }`}
            >
              {d.label || `Камера ${i + 1}`}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

export function BarcodeScannerView({
  videoRef,
  phase,
  onStart,
  className,
  justDetected,
  devices,
  selectedDeviceId,
  onSelectDevice,
  size = 'default',
}: BarcodeScannerViewProps) {
  const live = phase === 'live'
  const canRetry = phase === 'denied' || phase === 'error'
  const message = PHASE_MESSAGE[phase]
  const defaultSizeClass = size === 'large' ? 'aspect-video w-full min-h-[320px] sm:min-h-[420px]' : 'aspect-video w-full'

  return (
    <div className={`relative overflow-hidden rounded-xl bg-black ${className ?? defaultSizeClass}`}>
      <video
        ref={videoRef}
        playsInline
        muted
        // Never mirrored -- a horizontally-flipped barcode does not decode. object-cover (not
        // contain) so the ROI crop math (SCAN_REGION, in the scanning hook) lines up with what's
        // actually visible instead of leaving letterboxed bars the guide frame ends up covering.
        className="h-full w-full object-cover"
        style={{ opacity: live ? 1 : 0, transition: 'opacity .4s' }}
      />

      {live && devices && devices.length > 1 && onSelectDevice && (
        <CameraSelect devices={devices} selectedDeviceId={selectedDeviceId} onSelectDevice={onSelectDevice} />
      )}

      {live && (
        <div
          aria-hidden
          className="pointer-events-none absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 rounded-lg border-2 transition-colors duration-150"
          style={{
            width: `${SCAN_REGION.widthFraction * 100}%`,
            height: `${SCAN_REGION.heightFraction * 100}%`,
            borderColor: justDetected ? 'var(--admin-success)' : 'rgba(255,255,255,0.8)',
            boxShadow: justDetected ? '0 0 0 4px color-mix(in srgb, var(--admin-success) 35%, transparent)' : 'none',
          }}
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
          {message && <p className="max-w-[320px] text-[12px] leading-relaxed text-white/75">{message}</p>}
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
