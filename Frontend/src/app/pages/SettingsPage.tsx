import { useState } from 'react'
import { meApi, type ConsentType } from '../../lib/api'
import { EmptyState, ErrorState, LINE, Reveal, SectionTitle, Skeleton, TXT, useAsync } from '../ui'

/* Exactly the four the backend defines (ConsentType in lib/api/me.ts) — no more,
   no fewer, so a toggle can never post a value the server will reject. */
const CONSENTS: { type: ConsentType; label: string; body: string }[] = [
  {
    type: 'Geolocation',
    label: 'Геолокация',
    body: 'Нужна, чтобы показывать расстояние до магазинов и ближайшие цены.',
  },
  {
    type: 'ReceiptStorage',
    label: 'Хранение чеков',
    body: 'Позволяет сверять загруженные чеки с ценниками магазинов.',
  },
  {
    type: 'PaymentData',
    label: 'Платёжные данные',
    body: 'Обработка платежей через провайдера. Карты на нашей стороне не хранятся.',
  },
  {
    type: 'Marketing',
    label: 'Маркетинговые сообщения',
    body: 'Подборки скидок и товаров с истекающим сроком.',
  },
]

const EVENT_LABEL: Record<string, string> = {
  LoginSucceeded: 'Вход выполнен',
  LoginFailed: 'Неудачная попытка входа',
  NewDeviceLogin: 'Вход с нового устройства',
  PasswordChanged: 'Пароль изменён',
  AnomalousActivity: 'Подозрительная активность',
}

export function SettingsPage() {
  const consents = useAsync(() => meApi.getConsents(), [])
  const events = useAsync(() => meApi.getSecurityEvents(), [])
  const [busy, setBusy] = useState<string | null>(null)

  async function toggle(type: ConsentType, next: boolean) {
    setBusy(type)
    try {
      await meApi.recordConsent(type, next)
      consents.reload()
    } finally {
      setBusy(null)
    }
  }

  const granted = (t: ConsentType) =>
    consents.data?.consents.find((c) => c.type === t)?.isGranted ?? false

  return (
    <>
      <Reveal>
        <p
          className="mb-5 text-[10px] font-bold uppercase tracking-[0.28em]"
          style={{ color: TXT.rest }}
        >
          Настройки
        </p>
        <h1 className="mb-12 text-[clamp(30px,4.6vw,46px)] font-extrabold leading-[1.04] tracking-[-0.04em]">
          Приватность и вход
        </h1>
      </Reveal>

      {/* ── CONSENTS ─────────────────────────────────────────── */}
      <Reveal i={1}>
        <SectionTitle>Согласия</SectionTitle>

        {consents.loading && <Skeleton h={150} className="rounded-xl" />}
        {consents.error && <ErrorState message={consents.error} onRetry={consents.reload} />}

        {!consents.loading && !consents.error && (
          <ul className="mb-16 flex flex-col">
            {CONSENTS.map((c) => {
              const on = granted(c.type)
              return (
                <li
                  key={c.type}
                  className="flex items-start justify-between gap-6 border-b py-5"
                  style={{ borderColor: LINE }}
                >
                  <span className="min-w-0">
                    <span className="block text-[14.5px] font-semibold text-[color:var(--app-text-primary)]">{c.label}</span>
                    <span
                      className="mt-1 block max-w-[420px] text-[12.5px] leading-relaxed"
                      style={{ color: TXT.rest }}
                    >
                      {c.body}
                    </span>
                  </span>
                  <button
                    role="switch"
                    aria-checked={on}
                    aria-label={c.label}
                    disabled={busy === c.type}
                    onClick={() => toggle(c.type, !on)}
                    className="relative mt-1 h-[26px] w-[46px] shrink-0 rounded-full transition-colors duration-500 disabled:opacity-50"
                    style={{
                      background: on ? TXT.primary : 'color-mix(in srgb, var(--app-text-primary) 14%, transparent)',
                      transitionTimingFunction: 'cubic-bezier(0.16,1,0.3,1)',
                    }}
                  >
                    <span
                      aria-hidden
                      className="absolute top-1/2 block h-[20px] w-[20px] -translate-y-1/2 rounded-full transition-all duration-500"
                      style={{
                        left: on ? 23 : 3,
                        background: on ? 'var(--bg-app)' : 'color-mix(in srgb, var(--app-text-primary) 75%, transparent)',
                        transitionTimingFunction: 'cubic-bezier(0.16,1,0.3,1)',
                      }}
                    />
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </Reveal>

      {/* ── SECURITY ─────────────────────────────────────────── */}
      <Reveal i={2}>
        <SectionTitle>Последние события входа</SectionTitle>

        {events.loading && <Skeleton h={110} className="rounded-xl" />}
        {events.error && <ErrorState message={events.error} onRetry={events.reload} />}

        {!events.loading && !events.error && events.data && (
          events.data.events.length === 0 ? (
            <EmptyState title="Событий пока нет" />
          ) : (
            <ul className="flex flex-col">
              {events.data.events.slice(0, 10).map((ev, i) => (
                <li
                  key={i}
                  className="flex items-baseline justify-between gap-5 border-b py-4"
                  style={{ borderColor: LINE }}
                >
                  <span className="text-[13.5px] text-[color:var(--app-text-primary)]">
                    {EVENT_LABEL[ev.type] ?? ev.type}
                  </span>
                  <span className="shrink-0 text-[12px] tabular-nums" style={{ color: TXT.rest }}>
                    {new Date(ev.occurredAt).toLocaleString('ru-RU', {
                      day: '2-digit',
                      month: '2-digit',
                      year: '2-digit',
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
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
