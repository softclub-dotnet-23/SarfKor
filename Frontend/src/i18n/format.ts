import { useLanguage, type Language } from './LanguageProvider'

// Tajikistan uses the same grouping/decimal conventions Intl ships for 'ru-RU' (space thousands
// separator, comma decimal) — there's no 'tg-TJ' Intl locale data bundled in most runtimes, so
// both languages format numbers identically; only which BCP-47 tag gets used for Date formatting
// (month/weekday names) actually differs by language.
const LOCALE_TAG: Record<Language, string> = { ru: 'ru-RU', tg: 'tg-TJ' }
// Date/time names in the 'tg-TJ' ICU locale come out empty/broken in some browsers (patchy Tajik
// coverage) — fall back to 'ru-RU' for the calendar output specifically while still keying number
// formatting off the intended locale (which degrades gracefully, unlike date names).
const DATE_LOCALE_TAG: Record<Language, string> = { ru: 'ru-RU', tg: 'ru-RU' }

export function formatNumber(n: number, language: Language, options?: Intl.NumberFormatOptions): string {
  return n.toLocaleString(LOCALE_TAG[language], options)
}

export function formatMoney(amount: number, language: Language): string {
  return amount.toLocaleString(LOCALE_TAG[language], { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export function formatDate(iso: string | Date, language: Language, options?: Intl.DateTimeFormatOptions): string {
  const d = typeof iso === 'string' ? new Date(iso) : iso
  return d.toLocaleDateString(DATE_LOCALE_TAG[language], options)
}

export function formatDateTime(iso: string | Date, language: Language, options?: Intl.DateTimeFormatOptions): string {
  const d = typeof iso === 'string' ? new Date(iso) : iso
  return d.toLocaleString(DATE_LOCALE_TAG[language], options)
}

export function formatTime(iso: string | Date, language: Language, options?: Intl.DateTimeFormatOptions): string {
  const d = typeof iso === 'string' ? new Date(iso) : iso
  return d.toLocaleTimeString(DATE_LOCALE_TAG[language], options ?? { hour: '2-digit', minute: '2-digit' })
}

/** Convenience hook — every StorePartner page currently hand-calls `.toLocaleString('ru-RU')`
 *  directly; this is the drop-in replacement, pre-bound to the active language. */
export function useLocaleFormat() {
  const { language } = useLanguage()
  return {
    language,
    number: (n: number, options?: Intl.NumberFormatOptions) => formatNumber(n, language, options),
    money: (amount: number) => formatMoney(amount, language),
    date: (iso: string | Date, options?: Intl.DateTimeFormatOptions) => formatDate(iso, language, options),
    dateTime: (iso: string | Date, options?: Intl.DateTimeFormatOptions) => formatDateTime(iso, language, options),
    time: (iso: string | Date, options?: Intl.DateTimeFormatOptions) => formatTime(iso, language, options),
  }
}
