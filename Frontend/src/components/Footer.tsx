import { useState } from 'react'
import { motion } from 'framer-motion'
import { LogoMark } from './Logo'
import { Modal } from './Modal'

const FOOTER_LINKS = [
  { id: 'how-it-works', label: 'Возможности' },
  { id: 'stores', label: 'Магазины' },
  { id: 'faq', label: 'Поддержка' },
]

function scrollToSection(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

function BreakoutDevice() {
  return (
    <div
      className="pointer-events-none absolute left-1/2 top-0 z-[4]"
      style={{ transform: 'translate(-50%, -58%)', transformStyle: 'preserve-3d' }}
    >
      <div style={{ transform: 'rotateX(24deg) rotateZ(-8deg)', transformStyle: 'preserve-3d' }}>
        <div
          className="relative grid h-[110px] w-[110px] place-items-center rounded-[30px] sm:h-[150px] sm:w-[150px] sm:rounded-[38px]"
          style={{
            background: 'linear-gradient(150deg,#3b82f6,#1d4ed8)',
            boxShadow: '0 44px 70px -18px rgba(37,99,235,.6), inset 0 2px 6px rgba(255,255,255,.35)',
          }}
        >
          <LogoMark size={56} />
          <div
            className="absolute left-[10%] w-[88%]"
            style={{
              bottom: -22,
              height: 20,
              borderRadius: '50%',
              background: 'radial-gradient(ellipse, rgba(5,7,13,.4), transparent 70%)',
              filter: 'blur(7px)',
              transform: 'translateZ(-40px)',
            }}
          />
        </div>
      </div>
    </div>
  )
}

export function Footer() {
  const [showComingSoon, setShowComingSoon] = useState(false)

  return (
    <footer
      className="relative mt-[clamp(90px,14vh,180px)] overflow-visible text-white"
      style={{ background: 'linear-gradient(135deg,#0e1424,#0b1020)', perspective: 1400 }}
    >
      <BreakoutDevice />

      <div className="mx-auto max-w-7xl px-6 pb-11 pt-[clamp(78px,10vw,130px)] text-center lg:px-10">
        <motion.h2
          initial={{ opacity: 0, y: 16 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: '-80px' }}
          transition={{ duration: 0.6 }}
          className="text-[clamp(30px,4.5vw,58px)] font-extrabold leading-[1.02] tracking-tight"
        >
          Начни экономить
          <br />
          уже сегодня
        </motion.h2>
        <p className="mt-[18px] text-[17px] font-medium text-[#9aa6c5]">
          Скачай Sarfkor и находи лучшие цены за секунды.
        </p>

        <div className="mt-8 flex flex-wrap justify-center gap-3.5">
          <button
            onClick={() => setShowComingSoon(true)}
            className="rounded-2xl bg-white px-7 py-4 text-[15px] font-extrabold text-[#0b1020] transition-transform hover:scale-[1.03] active:scale-[0.97]"
          >
            Попробовать бесплатно
          </button>
          <button
            onClick={() => scrollToSection('faq')}
            className="rounded-2xl border border-white/20 bg-transparent px-7 py-4 text-[15px] font-extrabold text-white transition-transform hover:scale-[1.03] active:scale-[0.97]"
          >
            Стать партнёром
          </button>
        </div>

        <div className="mt-14 flex flex-wrap items-center justify-between gap-4 border-t border-white/10 pt-7 text-sm font-semibold text-[#7f8db3]">
          <div className="flex items-center gap-2.5 text-lg font-extrabold text-white">
            <LogoMark size={26} />
            Sarfkor
          </div>
          <div className="flex gap-6">
            {FOOTER_LINKS.map((link) => (
              <button
                key={link.id}
                onClick={() => scrollToSection(link.id)}
                className="text-[#9aa6c5] transition-colors hover:text-white"
              >
                {link.label}
              </button>
            ))}
          </div>
          <div>© 2026 Sarfkor. Таджикистан</div>
        </div>
      </div>

      <Modal open={showComingSoon} onClose={() => setShowComingSoon(false)}>
        <h3 className="text-xl font-bold text-[color:var(--text-primary)]">Sarfkor уже рядом</h3>
        <p className="mt-2 text-[15px] text-[color:var(--text-secondary)]">
          Приложение скоро появится в App Store и Google Play. Оставайтесь на странице — мы покажем,
          как всё работает, прямо сейчас.
        </p>
        <button
          onClick={() => {
            setShowComingSoon(false)
            scrollToSection('how-it-works')
          }}
          className="mt-6 w-full rounded-full bg-[color:var(--color-brand)] py-3 text-[15px] font-semibold text-white transition-transform hover:scale-[1.02] active:scale-[0.98]"
        >
          Посмотреть как это работает
        </button>
      </Modal>
    </footer>
  )
}
