import { useT } from '../../i18n/translations'

// Replaces the plain-text "Загружаем…" repeated verbatim across Dashboard/
// Reports/Inventory/Staff with a real spinner.
function Spinner() {
  return <span aria-hidden className="h-4 w-4 shrink-0 animate-spin rounded-full border-2 border-[color:var(--admin-border)] border-t-[color:var(--admin-accent)]" />
}

export function Loading({ label }: { scheme?: 'admin'; label?: string }) {
  const t = useT()
  return (
    <div className="flex items-center justify-center gap-2.5 py-16 text-[13px] font-medium text-[color:var(--admin-text-tertiary)]">
      <Spinner />
      {label ?? t('common.loading')}
    </div>
  )
}
