import { useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card } from '../components/Card'
import { Input } from '../components/Input'
import { Toast } from '../components/Toast'
import { useTheme } from '../../theme/ThemeProvider'
import { useThemeTransition } from '../../theme/useThemeTransition'
import { useAuth } from '../../auth/AuthContext'
import { useProfile } from '../../lib/useProfile'
import { useAvatarUrl } from '../../lib/useAvatarUrl'
import { meApi, ApiError } from '../../lib/api'
import { SunIcon, MoonIcon } from '../../components/icons'
import { StoreIcon, KeyIcon, CheckIcon, UploadIcon } from '../components/icons'
import { SuppliersSection } from './SuppliersSection'

const DAILY_GOAL_KEY = 'sarfkor-daily-goal'
const MAX_AVATAR_BYTES = 2 * 1024 * 1024

function AvatarSection() {
  const { user } = useAuth()
  const { profile, reload } = useProfile()
  const [avatarVersion, setAvatarVersion] = useState(0)
  const avatarUrl = useAvatarUrl(!!profile?.avatarReference, avatarVersion)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const fileInputRef = useRef<HTMLInputElement>(null)
  const initial = user?.email?.charAt(0).toUpperCase() ?? '?'

  async function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return

    if (!['image/jpeg', 'image/png'].includes(file.type)) {
      setError('Только JPEG или PNG.')
      return
    }
    if (file.size > MAX_AVATAR_BYTES) {
      setError('Файл больше 2 МБ.')
      return
    }

    setBusy(true)
    setError('')
    try {
      await meApi.uploadAvatar(file)
      setAvatarVersion((v) => v + 1)
      reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить фото')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mb-5 flex items-center gap-4">
      <div
        className="grid h-16 w-16 shrink-0 place-items-center overflow-hidden rounded-2xl text-[22px] font-bold text-white [box-shadow:var(--admin-shadow)]"
        style={{ background: 'linear-gradient(135deg, var(--admin-accent), color-mix(in srgb, var(--admin-accent) 65%, black))' }}
      >
        {avatarUrl ? <img src={avatarUrl} alt="" className="h-full w-full object-cover" /> : initial}
      </div>
      <div>
        <input ref={fileInputRef} type="file" accept="image/jpeg,image/png" className="hidden" onChange={handleFileChange} />
        <button
          onClick={() => fileInputRef.current?.click()}
          disabled={busy}
          className="flex items-center gap-1.5 rounded-lg bg-[color:var(--admin-hover)] px-3 py-1.5 text-[12px] font-semibold text-[color:var(--admin-text-secondary)] ring-1 ring-[color:var(--admin-border)] hover:text-[color:var(--admin-text)] disabled:opacity-50"
        >
          <UploadIcon width={13} height={13} />
          {busy ? 'Загружаем…' : 'Изменить фото'}
        </button>
        <div className="mt-1.5 text-[11px] text-[color:var(--admin-text-tertiary)]">JPEG или PNG, до 2 МБ</div>
        {error && <div className="mt-1 text-[11px] font-medium text-[color:var(--admin-danger)]">{error}</div>}
      </div>
    </div>
  )
}

function ChangePasswordForm() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    if (newPassword !== confirmPassword) {
      setError('Пароли не совпадают.')
      return
    }
    setBusy(true)
    try {
      await meApi.changePassword(currentPassword, newPassword)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setDone(true)
      setTimeout(() => setDone(false), 2500)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сменить пароль')
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <Input
        type="password"
        autoComplete="current-password"
        placeholder="Текущий пароль"
        value={currentPassword}
        onChange={(e) => setCurrentPassword(e.target.value)}
        required
      />
      <Input
        type="password"
        autoComplete="new-password"
        placeholder="Новый пароль"
        value={newPassword}
        onChange={(e) => setNewPassword(e.target.value)}
        required
        minLength={8}
      />
      <Input
        type="password"
        autoComplete="new-password"
        placeholder="Повторите новый пароль"
        value={confirmPassword}
        onChange={(e) => setConfirmPassword(e.target.value)}
        required
        minLength={8}
      />
      {error && <div className="text-[11px] font-medium text-[color:var(--admin-danger)]">{error}</div>}
      <button
        type="submit"
        disabled={busy}
        className="self-start rounded-lg bg-[color:var(--admin-accent-soft)] px-3.5 py-2 text-[12.5px] font-semibold text-[color:var(--admin-accent)] transition-opacity hover:opacity-80 disabled:opacity-50"
      >
        {busy ? 'Меняем…' : 'Сменить пароль'}
      </button>
      <Toast open={done} variant="success">
        Пароль изменён. Другие сессии выйдут из аккаунта.
      </Toast>
    </form>
  )
}

