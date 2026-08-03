import { useRef, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Panel, SectionHeader } from '../cabinet/components/primitives'
import { useTheme } from '../../theme/ThemeProvider'
import { useThemeTransition } from '../../theme/useThemeTransition'
import { useAuth } from '../../auth/AuthContext'
import { SunIcon, MoonIcon } from '../../components/icons'
import { CheckIcon } from '../components/icons'
import { SuppliersSection } from './SuppliersSection'
import { storesApi, meApi, ApiError } from '../../lib/api'

const DAILY_GOAL_KEY = 'sarfkor-daily-goal'

function AvatarSection() {
  const { user } = useAuth()
  const fileRef = useRef<HTMLInputElement>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState<'idle' | 'saved' | 'error'>('idle')
  const [errMsg, setErrMsg] = useState('')

  const initial = (user?.email ?? '?').charAt(0).toUpperCase()

  async function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    if (file.size > 2 * 1024 * 1024) {
      setErrMsg('Максимальный размер: 2 МБ')
      setStatus('error')
      return
    }
    if (!file.type.startsWith('image/')) {
      setErrMsg('Разрешены только изображения')
      setStatus('error')
      return
    }
    setPreview(URL.createObjectURL(file))
    setBusy(true)
    setStatus('idle')
    try {
      await meApi.uploadAvatar(file)
      setStatus('saved')
      setTimeout(() => setStatus('idle'), 2200)
    } catch (err) {
      setErrMsg(err instanceof ApiError ? err.message : 'Не удалось загрузить')
      setStatus('error')
      setPreview(null)
    } finally {
      setBusy(false)
      if (fileRef.current) fileRef.current.value = ''
    }
  }

  return (
    <Panel>
      <SectionHeader title="Аватар" />
      <div className="flex items-center gap-5">
        <button
          onClick={() => fileRef.current?.click()}
          disabled={busy}
          className="group relative h-[72px] w-[72px] shrink-0 overflow-hidden rounded-full ring-2 ring-[color:var(--admin-border)] ring-offset-2 ring-offset-[color:var(--admin-card)] transition-opacity hover:opacity-80 disabled:cursor-not-allowed disabled:opacity-50"
          title="Загрузить фото"
        >
          {preview ? (
            <img src={preview} alt="Аватар" className="h-full w-full object-cover" />
          ) : (
            <span className="flex h-full w-full items-center justify-center text-[26px] font-extrabold text-[color:var(--admin-text)]" style={{ background: 'color-mix(in srgb, var(--admin-text) 10%, var(--admin-hover))' }}>
              {initial}
            </span>
          )}
          <span className="absolute inset-0 flex items-center justify-center bg-black/40 opacity-0 transition-opacity group-hover:opacity-100">
            <svg width={20} height={20} viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
              <path d="M23 19a2 2 0 01-2 2H3a2 2 0 01-2-2V8a2 2 0 012-2h4l2-3h6l2 3h4a2 2 0 012 2z"/>
              <circle cx="12" cy="13" r="4"/>
            </svg>
          </span>
        </button>
        <input
          ref={fileRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          className="hidden"
          onChange={handleFile}
        />
        <div>
          <button
            onClick={() => fileRef.current?.click()}
            disabled={busy}
            className="mb-1 text-[13px] font-semibold text-[color:var(--admin-accent)] hover:opacity-70 disabled:opacity-40"
          >
            {busy ? 'Загружаем…' : 'Загрузить фото'}
          </button>
          <div className="text-[11.5px] text-[color:var(--admin-text-tertiary)]">
            JPEG, PNG или WebP · максимум 2 МБ
          </div>
          {status === 'saved' && (
            <div className="mt-1 text-[11.5px] font-medium text-[color:var(--admin-success)]">Аватар обновлён</div>
          )}
          {status === 'error' && (
            <div className="mt-1 text-[11.5px] font-medium text-[color:var(--admin-danger)]">{errMsg}</div>
          )}
        </div>
      </div>
    </Panel>
  )
}

