import ScreenShell from './ScreenChrome'

type Store = {
  name: string
  price: string
  dot: string
}

const STORES: Store[] = [
  { name: 'Ашан', price: '12.40', dot: '#34d399' },
  { name: 'Пойтахт', price: '13.90', dot: '#60a5fa' },
  { name: 'Ватан', price: '15.20', dot: '#f472b6' },
]

function StoreCard({ store, highlighted }: { store: Store; highlighted: boolean }) {
  return (
    <div
      className={
        'flex items-center gap-4 rounded-[20px] px-6 py-5 ' +
        (highlighted
          ? 'border-2 border-[#ffb300] bg-[#ffb300]/[0.06] shadow-[0_0_28px_2px_rgba(255,179,0,0.18)]'
          : 'border-2 border-white/[0.07] bg-white/[0.03]')
      }
    >
      <span className="h-3 w-3 shrink-0 rounded-full" style={{ backgroundColor: store.dot }} />
      <span className="flex-1 text-[22px] font-medium leading-none text-white">{store.name}</span>
      <span
        className={
          'font-mono text-[22px] font-semibold leading-none ' +
          (highlighted ? 'text-[#ffb300]' : 'text-white')
        }
      >
        {store.price}
      </span>
    </div>
  )
}

/** Экран 3 — результаты сравнения. */
export default function ScreenResults() {
  return (
    <ScreenShell>
      <div className="flex flex-1 flex-col px-8 pt-12">
        <h2 className="text-[32px] font-semibold leading-none tracking-tight text-white">
          Где дешевле?
        </h2>

        <div className="mt-10 flex flex-col gap-4">
          {STORES.map((store, i) => (
            <StoreCard key={store.name} store={store} highlighted={i === 0} />
          ))}
        </div>

        <div className="flex-1" />

        <button
          type="button"
          className="mb-10 w-full rounded-[22px] bg-[#ffb300] py-5 text-[21px] font-semibold leading-none text-[#0a0a0a]"
        >
          Построить маршрут
        </button>
      </div>
    </ScreenShell>
  )
}
