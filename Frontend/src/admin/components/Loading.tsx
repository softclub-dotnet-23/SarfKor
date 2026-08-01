// Replaces the plain-text "Загружаем…" repeated verbatim across Dashboard/
// Reports/Inventory/Staff with a real spinner, matching ModerationPage's
// already-better Loading state.
function Spinner({ scheme }: { scheme: 'admin' | 'mod' }) {
  const border =
    scheme === 'admin'
      ? 'border-[color:var(--admin-border)] border-t-[color:var(--admin-accent)]'
      : 'border-[color:var(--mod-border)] border-t-[color:var(--mod-accent)]'
  return <span aria-hidden className={`h-4 w-4 shrink-0 animate-spin rounded-full border-2 ${border}`} />
}

export function Loading({ scheme = 'admin', label = 'Загрузка…' }: { scheme?: 'admin' | 'mod'; label?: string }) {
  const color = scheme === 'admin' ? 'text-[color:var(--admin-text-tertiary)]' : 'text-[color:var(--mod-faint)]'
  return (
    <div className={`flex items-center justify-center gap-2.5 py-16 text-[13px] font-medium ${color}`}>
      <Spinner scheme={scheme} />
      {label}
    </div>
  )
}
