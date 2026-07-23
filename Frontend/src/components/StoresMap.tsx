import { useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Eyebrow } from './Eyebrow'
import { CloseIcon, MapPinIcon, StarIcon } from './icons'

interface StorePin {
  id: string
  name: string
  address: string
  rating: number
  distance: string
  top: string
  left: string
}

const STORES: StorePin[] = [
  { id: 'evar', name: 'Ёвар', address: 'ул. Рудаки, 123', rating: 4.8, distance: '320 м', top: '46%', left: '68%' },
  { id: 'ashan', name: 'Ашан', address: 'ул. Айни, 45', rating: 4.5, distance: '650 м', top: '22%', left: '30%' },
  { id: 'aminot', name: 'Аминот', address: 'ул. Сино, 12', rating: 4.6, distance: '910 м', top: '68%', left: '20%' },
  { id: 'orienbank', name: 'Дехот', address: 'ул. Бухоро, 8', rating: 4.3, distance: '1.2 км', top: '30%', left: '80%' },
  { id: 'somon', name: 'Сомон', address: 'ул. Фирдавси, 61', rating: 4.7, distance: '1.4 км', top: '78%', left: '55%' },
]

function MapBackdrop() {
  return (
    <div className="absolute inset-0 overflow-hidden rounded-[28px]">
      <div className="absolute inset-0 bg-[color:var(--bg-section)]" />
      <div
        className="absolute inset-0 opacity-60 dark:opacity-30"
        style={{
          backgroundImage:
            'repeating-linear-gradient(0deg, transparent 0 64px, var(--border-subtle) 64px 65px), repeating-linear-gradient(90deg, transparent 0 84px, var(--border-subtle) 84px 85px)',
        }}
      />
      <div className="absolute left-[8%] top-[12%] h-[22%] w-[26%] rounded-2xl bg-[color:var(--bg-card)]/70" />
      <div className="absolute left-[55%] top-[8%] h-[16%] w-[20%] rounded-2xl bg-[color:var(--bg-card)]/70" />
      <div className="absolute left-[30%] top-[58%] h-[26%] w-[22%] rounded-2xl bg-[color:var(--bg-card)]/70" />
      <div className="absolute left-[68%] top-[62%] h-[18%] w-[24%] rounded-2xl bg-[color:var(--bg-card)]/70" />
    </div>
  )
}

export function StoresMap() {
  const [activeId, setActiveId] = useState<string | null>('evar')
  const active = STORES.find((s) => s.id === activeId)

  return (
    <section id="stores" className="py-24 lg:py-32">
      <div className="mx-auto max-w-7xl px-6 lg:px-10">
        <div className="grid grid-cols-1 items-center gap-12 lg:grid-cols-2 lg:gap-16">
          <div>
            <Eyebrow align="left">Магазины рядом с вами</Eyebrow>
            <motion.h2
              initial={{ opacity: 0, y: 16 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, margin: '-80px' }}
              transition={{ duration: 0.6 }}
              className="text-3xl font-bold leading-tight tracking-tight text-[color:var(--text-primary)] sm:text-4xl"
            >
              Тысячи магазинов
              <br />
              по всему Таджикистану
            </motion.h2>
            <motion.p
              initial={{ opacity: 0, y: 16 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, margin: '-80px' }}
              transition={{ duration: 0.6, delay: 0.1 }}
              className="mt-5 max-w-md text-[17px] text-[color:var(--text-secondary)]"
            >
              Находите лучшие цены рядом с вами. Мы постоянно расширяем сеть партнёров.
            </motion.p>
          </div>

          <motion.div
            initial={{ opacity: 0, scale: 0.96 }}
            whileInView={{ opacity: 1, scale: 1 }}
            viewport={{ once: true, margin: '-60px' }}
            transition={{ duration: 0.6 }}
            className="relative aspect-[4/3.2] w-full overflow-visible rounded-[28px] ring-1 ring-[color:var(--border-subtle)]"
          >
            <MapBackdrop />

            {STORES.map((store) => (
              <button
                key={store.id}
                onClick={() => setActiveId((cur) => (cur === store.id ? null : store.id))}
                style={{ top: store.top, left: store.left }}
                className="absolute z-10 -translate-x-1/2 -translate-y-full"
                aria-label={store.name}
              >
                <span className="relative flex h-9 w-9 items-center justify-center">
                  {store.id === activeId && (
                    <span className="absolute inset-0 animate-pulse-ring rounded-full bg-[color:var(--color-brand)]" />
                  )}
                  <MapPinIcon
                    width={activeId === store.id ? 34 : 28}
                    height={activeId === store.id ? 34 : 28}
                    className={
                      activeId === store.id
                        ? 'text-[color:var(--color-brand)] drop-shadow-lg transition-all'
                        : 'text-neutral-400 transition-all hover:text-[color:var(--color-brand)] dark:text-neutral-500'
                    }
                  />
                </span>
              </button>
            ))}

            <AnimatePresence>
              {active && (
                <motion.div
                  key={active.id}
                  initial={{ opacity: 0, y: 10, scale: 0.95 }}
                  animate={{ opacity: 1, y: 0, scale: 1 }}
                  exit={{ opacity: 0, y: 10, scale: 0.95 }}
                  transition={{ type: 'spring', stiffness: 380, damping: 30 }}
                  style={{
                    top: `calc(${active.top} - 12px)`,
                    left: `min(${active.left}, 62%)`,
                  }}
                  className="absolute z-20 w-56 -translate-x-1/2 -translate-y-full rounded-2xl bg-[color:var(--bg-card)] p-4 shadow-[var(--shadow-lift)] ring-1 ring-[color:var(--border-subtle)]"
                >
                  <button
                    onClick={() => setActiveId(null)}
                    aria-label="Закрыть"
                    className="absolute right-3 top-3 text-[color:var(--text-secondary)] transition-colors hover:text-[color:var(--text-primary)]"
                  >
                    <CloseIcon width={14} height={14} />
                  </button>
                  <p className="pr-4 font-bold text-[color:var(--text-primary)]">{active.name}</p>
                  <p className="mt-1 text-sm text-[color:var(--text-secondary)]">{active.address}</p>
                  <div className="mt-3 flex items-center gap-3 text-sm">
                    <span className="flex items-center gap-1 font-semibold text-[color:var(--text-primary)]">
                      <StarIcon width={14} height={14} className="text-amber-400" />
                      {active.rating}
                    </span>
                    <span className="text-[color:var(--color-brand)]">{active.distance}</span>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          </motion.div>
        </div>
      </div>
    </section>
  )
}
