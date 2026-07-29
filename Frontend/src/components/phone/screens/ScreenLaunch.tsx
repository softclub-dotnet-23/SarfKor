import ScreenShell from './ScreenChrome'

/** Экран 1 — запуск. */
export default function ScreenLaunch() {
  return (
    <ScreenShell>
      <div className="flex flex-1 flex-col items-center justify-center gap-10 px-10">
        <div className="flex h-36 w-36 items-center justify-center rounded-[38px] bg-[#ffb300] shadow-[0_0_70px_18px_rgba(255,179,0,0.28)]">
          <span className="text-[86px] font-bold leading-none text-white">S</span>
        </div>

        <div className="flex flex-col items-center gap-4">
          <span className="text-[42px] font-semibold leading-none tracking-tight text-white">Sarfkor</span>
          <span className="text-[19px] leading-none text-[#888888]">Цены в магазинах рядом</span>
        </div>
      </div>
    </ScreenShell>
  )
}
