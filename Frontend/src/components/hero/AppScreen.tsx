import { SearchIcon } from '../icons'

interface StoreRow {
  name: string
  price: string
  distance: string
  best?: boolean
  color: string
}

const STORES: StoreRow[] = [
  { name: 'Ёвар', price: '7.90', distance: 'лучшая цена · 320м', best: true, color: 'linear-gradient(135deg,#16a34a,#15803d)' },
  { name: 'Ашан', price: '8.40', distance: '1.2 км', color: 'linear-gradient(135deg,#f97316,#ea580c)' },
  { name: 'Амонат', price: '9.20', distance: '2.0 км', color: 'linear-gradient(135deg,#3b82f6,#1d4ed8)' },
]

export function AppScreen() {
  return (
    <div className="flex h-full w-full flex-col gap-[5%] bg-gradient-to-b from-[#f8fafc] to-[#eef2ff] px-[7%] pb-[6%] pt-[20%]">
      <div className="flex items-center justify-between">
        <span className="font-extrabold tracking-tight text-[#0b0f19]" style={{ fontSize: '7.5cqw' }}>
          Где дешевле?
        </span>
        <span
          className="h-[12%] w-[12%] shrink-0 rounded-full"
          style={{ background: 'linear-gradient(135deg,#93c5fd,#2563eb)' }}
        />
      </div>

      <div className="flex items-center gap-[3%] rounded-xl border border-[#e7e9ee] bg-white px-[4%] py-[3.5%] font-semibold text-[#8a93a3]" style={{ fontSize: '3.2cqw' }}>
        <SearchIcon width="4.5cqw" height="4.5cqw" />
        Молоко «Домик в деревне»
      </div>

      <div className="flex flex-col gap-[1%] rounded-2xl border border-[#e7e9ee] bg-white p-[1.5%]">
        {STORES.map((store) => (
          <div
            key={store.name}
            className="flex items-center gap-[3%] rounded-xl px-[3%] py-[3.5%]"
            style={{ background: store.best ? '#ecfdf3' : 'transparent' }}
          >
            <span className="h-[9%] w-[9%] shrink-0 rounded-lg" style={{ background: store.color }} />
            <div className="min-w-0 flex-1">
              <div className="truncate font-extrabold text-[#0b0f19]" style={{ fontSize: '3.4cqw' }}>
                {store.name}
              </div>
              <div
                className="truncate font-bold"
                style={{ fontSize: '2.7cqw', color: store.best ? '#12b76a' : '#8a93a3' }}
              >
                {store.distance}
              </div>
            </div>
            <div
              className="shrink-0 font-extrabold"
              style={{ fontSize: '3.6cqw', color: store.best ? '#12b76a' : '#0b0f19' }}
            >
              {store.price}
            </div>
          </div>
        ))}
      </div>

      <button
        className="mt-auto rounded-2xl py-[4%] text-center font-extrabold text-white shadow-lg"
        style={{ fontSize: '3.4cqw', background: 'linear-gradient(135deg,#3b82f6,#2563eb)', boxShadow: '0 10px 24px rgba(37,99,235,.35)' }}
      >
        Построить маршрут
      </button>
    </div>
  )
}
