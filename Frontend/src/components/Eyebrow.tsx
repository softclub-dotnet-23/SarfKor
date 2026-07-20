import { motion } from 'framer-motion'

export function Eyebrow({ children, align = 'center' }: { children: string; align?: 'center' | 'left' }) {
  return (
    <motion.p
      initial={{ opacity: 0, y: 10 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: '-80px' }}
      transition={{ duration: 0.5 }}
      className={`mb-3 text-[13px] font-bold uppercase tracking-[0.12em] text-[color:var(--color-brand)] ${
        align === 'center' ? 'text-center' : 'text-left'
      }`}
    >
      {children}
    </motion.p>
  )
}
