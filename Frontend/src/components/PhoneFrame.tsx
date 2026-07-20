import type { ReactNode } from 'react'
import clsx from 'clsx'

interface PhoneFrameProps {
  children: ReactNode
  className?: string
  statusBarLight?: boolean
}

export function PhoneFrame({ children, className, statusBarLight = true }: PhoneFrameProps) {
  return (
    <div
      className={clsx('relative aspect-[300/620] w-full select-none', className)}
      style={{ containerType: 'inline-size', containerName: 'phone' }}
    >
      <div
        className="absolute inset-0 rounded-[15.3%] p-[2%]"
        style={{ background: 'linear-gradient(160deg,#43474e,#26282d 34%,#0c0d10 66%,#33363c)' }}
      >
        <div className="relative h-full w-full overflow-hidden rounded-[13.3%] bg-black">
          <div className="absolute inset-0 overflow-hidden rounded-[13.3%]">{children}</div>

          <div className="pointer-events-none absolute left-1/2 top-[1.6%] z-20 h-[3.4%] w-[28%] -translate-x-1/2 rounded-full bg-black" />

          <div
            className={clsx(
              'pointer-events-none absolute inset-x-0 top-0 z-10 flex items-center justify-between px-[9%] pt-[2.4%] font-semibold',
              statusBarLight ? 'text-white' : 'text-black',
            )}
            style={{ fontSize: '5.2cqw' }}
          >
            <span>9:41</span>
            <div className="flex items-center gap-[4%]">
              <SignalGlyph />
              <WifiGlyph />
              <BatteryGlyph />
            </div>
          </div>
        </div>
      </div>

      {/* side buttons — rounded-*-full (proportional radius, not a fixed px
          one that outsizes the button) AND a max(2px, …) floor on the width:
          at the small container sizes these render at (~110-220px), a pure
          1.4% width is under 2 physical px, which the browser rasterizes
          as a blurry, unevenly-antialiased sliver that reads as "crooked"
          even though the geometry itself is correct. */}
      <div className="absolute -left-[1.2%] top-[16%] h-[3.5%] w-[max(2px,1.4%)] rounded-l-full bg-neutral-800" />
      <div className="absolute -left-[1.2%] top-[22%] h-[6.5%] w-[max(2px,1.4%)] rounded-l-full bg-neutral-800" />
      <div className="absolute -left-[1.2%] top-[30%] h-[6.5%] w-[max(2px,1.4%)] rounded-l-full bg-neutral-800" />
      <div className="absolute -right-[1.2%] top-[20%] h-[9%] w-[max(2px,1.4%)] rounded-r-full bg-neutral-800" />
    </div>
  )
}

export function SignalGlyph() {
  return (
    <svg viewBox="0 0 18 12" width="1.1em" height="0.75em" fill="currentColor">
      <rect x="0" y="7" width="3" height="5" rx="0.6" />
      <rect x="5" y="5" width="3" height="7" rx="0.6" />
      <rect x="10" y="3" width="3" height="9" rx="0.6" />
      <rect x="15" y="0" width="3" height="12" rx="0.6" />
    </svg>
  )
}
export function WifiGlyph() {
  return (
    <svg viewBox="0 0 18 14" width="1.1em" height="0.75em" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round">
      <path d="M1 5.5a12 12 0 0 1 16 0" />
      <path d="M4.2 8.7a7.5 7.5 0 0 1 9.6 0" />
      <path d="M7.5 11.8a3 3 0 0 1 3 0" />
    </svg>
  )
}
export function BatteryGlyph() {
  return (
    <svg viewBox="0 0 25 12" width="1.5em" height="0.75em" fill="none" stroke="currentColor" strokeWidth="1">
      <rect x="0.5" y="0.5" width="21" height="11" rx="2.5" />
      <rect x="2" y="2" width="18" height="8" rx="1.5" fill="currentColor" />
      <rect x="22.5" y="4" width="2" height="4" rx="1" fill="currentColor" stroke="none" />
    </svg>
  )
}