function EyeToggle({ shown, onClick }: { shown: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={shown ? 'Скрыть пароль' : 'Показать пароль'}
      className="absolute right-3 top-1/2 -translate-y-1/2 text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text-secondary)]"
    >
      <svg width={16} height={16} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
        {shown ? (
          <>
            <path d="M17.94 17.94A10.94 10.94 0 0 1 12 19c-7 0-11-7-11-7a21.6 21.6 0 0 1 5.06-5.94M9.9 4.24A10.4 10.4 0 0 1 12 4c7 0 11 7 11 7a21.6 21.6 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
            <line x1="1" y1="1" x2="23" y2="23" />
          </>
        ) : (
          <>
            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z" />
            <circle cx="12" cy="12" r="3" />
          </>
        )}
      </svg>
    </button>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">{label}</span>
      {children}
    </label>
  )
}

const inputCls = 'rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)] placeholder:text-[color:var(--admin-text-tertiary)]'

function StoreSection({ storeId }: { storeId: number | null }) {
  const { myStores, refreshRoles } = useAuth()
  const navigate = useNavigate()
  const currentStore = (myStores ?? []).find((s) => s.storeId === storeId)

  const [name, setName] = useState(currentStore?.name ?? '')
  const [address, setAddress] = useState('')
  const [lat, setLat] = useState('')
  const [lng, setLng] = useState('')
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState<'idle' | 'saved' | 'error'>('idle')
  const [errMsg, setErrMsg] = useState('')
  const [locating, setLocating] = useState(false)

  function handleLocate() {
    if (!navigator.geolocation) return
    setLocating(true)
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLat(pos.coords.latitude.toFixed(6))
        setLng(pos.coords.longitude.toFixed(6))
        setLocating(false)
      },
      () => setLocating(false),
    )
  }

  async function handleSave(e: FormEvent) {
    e.preventDefault()
    if (!storeId || busy) return
    const latitude = Number(lat)
    const longitude = Number(lng)
    if (lat && (isNaN(latitude) || latitude < -90 || latitude > 90)) {
      setErrMsg('Широта должна быть от −90 до 90')
      setStatus('error')
      return
    }
    if (lng && (isNaN(longitude) || longitude < -180 || longitude > 180)) {
      setErrMsg('Долгота должна быть от −180 до 180')
      setStatus('error')
      return
    }
    setBusy(true)
    setStatus('idle')
    try {
      const res = await storesApi.updateStore(storeId, {
        name: name.trim() || (currentStore?.name ?? ''),
        address: address.trim() || '—',
        latitude: lat ? latitude : 0,
        longitude: lng ? longitude : 0,
      })
      if (res.outcome === 'Updated') {
        await refreshRoles()
        setStatus('saved')
        setTimeout(() => setStatus('idle'), 2200)
      } else if (res.outcome === 'Forbidden') {
        setErrMsg('Нет доступа к редактированию этого магазина')
        setStatus('error')
      } else {
        setErrMsg('Магазин не найден')
        setStatus('error')
      }
    } catch (err) {
      setErrMsg(err instanceof ApiError ? err.message : 'Не удалось сохранить')
      setStatus('error')
    } finally {
      setBusy(false)
    }
  }

  if (!storeId) {
    return (
      <Panel>
        <SectionHeader title="Магазин" />
        <div className="rounded-2xl bg-[color:var(--admin-hover)] px-4 py-4 text-[13px] text-[color:var(--admin-text-tertiary)]">
          Магазин не выбран —{' '}
          <button onClick={() => navigate('/admin/onboarding')} className="font-semibold text-[color:var(--admin-accent)]">
            выберите магазин
          </button>
        </div>
      </Panel>
    )
  }

  return (
    <Panel>
      <SectionHeader title="Информация о магазине" />
      <form onSubmit={handleSave} className="flex flex-col gap-4">
        <Field label="Название">
          <input
            className={inputCls}
            placeholder={currentStore?.name ?? 'Название магазина'}
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </Field>
        <Field label="Адрес">
          <input
            className={inputCls}
            placeholder="ул. Рудаки, 17, Душанбе"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
          />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Широта">
            <input
              className={inputCls}
              placeholder="38.559772"
              value={lat}
              onChange={(e) => setLat(e.target.value)}
            />
          </Field>
          <Field label="Долгота">
            <input
              className={inputCls}
              placeholder="68.773940"
              value={lng}
              onChange={(e) => setLng(e.target.value)}
            />
          </Field>
        </div>
        <button
          type="button"
          onClick={handleLocate}
          disabled={locating}
          className="flex items-center gap-2 self-start text-[12px] font-medium text-[color:var(--admin-accent)] hover:opacity-70 disabled:opacity-50"
        >
          {locating ? 'Определяем…' : '⌖ Определить моё местоположение'}
        </button>

        {status === 'error' && (
          <div className="rounded-lg bg-[color:var(--admin-danger-dim)] px-3.5 py-2.5 text-[12.5px] font-medium text-[color:var(--admin-danger)]">
            {errMsg}
          </div>
        )}

        <div className="flex items-center gap-3">
          <button
            type="submit"
            disabled={busy}
            className="flex items-center gap-2 rounded-full bg-[color:var(--admin-accent)] px-5 py-2.5 text-[13px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50"
          >
            {status === 'saved' && <CheckIcon width={14} height={14} />}
            {busy ? 'Сохраняем…' : status === 'saved' ? 'Сохранено' : 'Сохранить'}
          </button>
          <button
            type="button"
            onClick={() => navigate('/admin/onboarding')}
            className="text-[12px] text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)]"
          >
            Сменить магазин
          </button>
        </div>
      </form>
    </Panel>
  )
}

