import ScreenShell from './ScreenChrome'

const RING_RADIUS = 58
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS
const RING_FILL = 0.68

function ProgressRing() {
  return (
    <svg width="132" height="132" viewBox="0 0 132 132" aria-hidden>
      <circle cx="66" cy="66" r={RING_RADIUS} fill="none" stroke="white" strokeOpacity="0.09" strokeWidth="3" />
      <circle
        cx="66"
        cy="66"
        r={RING_RADIUS}
        fill="none"
        stroke="#ffb300"
        strokeWidth="3"
        strokeLinecap="round"
        strokeDasharray={`${RING_CIRCUMFERENCE * RING_FILL} ${RING_CIRCUMFERENCE}`}
        transform="rotate(-90 66 66)"
      />
    </svg>
  )
}

/** Экран 4 — накопленная экономия. */
export default function ScreenSavings() {
  return (
    <ScreenShell>
      <div className="flex flex-1 flex-col items-center justify-center gap-12 px-10">
        <ProgressRing />

        <div className="flex flex-col items-center gap-5">
          <div className="flex items-baseline gap-3">
            <span className="font-mono text-[86px] font-bold leading-none text-[#ffb300]">+38</span>
            <span className="text-[26px] font-medium leading-none text-[#ffb300]">смн</span>
          </div>

          <span className="text-[19px] leading-none text-[#888888]">сэкономлено в этом месяце</span>
        </div>
      </div>
    </ScreenShell>
  )
}
