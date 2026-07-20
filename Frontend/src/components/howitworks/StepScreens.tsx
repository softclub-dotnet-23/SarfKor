import { motion } from 'framer-motion'
import { BarcodeIcon, RouteIcon } from '../icons'

export function ScanScreen() {
  return (
    <div className="relative flex h-full w-full items-center justify-center bg-gradient-to-b from-[#20242e] to-[#0a0b10]">
      <motion.div
        initial={{ opacity: 0.3 }}
        animate={{ opacity: [0.3, 1, 0.3] }}
        transition={{ duration: 2.2, repeat: Infinity, ease: 'easeInOut' }}
        className="relative flex h-[38%] w-[70%] items-center justify-center rounded-2xl border-2 border-white/70"
      >
        <BarcodeIcon width="60%" height="60%" className="text-white/90" />
        <motion.div
          initial={{ top: '8%' }}
          animate={{ top: ['8%', '88%', '8%'] }}
          transition={{ duration: 2.2, repeat: Infinity, ease: 'easeInOut' }}
          className="absolute inset-x-[6%] h-[2px] bg-[color:var(--color-brand)] shadow-[0_0_8px_2px_var(--color-brand)]"
        />
        <span className="absolute -left-[2px] -top-[2px] h-4 w-4 rounded-tl-xl border-l-4 border-t-4 border-white" />
        <span className="absolute -right-[2px] -top-[2px] h-4 w-4 rounded-tr-xl border-r-4 border-t-4 border-white" />
        <span className="absolute -bottom-[2px] -left-[2px] h-4 w-4 rounded-bl-xl border-b-4 border-l-4 border-white" />
        <span className="absolute -bottom-[2px] -right-[2px] h-4 w-4 rounded-br-xl border-b-4 border-r-4 border-white" />
      </motion.div>
    </div>
  )
}

export function ProductInfoScreen() {
  return (
    <div className="flex h-full w-full flex-col bg-[#f3f4f7] px-[7%] pt-[18%]">
      <div className="mb-[6%] text-center font-semibold text-neutral-500" style={{ fontSize: '3.4cqw' }}>
        Информация о товаре
      </div>
      <div className="flex flex-1 flex-col items-center justify-center gap-[6%] rounded-2xl bg-white p-[6%] shadow-sm">
        <div className="h-[26%] w-[46%] rounded-xl bg-gradient-to-br from-sky-100 to-blue-200" />
        <div className="w-full text-center">
          <p className="font-bold text-neutral-800" style={{ fontSize: '4.2cqw' }}>Молоко Домик</p>
          <p className="mt-[2%] text-neutral-400" style={{ fontSize: '3.2cqw' }}>1 литр</p>
        </div>
        <div className="w-full rounded-xl bg-emerald-50 px-[4%] py-[4%] text-center">
          <p className="font-medium text-emerald-700" style={{ fontSize: '3.2cqw' }}>в деревне 2.5% 1л</p>
        </div>
      </div>
    </div>
  )
}

export function CompareScreen() {
  const stores = [
    { name: 'Ашан', price: '27.00 сомони', good: false },
    { name: 'Ёвар', price: '25.90 сомони', good: true },
    { name: 'Аминот', price: '28.20 сомони', good: false },
  ]
  return (
    <div className="flex h-full w-full flex-col bg-white px-[6%] pt-[18%]">
      <p className="mb-[5%] font-bold text-neutral-800" style={{ fontSize: '4.4cqw' }}>
        Где дешевле?
      </p>
      <div className="flex flex-col gap-[4%]">
        {stores.map((store) => (
          <div
            key={store.name}
            className={`flex items-center justify-between rounded-xl px-[5%] py-[4%] ${
              store.good ? 'bg-emerald-50 ring-1 ring-emerald-300' : 'bg-neutral-100'
            }`}
          >
            <div className="flex items-center gap-[4%]">
              <span
                className="grid place-items-center rounded-full bg-white"
                style={{ width: '9cqw', height: '9cqw', fontSize: '4cqw' }}
              >
                🏬
              </span>
              <div>
                <p className="font-semibold text-neutral-800" style={{ fontSize: '3.4cqw' }}>{store.name}</p>
                {store.good && (
                  <p className="font-medium text-emerald-600" style={{ fontSize: '2.8cqw' }}>Выгодно</p>
                )}
              </div>
            </div>
            <p className="font-bold text-neutral-800" style={{ fontSize: '3.4cqw' }}>{store.price}</p>
          </div>
        ))}
      </div>
    </div>
  )
}

export function SavingsScreen() {
  return (
    <div className="flex h-full w-full flex-col items-center justify-center gap-[5%] bg-gradient-to-b from-white to-emerald-50 px-[8%] text-center">
      <motion.div
        initial={{ scale: 0 }}
        animate={{ scale: 1 }}
        transition={{ type: 'spring', stiffness: 260, damping: 18, delay: 0.15 }}
        className="grid h-[16%] w-[30%] place-items-center rounded-full bg-emerald-500 text-white"
      >
        <svg viewBox="0 0 24 24" width="55%" height="55%" fill="none" stroke="currentColor" strokeWidth={2.5} strokeLinecap="round" strokeLinejoin="round">
          <path d="m5 13 4 4L19 7" />
        </svg>
      </motion.div>
      <p className="font-medium text-neutral-500" style={{ fontSize: '3.4cqw' }}>Вы сэкономили</p>
      <p className="font-extrabold text-emerald-600" style={{ fontSize: '7.5cqw' }}>3.10 сомони</p>
      <p className="text-neutral-500" style={{ fontSize: '3cqw' }}>
        Выбрали самую низкую цену в магазине «Ёвар»
      </p>
      <button
        className="mt-[3%] flex items-center gap-[2%] rounded-full bg-[color:var(--color-brand)] px-[6%] py-[3%] font-semibold text-white"
        style={{ fontSize: '3.2cqw' }}
      >
        <RouteIcon width="5.5cqw" height="5.5cqw" />
        Построить маршрут
      </button>
    </div>
  )
}
