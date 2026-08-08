import { useEffect, useState } from 'react'

// Plain typed dd.mm.yyyy text, not a native <input type="date"> -- the browser's calendar-icon
// picker renders inconsistently across OS/locale (showed as a static "дд.мм.гггг" mask with a
// barely-visible icon in dark mode, looked broken/unusable). Value in/out is still a real ISO
// date string; only the on-screen editing format changes.
function isoToDisplay(iso: string): string {
  if (!iso) return ''
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  return `${String(d.getDate()).padStart(2, '0')}.${String(d.getMonth() + 1).padStart(2, '0')}.${d.getFullYear()}`
}

function displayToIso(display: string, outputFormat: 'iso' | 'dateOnly'): string {
  const match = display.trim().match(/^(\d{1,2})\.(\d{1,2})\.(\d{4})$/)
  if (!match) return ''
  const [, dd, mm, yyyy] = match
  const day = Number(dd)
  const month = Number(mm)
  const year = Number(yyyy)
  if (month < 1 || month > 12 || day < 1 || day > 31) return ''
  const d = new Date(Date.UTC(year, month - 1, day))
  // Rejects e.g. 31.02.2026 -- Date normalizes overflow instead of erroring, so check it round-trips.
  if (d.getUTCFullYear() !== year || d.getUTCMonth() !== month - 1 || d.getUTCDate() !== day) return ''
  // dateOnly: backend fields typed DateOnly (tax-rate effective dates, payment period) expect a
  // bare "yyyy-MM-dd" -- a full ISO datetime string fails to bind against them.
  return outputFormat === 'dateOnly' ? d.toISOString().slice(0, 10) : d.toISOString()
}

export function DateField({
  value,
  onChange,
  title,
  className = '',
  outputFormat = 'iso',
}: {
  /** ISO date (or yyyy-MM-dd, or '') -- matching whatever the caller's own state already holds. */
  value: string
  onChange: (value: string) => void
  title?: string
  className?: string
  /** 'iso' (default) for DateTimeOffset-bound fields; 'dateOnly' for DateOnly-bound fields. */
  outputFormat?: 'iso' | 'dateOnly'
}) {
  const [text, setText] = useState(() => isoToDisplay(value))

  // Follows external resets (e.g. a "clear filters" button) without fighting the user's typing.
  useEffect(() => {
    setText(isoToDisplay(value))
  }, [value])

  function commit(raw: string) {
    const digits = raw.replace(/[^\d]/g, '')
    let formatted = digits.slice(0, 2)
    if (digits.length > 2) formatted += `.${digits.slice(2, 4)}`
    if (digits.length > 4) formatted += `.${digits.slice(4, 8)}`
    setText(formatted)
    const iso = displayToIso(formatted, outputFormat)
    if (iso || formatted === '') onChange(iso)
  }

  return (
    <input
      type="text"
      inputMode="numeric"
      value={text}
      onChange={(e) => commit(e.target.value)}
      placeholder="дд.мм.гггг"
      title={title}
      maxLength={10}
      className={`rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)] ${className}`}
    />
  )
}