export function SettingsPage() {
  const { theme, setTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const { storeId, user, logout } = useAuth()
  const navigate = useNavigate()

  const [dailyGoal, setDailyGoal] = useState(() => Number(localStorage.getItem(DAILY_GOAL_KEY)) || 150)
  const [saved, setSaved] = useState(false)

  function handleSave(e: FormEvent) {
    e.preventDefault()
    localStorage.setItem(DAILY_GOAL_KEY, String(dailyGoal))
    setSaved(true)
    setTimeout(() => setSaved(false), 2200)
  }

  return (
    <div className="mx-auto flex max-w-[820px] flex-col gap-6">
      <Card className="p-6">
        <div className="mb-5 flex items-center gap-2">
          <StoreIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Магазин</span>
        </div>
        <div className="mb-5 flex items-center justify-between rounded-xl bg-[color:var(--admin-hover)] px-4 py-3">
          <div>
            <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">ID магазина: {storeId}</div>
            <div className="mt-0.5 text-[11px] text-[color:var(--admin-text-tertiary)]">
              Редактирование названия и адреса магазина пока не поддерживается бэкендом
            </div>
          </div>
          <button
            onClick={() => navigate('/admin/onboarding')}
            className="shrink-0 rounded-lg bg-[color:var(--admin-card)] px-3 py-1.5 text-[11px] font-semibold text-[color:var(--admin-text-secondary)] ring-1 ring-[color:var(--admin-border)] hover:text-[color:var(--admin-text)]"
          >
            Сменить магазин
          </button>
        </div>
        <form onSubmit={handleSave} className="flex flex-col gap-4">
          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">
              Дневной план продаж: <span className="font-semibold text-[color:var(--admin-text)]">{dailyGoal} чеков</span>
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
              Хранится только в этом браузере и используется для кольца «Цель дня» на дашборде
            </span>
          </label>
          <button
            type="submit"
            className="flex items-center justify-center gap-2 self-start rounded-xl bg-[color:var(--admin-accent)] px-5 py-2.5 text-[13px] font-bold text-white transition-transform hover:scale-[1.02] active:scale-[0.98]"
          >
            {saved ? <CheckIcon width={15} height={15} /> : null}
            {saved ? 'Сохранено' : 'Сохранить изменения'}
          </button>
        </form>
      </Card>

      <Card className="p-6">
        <div className="mb-5 flex items-center gap-2">
          <SunIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Оформление</span>
        </div>
        <div className="flex gap-3">
          <button
            onClick={(e) => runThemeTransition(e.currentTarget, () => setTheme('light'))}
            className={`flex flex-1 flex-col items-center gap-2 rounded-xl border py-4 text-[13px] font-semibold transition-colors ${
              theme === 'light'
                ? 'border-[color:var(--admin-accent)] bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]'
                : 'border-[color:var(--admin-border)] text-[color:var(--admin-text-secondary)]'
            }`}
          >
            <SunIcon width={20} height={20} />
            Светлая
          </button>
          <button
            onClick={(e) => runThemeTransition(e.currentTarget, () => setTheme('dark'))}
            className={`flex flex-1 flex-col items-center gap-2 rounded-xl border py-4 text-[13px] font-semibold transition-colors ${
              theme === 'dark'
                ? 'border-[color:var(--admin-accent)] bg-[color:var(--admin-accent-soft)] text-[color:var(--admin-accent)]'
                : 'border-[color:var(--admin-border)] text-[color:var(--admin-text-secondary)]'
            }`}
          >
            <MoonIcon width={20} height={20} />
            Тёмная
          </button>
        </div>
      </Card>

      <SuppliersSection storeId={storeId} />

      <Card className="p-6">
        <div className="mb-5 flex items-center gap-2">
          <KeyIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Аккаунт</span>
        </div>

        <AvatarSection />

        <div className="mb-5 flex items-center justify-between rounded-xl bg-[color:var(--admin-hover)] px-4 py-3">
          <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
          <button
            onClick={logout}
            className="shrink-0 rounded-lg bg-[color:var(--admin-card)] px-3 py-1.5 text-[11px] font-semibold text-[color:var(--admin-danger)] ring-1 ring-[color:var(--admin-border)] hover:bg-[color:var(--admin-danger-dim)]"
          >
            Выйти
          </button>
        </div>

        <div className="mb-3 text-[12px] font-semibold text-[color:var(--admin-text-secondary)]">Сменить пароль</div>
        <ChangePasswordForm />
      </Card>
    </div>
  )
}
