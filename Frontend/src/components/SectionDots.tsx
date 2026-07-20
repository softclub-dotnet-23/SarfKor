import { motion } from 'framer-motion'
import { useActiveSection } from '../hooks/useActiveSection'

const SECTIONS = ['hero', 'how-it-works', 'stores', 'testimonials', 'faq']

export function SectionDots() {
  const active = useActiveSection(SECTIONS)
  const activeIndex = Math.max(SECTIONS.indexOf(active), 0)

  return (
    <div className="fixed right-6 top-1/2 z-40 hidden -translate-y-1/2 flex-col items-center gap-4 xl:flex">
      <span className="text-xs font-semibold tabular-nums text-[color:var(--color-brand)]">
        {String(activeIndex + 1).padStart(2, '0')}
      </span>
      <div className="flex flex-col items-center gap-3">
        {SECTIONS.map((id, i) => (
          <button
            key={id}
            aria-label={`Перейти к разделу ${i + 1}`}
            onClick={() => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' })}
            className="group relative flex h-3 w-3 items-center justify-center"
          >
            <span
              className={
                i === activeIndex
                  ? 'h-2.5 w-2.5 rounded-full bg-[color:var(--color-brand)]'
                  : 'h-1.5 w-1.5 rounded-full bg-[color:var(--text-secondary)]/40 transition-all group-hover:bg-[color:var(--text-secondary)]/70'
              }
            />
            {i === activeIndex && (
              <motion.span
                layoutId="section-dot-ring"
                className="absolute inset-[-4px] rounded-full ring-2 ring-[color:var(--color-brand)]/30"
                transition={{ type: 'spring', stiffness: 400, damping: 30 }}
              />
            )}
          </button>
        ))}
      </div>
    </div>
  )
}
