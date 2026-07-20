import { motion } from 'framer-motion'
import { useCountUp } from '../hooks/useCountUp'
import { ReceiptIcon, ScanIcon, ShieldIcon, UsersIcon } from './icons'
import type { IconProps } from './icons'

interface Stat {
  icon: (props: IconProps) => React.ReactNode
  value: number
  suffix: string
  decimals?: number
  label: string
}

const STATS: Stat[] = [
  { icon: UsersIcon, value: 50000, suffix: '+', label: 'Активных пользователей' },
  { icon: ScanIcon, value: 1.2, suffix: 'M+', decimals: 1, label: 'Сканирований товаров' },
  { icon: ReceiptIcon, value: 18.4, suffix: 'M', decimals: 1, label: 'Сомони сэкономлено' },
  { icon: ShieldIcon, value: 2500, suffix: '+', label: 'Подключенных магазинов' },
]

function StatCard({ stat, index }: { stat: Stat; index: number }) {
  const { ref, value } = useCountUp(stat.decimals ? stat.value * 10 : stat.value)
  const displayValue = stat.decimals ? (value / 10).toFixed(stat.decimals) : value.toLocaleString('ru-RU')

  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: 24 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: '-60px' }}
      transition={{ duration: 0.5, delay: index * 0.1 }}
      className="flex items-center gap-4"
    >
      <span className="grid h-12 w-12 shrink-0 place-items-center rounded-2xl bg-white/10 text-[color:var(--color-brand)]">
        <stat.icon width={22} height={22} className="text-[#5b9bff]" />
      </span>
      <div>
        <p className="text-2xl font-extrabold tabular-nums text-white sm:text-3xl">
          {displayValue}
          {stat.suffix}
        </p>
        <p className="mt-0.5 text-sm text-white/50">{stat.label}</p>
      </div>
    </motion.div>
  )
}

export function StatsBar() {
  return (
    <div className="mx-auto max-w-7xl px-6 lg:px-10">
      <div className="grid grid-cols-1 gap-8 rounded-[28px] bg-[#0b0f19] px-8 py-10 sm:grid-cols-2 lg:grid-cols-4 lg:px-12">
        {STATS.map((stat, i) => (
          <StatCard key={stat.label} stat={stat} index={i} />
        ))}
      </div>
    </div>
  )
}
