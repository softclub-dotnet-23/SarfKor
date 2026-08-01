// Promoted from ModerationPage's local ErrorState, replacing the bespoke
// error Card + "Повторить" button each page previously hand-rolled.
const SCHEMES = {
  admin: {
    text: 'text-[color:var(--admin-text-secondary)]',
    btn: 'bg-[color:var(--admin-accent)]',
  },
  mod: {
    text: 'text-[color:var(--mod-muted)]',
    btn: 'bg-[color:var(--mod-accent)]',
  },
} as const

export function ErrorState({
  message,
  onRetry,
  scheme = 'admin',
}: {
  message: string
  onRetry?: () => void
  scheme?: keyof typeof SCHEMES
}) {
  const t = SCHEMES[scheme]
  return (
    <div role="alert" className="flex flex-col items-center gap-4 px-6 py-16 text-center">
      <p className={`max-w-xs text-[14px] leading-relaxed ${t.text}`}>{message}</p>
      {onRetry && (
        <button
          onClick={onRetry}
          className={`rounded-xl px-5 py-2.5 text-[13px] font-bold text-white transition-transform duration-300 hover:brightness-110 active:scale-95 ${t.btn}`}
        >
          Повторить
        </button>
      )}
    </div>
  )
}
