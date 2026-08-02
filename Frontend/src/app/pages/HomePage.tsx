import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { productsApi } from '../../lib/api'
import {
  CountUp,
  EmptyState,
  LINE,
  MaskReveal,
  Reveal,
  SectionTitle,
  Skeleton,
  TXT,
  useAsync,
} from '../ui'

/** The landing's own scan glyph, reused so the two halves share one mark. */
export function BarcodeGlyph({ size = 24 }: { size?: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <path d="M4 8V5.6A1.6 1.6 0 015.6 4H8" />
      <path d="M16 4h2.4A1.6 1.6 0 0120 5.6V8" />
      <path d="M20 16v2.4a1.6 1.6 0 01-1.6 1.6H16" />
      <path d="M8 20H5.6A1.6 1.6 0 014 18.4V16" />
      <path d="M7.5 8.5v7M11 8.5v7M14.5 8.5v7M17 8.5v7" />
    </svg>
  )
}

export function HomePage() {
  const navigate = useNavigate()
  const [barcode, setBarcode] = useState('')

  // Real endpoint. Note it returns productId + name only: with no product-by-id
  // route on the backend there is nothing to navigate to, so this stays a
  // read-only pulse of what the platform is being asked about rather than a
  // list of links that would 404.
  const popular = useAsync(() => productsApi.getMostScannedProducts(6), [])

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const code = barcode.trim()
    if (code) navigate(`/app/p/${encodeURIComponent(code)}`)
  }

  return (
    <>
      <header className="mb-14">
        <Reveal>
          <p
            className="mb-6 text-[10px] font-bold uppercase tracking-[0.28em]"
            style={{ color: TXT.rest }}
          >
            Sarfkor
          </p>
        </Reveal>
        <h1 className="text-[clamp(38px,6vw,64px)] font-extrabold leading-[1.02] tracking-[-0.04em]">
          <MaskReveal delay={0.1}>Найдите</MaskReveal>
          <MaskReveal delay={0.22}>дешевле.</MaskReveal>
        </h1>
        <Reveal i={4}>
          <p
            className="mt-7 max-w-[400px] text-[15px] leading-relaxed"
            style={{ color: TXT.secondary }}
          >
            Наведите камеру на штрихкод — Sarfkor покажет цену этого товара во всех
            магазинах рядом.
          </p>
        </Reveal>
      </header>

      {/* ── PRIMARY ACTION ───────────────────────────────────── */}
      <Reveal i={5}>
        <button
          onClick={() => navigate('/app/scan')}
          className="group relative mb-4 flex w-full items-center gap-6 rounded-2xl border p-7 text-left transition-all duration-700 hover:bg-[color:var(--app-line-soft)] sm:p-9"
          style={{ borderColor: LINE, transitionTimingFunction: 'cubic-bezier(0.16,1,0.3,1)' }}
        >
          <span className="grid h-14 w-14 shrink-0 place-items-center rounded-2xl bg-[color:var(--app-text-primary)] text-[color:var(--bg-app)] transition-transform duration-700 group-hover:scale-[1.04]">
            <BarcodeGlyph size={26} />
          </span>
          <span className="min-w-0">
            <span className="block text-[19px] font-bold tracking-tight text-[color:var(--app-text-primary)]">
              Сканировать штрихкод
            </span>
            <span className="mt-1 block text-[13.5px]" style={{ color: TXT.rest }}>
              Камера найдёт товар за секунду
            </span>
          </span>
          <span
            aria-hidden
            className="ml-auto hidden text-[20px] transition-transform duration-700 group-hover:translate-x-1.5 sm:block"
            style={{ color: TXT.rest, transitionTimingFunction: 'cubic-bezier(0.16,1,0.3,1)' }}
          >
            →
          </span>
        </button>
      </Reveal>

      {/* ── MANUAL ENTRY ─────────────────────────────────────── */}
      <Reveal i={6}>
        <form onSubmit={handleSubmit} className="mb-16 flex items-end gap-4">
          <label className="relative flex-1">
            <span
              className="mb-2 block text-[10px] font-bold uppercase tracking-[0.16em]"
              style={{ color: TXT.rest }}
            >
              Или введите код вручную
            </span>
            <input
              value={barcode}
              onChange={(e) => setBarcode(e.target.value.replace(/[^\d]/g, ''))}
              inputMode="numeric"
              autoComplete="off"
              placeholder="4780016470012"
              className="w-full border-0 bg-transparent pb-3 text-[16px] tracking-[0.04em] text-[color:var(--app-text-primary)] caret-[color:var(--app-text-primary)] placeholder:text-[color:var(--app-text-rest)]"
              style={{ borderBottom: `1px solid ${LINE}` }}
            />
          </label>
          <button
            type="submit"
            disabled={!barcode.trim()}
            className="shrink-0 rounded-full bg-[color:var(--app-text-primary)] px-6 py-3 text-[13.5px] font-bold text-[color:var(--bg-app)] transition-all duration-500 hover:-translate-y-0.5 disabled:pointer-events-none disabled:opacity-30"
            style={{ transitionTimingFunction: 'cubic-bezier(0.16,1,0.3,1)' }}
          >
            Найти
          </button>
        </form>
      </Reveal>

      {/* ── POPULAR ──────────────────────────────────────────── */}
      <Reveal i={7}>
        <SectionTitle>Чаще всего ищут</SectionTitle>

        {popular.loading && (
          <ul className="flex flex-col">
            {Array.from({ length: 4 }).map((_, i) => (
              <li
                key={i}
                className="flex items-center justify-between gap-6 border-b py-4"
                style={{ borderColor: LINE }}
              >
                <Skeleton h={14} w={`${45 + ((i * 13) % 30)}%`} />
                <Skeleton h={12} w={44} />
              </li>
            ))}
          </ul>
        )}

        {/* A failure here must not shout — it is a secondary panel on a page whose
            primary action is unaffected, so it degrades to silence. */}
        {!popular.loading && !popular.error && popular.data && (
          popular.data.products.length === 0 ? (
            <EmptyState
              title="Пока нет данных"
              body="Как только покупатели начнут сканировать товары, здесь появится самое популярное."
            />
          ) : (
            <ul className="flex flex-col">
              {popular.data.products.map((p, i) => (
                <li
                  key={p.productId}
                  className="flex items-baseline justify-between gap-6 border-b py-4"
                  style={{ borderColor: LINE }}
                >
                  <span className="flex min-w-0 items-baseline gap-4">
                    <span
                      className="w-[18px] shrink-0 text-[10px] font-semibold tabular-nums"
                      style={{ color: 'var(--app-line)' }}
                    >
                      {String(i + 1).padStart(2, '0')}
                    </span>
                    <span className="truncate text-[14.5px] font-medium text-[color:var(--app-text-primary)]">
                      {p.productName}
                    </span>
                  </span>
                  <span
                    className="shrink-0 text-[12px] tabular-nums"
                    style={{ color: TXT.rest }}
                  >
                    <CountUp to={p.totalScans} /> ск.
                  </span>
                </li>
              ))}
            </ul>
          )
        )}
      </Reveal>
    </>
  )
}
