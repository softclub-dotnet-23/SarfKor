import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { meApi, ApiError, type UserConsent, type SecurityEvent, type ConsentType } from '../../lib/api'
import { registerDeviceToken } from '../../lib/api/deviceTokens'
import { useAuth } from '../../auth/AuthContext'
import { UserIcon, ShieldIcon, ClockIcon, BellIcon } from '../../components/icons'

const CONSENT_LABEL: Record<ConsentType, string> = {
  Geolocation: 'Геолокация — находить ближайшие магазины',
  ReceiptStorage: 'Хранение чеков — для сверки цен',
  PaymentData: 'Платёжные данные',
  Marketing: 'Маркетинговые рассылки',
}

const SECURITY_EVENT_LABEL: Record<string, string> = {
  LoginSucceeded: 'Успешный вход',
  LoginFailed: 'Неудачная попытка входа',
  NewDeviceLogin: 'Вход с нового устройства',
  PasswordChanged: 'Пароль изменён',
  AnomalousActivity: 'Подозрительная активность',
}

function Card({ children }: { children: React.ReactNode }) {
  return <div className="rounded-3xl bg-[color:var(--bg-card)] p-6 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]">{children}</div>
}

function ProfileSection() {
  const [displayName, setDisplayName] = useState('')
  const [preferredLanguage, setPreferredLanguage] = useState('ru')
  const [loaded, setLoaded] = useState(false)
  const [saving, setSaving] = useState(false)
  const [status, setStatus] = useState('')

  useEffect(() => {
    meApi
      .getProfile()
      .then((p) => {
        if (p.found) {
          setDisplayName(p.displayName ?? '')
          setPreferredLanguage(p.preferredLanguage ?? 'ru')
        }
      })
      .catch(() => {})
      .finally(() => setLoaded(true))
  }, [])

  async function save(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setStatus('')
    try {
      await meApi.updateProfile(displayName.trim() || 'Пользователь', undefined, preferredLanguage)
      setStatus('Сохранено')
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось сохранить')
    } finally {
      setSaving(false)
    }
  }

  if (!loaded) return <Card><p className="text-[13px] text-[color:var(--text-tertiary)]">Загрузка…</p></Card>

  return (
    <Card>
      <div className="mb-4 flex items-center gap-2 text-[15px] font-bold">
        <UserIcon width={17} height={17} />
        Профиль
      </div>
      <form onSubmit={save} className="flex flex-col gap-3.5">
        <label className="block">
          <span className="mb-1.5 block text-[12.5px] font-semibold text-[color:var(--text-secondary)]">Имя</span>
          <input
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="w-full rounded-xl border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-3.5 py-2.5 text-[14px] outline-none focus:border-[color:var(--color-brand)]"
          />
        </label>
        <label className="block">
          <span className="mb-1.5 block text-[12.5px] font-semibold text-[color:var(--text-secondary)]">Язык</span>
          <select
            value={preferredLanguage}
            onChange={(e) => setPreferredLanguage(e.target.value)}
            className="w-full rounded-xl border border-[color:var(--border-subtle)] bg-[color:var(--bg-section)] px-3.5 py-2.5 text-[14px] outline-none focus:border-[color:var(--color-brand)]"
          >
            <option value="ru">Русский</option>
            <option value="tg">Тоҷикӣ</option>
            <option value="en">English</option>
          </select>
        </label>
        <div className="flex items-center gap-3">
          <button
            type="submit"
            disabled={saving}
            className="rounded-xl bg-[color:var(--color-brand)] px-5 py-2.5 text-[13.5px] font-bold text-white disabled:opacity-50"
          >
            Сохранить
          </button>
          {status && <span className="text-[12.5px] text-[color:var(--text-secondary)]">{status}</span>}
        </div>
      </form>
    </Card>
  )
}