function PasswordSection() {
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [showCurrent, setShowCurrent] = useState(false)
  const [showNext, setShowNext] = useState(false)
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState<'idle' | 'saved' | 'error'>('idle')
  const [errMsg, setErrMsg] = useState('')

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (next !== confirm) {
      setErrMsg('Новые пароли не совпадают')
      setStatus('error')
      return
    }
    if (next.length < 8) {
      setErrMsg('Новый пароль должен содержать минимум 8 символов')
      setStatus('error')
      return
    }
    setBusy(true)
    setStatus('idle')
    try {
      const res = await meApi.changePassword(current, next)
      if (res.outcome === 'Changed') {
        setCurrent('')
        setNext('')
        setConfirm('')
        setStatus('saved')
        setTimeout(() => setStatus('idle'), 2500)
      } else if (res.outcome === 'WrongCurrentPassword') {
        setErrMsg('Неверный текущий пароль')
        setStatus('error')
      } else {
        setErrMsg('Не удалось сменить пароль')
        setStatus('error')
      }
    } catch (err) {
      setErrMsg(err instanceof ApiError ? err.message : 'Не удалось сменить пароль')
      setStatus('error')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Panel>
      <SectionHeader title="Смена пароля" />
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <Field label="Текущий пароль">
          <div className="relative">
            <input
              type={showCurrent ? 'text' : 'password'}
              required
              autoComplete="current-password"
              value={current}
              onChange={(e) => setCurrent(e.target.value)}
              placeholder="••••••••"
              className={`${inputCls} w-full pr-10`}
            />
            <EyeToggle shown={showCurrent} onClick={() => setShowCurrent((v) => !v)} />
          </div>
        </Field>
        <Field label="Новый пароль">
          <div className="relative">
            <input
              type={showNext ? 'text' : 'password'}
              required
              minLength={8}
              autoComplete="new-password"
              value={next}
              onChange={(e) => setNext(e.target.value)}
              placeholder="••••••••"
              className={`${inputCls} w-full pr-10`}
            />
            <EyeToggle shown={showNext} onClick={() => setShowNext((v) => !v)} />
          </div>
          <span className="text-[11px] text-[color:var(--admin-text-tertiary)]">Минимум 8 символов</span>
        </Field>
        <Field label="Повторите новый пароль">
          <input
            type={showNext ? 'text' : 'password'}
            required
            autoComplete="new-password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            placeholder="••••••••"
            className={`${inputCls} w-full`}
          />
        </Field>

        {status === 'error' && (
          <div className="rounded-lg bg-[color:var(--admin-danger-dim)] px-3.5 py-2.5 text-[12.5px] font-medium text-[color:var(--admin-danger)]">
            {errMsg}
          </div>
        )}
        {status === 'saved' && (
          <div className="rounded-lg bg-[color:var(--admin-success-dim)] px-3.5 py-2.5 text-[12.5px] font-medium text-[color:var(--admin-success)]">
            Пароль успешно изменён
          </div>
        )}

        <button
          type="submit"
          disabled={busy || !current || !next || !confirm}
          className="flex items-center justify-center gap-2 self-start rounded-full bg-[color:var(--admin-accent)] px-5 py-2.5 text-[13px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50"
        >
          {busy ? 'Меняем…' : 'Изменить пароль'}
        </button>
      </form>
    </Panel>
  )
}

