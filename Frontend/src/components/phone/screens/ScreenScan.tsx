import ScreenShell from './ScreenChrome'

const FRAME_SIZE = 262

// Ширины штрихов — фиксированный набор, чтобы штрихкод не «дрожал» между рендерами.
const BARS = [3, 1, 2, 4, 1, 1, 3, 2, 5, 1, 2, 1, 4, 3, 1, 2, 2, 5, 1, 3, 1, 1, 4, 2, 3, 1, 2, 4]

function Barcode() {
  return (
    <div className="flex h-[104px] items-stretch gap-[3px]">
      {BARS.map((width, i) => (
        <div key={i} style={{ width: width * 2.4 }} className="rounded-[1px] bg-white/85" />
      ))}
    </div>
  )
}

function Corner({ className }: { className: string }) {
  return <div className={`absolute h-11 w-11 border-[#ffb300] ${className}`} />
}

/** Экран 2 — сканирование штрихкода. */
export default function ScreenScan() {
  return (
    <ScreenShell>
      {/* Лёгкая засветка под «объектив камеры» */}
      <div className="relative flex flex-1 flex-col items-center justify-center gap-12 px-10">
        <div
          className="pointer-events-none absolute inset-0"
          style={{
            background:
              'radial-gradient(circle at 50% 42%, rgba(255,255,255,0.07) 0%, rgba(255,255,255,0) 62%)',
          }}
        />

        <div className="relative" style={{ width: FRAME_SIZE, height: FRAME_SIZE }}>
          <Corner className="left-0 top-0 rounded-tl-[18px] border-l-[5px] border-t-[5px]" />
          <Corner className="right-0 top-0 rounded-tr-[18px] border-r-[5px] border-t-[5px]" />
          <Corner className="bottom-0 left-0 rounded-bl-[18px] border-b-[5px] border-l-[5px]" />
          <Corner className="bottom-0 right-0 rounded-br-[18px] border-b-[5px] border-r-[5px]" />

          <div className="flex h-full w-full items-center justify-center">
            <Barcode />
          </div>

          {/* Луч сканера: бежит сверху вниз циклически */}
          <div
            className="scanner-beam absolute inset-x-5 top-0 h-[3px] rounded-full bg-[#ffb300]"
            style={{
              boxShadow: '0 0 18px 4px rgba(255,179,0,0.75)',
              ['--scan-distance' as string]: `${FRAME_SIZE - 3}px`,
            }}
          />
        </div>

        <span className="relative text-[21px] font-medium leading-none text-white">
          Наведите на штрихкод
        </span>
      </div>
    </ScreenShell>
  )
}
