// The backend takes plain `DateOnly` query params (YYYY-MM-DD) — no time
// component, no timezone conversion.
export function toDateOnly(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

export function today(): string {
  return toDateOnly(new Date())
}

export function daysAgo(n: number): string {
  const d = new Date()
  d.setDate(d.getDate() - n)
  return toDateOnly(d)
}

export function firstOfMonth(): string {
  const d = new Date()
  return toDateOnly(new Date(d.getFullYear(), d.getMonth(), 1))
}

export function weekdayLabel(dateStr: string): string {
  const labels = ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб']
  return labels[new Date(dateStr).getDay()]
}