export function SettingsPage() {
  const { theme, setTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const { storeId, user, logout } = useAuth()

  const [dailyGoal, setDailyGoal] = useState(() => Number(localStorage.getItem(DAILY_GOAL_KEY)) || 150)
  const [goalSaved, setGoalSaved] = useState(false)

  function handleGoalSave(e: FormEvent) {
    e.preventDefault()
    localStorage.setItem(DAILY_GOAL_KEY, String(dailyGoal))
    setGoalSaved(true)
    setTimeout(() => setGoalSaved(false), 2200)
  }

  return (
    <div className="mx-auto flex max-w-[820px] flex-col gap-5">
      <AvatarSection />
      <StoreSection storeId={storeId} />

      <Panel>
        <SectionHeader title="Оформление" />
        <div className="flex gap-2 rounded-full bg-[color:var(--admin-hover)] p-1.5">
          {(['light', 'dark'] as const).map((t) => (
            <button
              key={t}
              onClick={(e) => runThemeTransition(e.currentTarget, () => setTheme(t))}
              className={`flex flex-1 items-center justify-center gap-2 rounded-full py-2.5 text-[13px] font-semibold transition-colors duration-200 ${
                theme === t
                  ? 'bg-[color:var(--admin-card)] text-[color:var(--admin-text)] [box-shadow:var(--admin-shadow)]'
                  : 'text-[color:var(--admin-text-tertiary)]'
              }`}
            >
              {t === 'light' ? <SunIcon width={16} height={16} /> : <MoonIcon width={16} height={16} />}
              {t === 'light' ? 'Светлая' : 'Тёмная'}
            </button>
          ))}
        </div>
      </Panel>

      <Panel>
        <SectionHeader title="Дневной план продаж" />
        <form onSubmit={handleGoalSave} className="flex flex-col gap-4">
          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">
              Цель:{' '}
              <span className="font-semibold text-[color:var(--admin-text)]">{dailyGoal} чеков</span>
            </span>
            <input
              type="range"
              min={50}
              max={500}
              step={10}
              value={dailyGoal}
              onChange={(e) => setDailyGoal(Number(e.target.value))}
              className="accent-[color:var(--admin-accent)]"
            />
            <span className="text-[11px] text-[color:var(--admin-text-tertiary)]">
              Хранится только в этом браузере — используется для кольца «Цель дня» на дашборде
            </span>
          </label>
          <button
            type="submit"
            className="flex items-center justify-center gap-2 self-start rounded-full bg-[color:var(--admin-accent)] px-5 py-2.5 text-[13px] font-bold text-[color:var(--admin-accent-fg)] transition-transform hover:scale-[1.02] active:scale-[0.98]"
          >
            {goalSaved && <CheckIcon width={14} height={14} />}
            {goalSaved ? 'Сохранено' : 'Сохранить'}
          </button>
        </form>
      </Panel>

      <SuppliersSection storeId={storeId} />

      <PasswordSection />

      <Panel>
        <SectionHeader title="Аккаунт" />
        <div className="flex items-center justify-between rounded-2xl bg-[color:var(--admin-hover)] px-4 py-3.5">
          <div>
            <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
            <div className="mt-0.5 text-[11px] text-[color:var(--admin-text-tertiary)]">
              ID: {user?.userId?.slice(0, 12)}…
            </div>
          </div>
          <button
            onClick={logout}
            className="shrink-0 rounded-full bg-[color:var(--admin-card)] px-3.5 py-1.5 text-[11px] font-semibold text-[color:var(--admin-danger)] ring-1 ring-[color:var(--admin-border)] hover:bg-[color:var(--admin-danger-dim)]"
          >
            Выйти
          </button>
        </div>
      </Panel>
    </div>
  )
}