function ConsentsSection() {
  const [consents, setConsents] = useState<Record<ConsentType, boolean>>({
    Geolocation: false,
    ReceiptStorage: false,
    PaymentData: false,
    Marketing: false,
  })
  const [loaded, setLoaded] = useState(false)
  const [busy, setBusy] = useState<ConsentType | null>(null)

  useEffect(() => {
    meApi
      .getConsents()
      .then((res) => {
        const next = { Geolocation: false, ReceiptStorage: false, PaymentData: false, Marketing: false }
        for (const c of res.consents as UserConsent[]) next[c.type] = c.isGranted
        setConsents(next)
      })
      .catch(() => {})
      .finally(() => setLoaded(true))
  }, [])

  async function toggle(type: ConsentType) {
    const next = !consents[type]
    setBusy(type)
    setConsents((cur) => ({ ...cur, [type]: next }))
    try {
      await meApi.recordConsent(type, next)
    } catch {
      setConsents((cur) => ({ ...cur, [type]: !next }))
    } finally {
      setBusy(null)
    }
  }

  if (!loaded) return <Card><p className="text-[13px] text-[color:var(--text-tertiary)]">Загрузка…</p></Card>

  return (
    <Card>
      <div className="mb-4 flex items-center gap-2 text-[15px] font-bold">
        <ShieldIcon width={17} height={17} />
        Разрешения и согласия
      </div>
      <div className="flex flex-col divide-y divide-[color:var(--border-subtle)]">
        {(Object.keys(consents) as ConsentType[]).map((type) => (
          <div key={type} className="flex items-center justify-between gap-3 py-3 first:pt-0 last:pb-0">
            <span className="text-[13.5px] font-medium">{CONSENT_LABEL[type]}</span>
            <button
              onClick={() => toggle(type)}
              disabled={busy === type}
              className={`relative h-7 w-12 shrink-0 rounded-full transition-colors disabled:opacity-50 ${consents[type] ? 'bg-[color:var(--color-brand)]' : 'bg-[color:var(--bg-section)] ring-1 ring-[color:var(--border-subtle)]'}`}
            >
              <span
                className="absolute top-1 h-5 w-5 rounded-full bg-white shadow transition-transform"
                style={{ transform: consents[type] ? 'translateX(22px)' : 'translateX(4px)' }}
              />
            </button>
          </div>
        ))}
      </div>
    </Card>
  )
}

function PushNotificationsSection() {
  const supported = typeof window !== 'undefined' && 'Notification' in window
  const [permission, setPermission] = useState<NotificationPermission | null>(supported ? Notification.permission : null)
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState('')

  async function enable() {
    if (!supported) return
    setBusy(true)
    setStatus('')
    try {
      const result = await Notification.requestPermission()
      setPermission(result)
      if (result === 'granted') {
        const token = crypto.randomUUID()
        await registerDeviceToken(token, 'Web')
        setStatus('Устройство зарегистрировано')
      } else {
        setStatus('Разрешение на уведомления не предоставлено')
      }
    } catch (err) {
      setStatus(err instanceof ApiError ? err.message : 'Не удалось зарегистрировать устройство')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Card>
      <div className="mb-4 flex items-center gap-2 text-[15px] font-bold">
        <BellIcon width={17} height={17} />
        Push-уведомления
      </div>
      {!supported ? (
        <p className="text-[13px] text-[color:var(--text-tertiary)]">Этот браузер не поддерживает push-уведомления.</p>
      ) : (
        <>
          <p className="mb-3 text-[13px] text-[color:var(--text-secondary)]">
            Регистрирует это устройство для будущих push-уведомлений. Доставка push пока не подключена — сейчас мы только сохраняем устройство на сервере.
          </p>
          <div className="flex items-center gap-3">
            <button
              onClick={enable}
              disabled={busy || permission === 'granted'}
              className="rounded-xl bg-[color:var(--color-brand)] px-5 py-2.5 text-[13.5px] font-bold text-white disabled:opacity-50"
            >
              {permission === 'granted' ? 'Уведомления включены' : 'Включить push-уведомления на этом устройстве'}
            </button>
            {status && <span className="text-[12.5px] text-[color:var(--text-secondary)]">{status}</span>}
          </div>
        </>
      )}
    </Card>
  )
}

function SecurityEventsSection() {
  const [events, setEvents] = useState<SecurityEvent[] | null>(null)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setError('')
    try {
      setEvents((await meApi.getSecurityEvents()).events)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить историю входов')
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  return (
    <Card>
      <div className="mb-4 flex items-center gap-2 text-[15px] font-bold">
        <ClockIcon width={17} height={17} />
        История безопасности
      </div>
      {events === null && !error && <p className="text-[13px] text-[color:var(--text-tertiary)]">Загрузка…</p>}
      {error && <p className="text-[13px] text-[color:var(--text-secondary)]">{error}</p>}
      {events && events.length === 0 && <p className="text-[13px] text-[color:var(--text-tertiary)]">Событий пока нет</p>}
      {events && events.length > 0 && (
        <div className="flex flex-col divide-y divide-[color:var(--border-subtle)]">
          {events.map((e, i) => (
            <div key={i} className="flex items-center justify-between gap-3 py-2.5 text-[13px]">
              <span className="font-medium">{SECURITY_EVENT_LABEL[e.type] ?? e.type}</span>
              <span className="text-[color:var(--text-tertiary)]">{new Date(e.occurredAt).toLocaleString('ru-RU')}</span>
            </div>
          ))}
        </div>
      )}
    </Card>
  )
}

export function AccountPage() {
  const { user } = useAuth()
  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="mb-1 text-[22px] font-extrabold tracking-tight">Профиль</h1>
      <p className="mb-6 text-[14px] text-[color:var(--text-secondary)]">{user?.email}</p>
      <div className="flex flex-col gap-4">
        <ProfileSection />
        <ConsentsSection />
        <PushNotificationsSection />
        <SecurityEventsSection />
      </div>
    </div>
  )
}
