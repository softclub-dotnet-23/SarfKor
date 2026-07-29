import type { ReactNode } from 'react'

export const AMBER = '#ffb300'
export const MUTED = '#888888'

function SignalIcon() {
  return (
    <svg width="20" height="14" viewBox="0 0 20 14" fill="none" aria-hidden>
      {[0, 1, 2, 3].map((i) => (
        <rect
          key={i}
          x={i * 5}
          y={10 - i * 3}
          width="3.2"
          height={4 + i * 3}
          rx="1"
          fill="white"
          opacity={i === 3 ? 0.35 : 1}
        />
      ))}
    </svg>
  )
}

function WifiIcon() {
  return (
    <svg width="18" height="14" viewBox="0 0 18 14" fill="none" aria-hidden>
      <path d="M1 4.6a12 12 0 0 1 16 0" stroke="white" strokeWidth="1.9" strokeLinecap="round" />
      <path d="M4.2 8a7.4 7.4 0 0 1 9.6 0" stroke="white" strokeWidth="1.9" strokeLinecap="round" />
      <circle cx="9" cy="11.6" r="1.5" fill="white" />
    </svg>
  )
}

function BatteryIcon() {
  return (
    <svg width="27" height="14" viewBox="0 0 27 14" fill="none" aria-hidden>
      <rect x="0.6" y="0.6" width="23" height="12.8" rx="4" stroke="white" strokeOpacity="0.45" strokeWidth="1.2" />
      <rect x="2.4" y="2.4" width="16" height="9.2" rx="2.4" fill="white" />
      <path d="M25.2 5.1v3.8a2.6 2.6 0 0 0 0-3.8Z" fill="white" fillOpacity="0.45" />
    </svg>
  )
}

function StatusBar() {
  return (
    <div className="flex shrink-0 items-center justify-between px-9 pt-5">
      <span className="font-mono text-[19px] font-semibold tracking-tight text-white">9:41</span>
      <div className="flex items-center gap-2">
        <SignalIcon />
        <WifiIcon />
        <BatteryIcon />
      </div>
    </div>
  )
}

function HomeIndicator() {
  return (
    <div className="flex shrink-0 justify-center pb-3 pt-2">
      <div className="h-[5px] w-[140px] rounded-full bg-white/85" />
    </div>
  )
}

/** Единый каркас экрана: фон, статус-бар сверху, home-индикатор снизу. */
export default function ScreenShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex h-full w-full flex-col overflow-hidden bg-[#0a0a0a] text-white">
      <StatusBar />
      <div className="flex min-h-0 flex-1 flex-col">{children}</div>
      <HomeIndicator />
    </div>
  )
}
