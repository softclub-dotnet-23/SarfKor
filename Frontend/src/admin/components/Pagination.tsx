import { ChevronDownIcon } from './icons'

// Shared server-side pagination control for the platform Admin console (all list endpoints there
// take skip/take and return totalCount) -- one control instead of six hand-rolled prev/next pairs.
export function Pagination({
  skip,
  take,
  totalCount,
  onChange,
}: {
  skip: number
  take: number
  totalCount: number
  onChange: (skip: number) => void
}) {
  if (totalCount === 0) return null
  const from = totalCount === 0 ? 0 : skip + 1
  const to = Math.min(skip + take, totalCount)
  const canPrev = skip > 0
  const canNext = to < totalCount

  return (
    <div className="mt-4 flex items-center justify-between gap-3 border-t border-[color:var(--admin-border)] pt-4">
      <span className="font-[JetBrains_Mono,monospace] text-[11.5px] text-[color:var(--admin-text-tertiary)]">
        {from}–{to} из {totalCount}
      </span>
      <div className="flex gap-2">
        <button
          onClick={() => onChange(Math.max(0, skip - take))}
          disabled={!canPrev}
          className="flex h-8 items-center gap-1 rounded-lg border border-[color:var(--admin-border)] px-3 text-[12px] font-semibold text-[color:var(--admin-text)] transition-colors hover:bg-[color:var(--admin-hover)] disabled:opacity-35"
        >
          <ChevronDownIcon width={12} height={12} className="rotate-90" />
          Назад
        </button>
        <button
          onClick={() => onChange(skip + take)}
          disabled={!canNext}
          className="flex h-8 items-center gap-1 rounded-lg border border-[color:var(--admin-border)] px-3 text-[12px] font-semibold text-[color:var(--admin-text)] transition-colors hover:bg-[color:var(--admin-hover)] disabled:opacity-35"
        >
          Далее
          <ChevronDownIcon width={12} height={12} className="-rotate-90" />
        </button>
      </div>
    </div>
  )
}
