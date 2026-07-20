import { useState } from 'react'
import { motion } from 'framer-motion'
import { Hero3DPhone } from './hero/Hero3DPhone'
import { Modal } from './Modal'
import { PlayIcon, SwipeIcon } from './icons'

const AVATAR_COLORS = ['#FF9F6B', '#6BCB9E', '#6BA4FF', '#FFC96B']

function AvatarStack() {
  return (
    <div className="flex -space-x-3">
      {AVATAR_COLORS.map((color, i) => (
        <span
          key={i}
          className="h-9 w-9 rounded-full ring-2 ring-[color:var(--bg-app)]"
          style={{ background: `linear-gradient(135deg, ${color}, ${color}cc)` }}
        />
      ))}
    </div>
  )
}

function PhoneStage() {
  const [interacted, setInteracted] = useState(false)

  return (
    <div className="relative mx-auto w-full">
      <motion.div
        initial={{ opacity: 0, y: 40, scale: 0.92 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={{ duration: 0.9, ease: [0.16, 1, 0.3, 1] }}
      >
        <Hero3DPhone onInteract={() => setInteracted(true)} />
      </motion.div>

      <motion.div
        animate={{ opacity: interacted ? 0 : 1 }}
        transition={{ duration: 0.6 }}
        className="pointer-events-none absolute bottom-2 right-2 flex items-center gap-2 rounded-full bg-[color:var(--bg-card)] px-4 py-2 text-xs font-semibold text-[color:var(--text-secondary)] shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)] sm:bottom-6 sm:right-6"
      >
        <SwipeIcon width={16} height={16} />
        Крути телефон · свайпай экран
      </motion.div>
    </div>
  )
}

export function Hero() {
  const [showComingSoon, setShowComingSoon] = useState(false)

  return (
    <section id="hero" className="relative overflow-hidden pb-20 pt-32 lg:pb-32 lg:pt-40">
      <div className="mx-auto grid max-w-7xl grid-cols-1 items-center gap-16 px-6 lg:grid-cols-2 lg:gap-8 lg:px-10">
        <div>
          <motion.h1
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, ease: [0.16, 1, 0.3, 1] }}
            className="text-[44px] font-bold leading-[1.05] tracking-tight sm:text-[56px] lg:text-[64px]"
          >
            <span className="block text-[color:var(--text-primary)]">Сканируй.</span>
            <span className="block text-[color:var(--text-primary)]">Сравнивай.</span>
            <span className="block text-[color:var(--color-brand)]">Экономь.</span>
          </motion.h1>

          <motion.p
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.15, ease: [0.16, 1, 0.3, 1] }}
            className="mt-6 max-w-md text-lg text-[color:var(--text-secondary)]"
          >
            Узнай цену товара во всех ближайших магазинах за несколько секунд.
          </motion.p>

          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.3, ease: [0.16, 1, 0.3, 1] }}
            className="mt-9 flex flex-wrap items-center gap-4"
          >
            <button
              onClick={() => setShowComingSoon(true)}
              className="rounded-full bg-[color:var(--bg-inverse)] px-7 py-3.5 text-[15px] font-semibold text-[color:var(--bg-app)] shadow-lg shadow-black/10 transition-transform duration-200 hover:scale-[1.03] active:scale-[0.97]"
            >
              Попробовать бесплатно
            </button>
            <button
              onClick={() =>
                document.getElementById('how-it-works')?.scrollIntoView({ behavior: 'smooth' })
              }
              className="group flex items-center gap-2 rounded-full px-6 py-3.5 text-[15px] font-semibold text-[color:var(--text-primary)] ring-1 ring-inset ring-[color:var(--border-subtle)] transition-colors hover:bg-[color:var(--bg-section)]"
            >
              <span className="grid h-6 w-6 place-items-center rounded-full bg-[color:var(--color-brand)] text-white transition-transform group-hover:scale-110">
                <PlayIcon width={11} height={11} />
              </span>
              Посмотреть как работает
            </button>
          </motion.div>

          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.7, delay: 0.5 }}
            className="mt-10 flex items-center gap-3"
          >
            <AvatarStack />
            <span className="text-sm text-[color:var(--text-secondary)]">
              <strong className="text-[color:var(--text-primary)]">50 000+</strong> пользователей уже
              экономят
            </span>
          </motion.div>
        </div>

        <PhoneStage />
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
            document.getElementById('how-it-works')?.scrollIntoView({ behavior: 'smooth' })
          }}
          className="mt-6 w-full rounded-full bg-[color:var(--color-brand)] py-3 text-[15px] font-semibold text-white transition-transform hover:scale-[1.02] active:scale-[0.98]"
        >
          Посмотреть как это работает
        </button>
      </Modal>
    </section>
  )
}
