import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card } from '../components/Card'
import { useTheme } from '../../theme/ThemeProvider'
import { useThemeTransition } from '../../theme/useThemeTransition'
import { useAuth } from '../../auth/AuthContext'
import { SunIcon, MoonIcon } from '../../components/icons'
import { StoreIcon, BellIcon, KeyIcon, CheckIcon } from '../components/icons'

const DAILY_GOAL_KEY = 'sarfkor-daily-goal'
const NOTIFY_KEY = 'sarfkor-notify-prefs'

interface NotifyPrefs {
  lowStock: boolean
  dailyReport: boolean
  voids: boolean
}

function loadNotifyPrefs(): NotifyPrefs {
  try {
    return { lowStock: true, dailyReport: true, voids: false, ...JSON.parse(localStorage.getItem(NOTIFY_KEY) ?? '{}') }
  } catch {
    return { lowStock: true, dailyReport: true, voids: false }
  }
}

function Toggle({ checked, onChange }: { checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      onClick={() => onChange(!checked)}
      className="relative h-6 w-11 shrink-0 rounded-full transition-colors"
      style={{ background: checked ? 'var(--admin-accent)' : 'var(--admin-border)' }}
    >
      <span
        className="absolute top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform"
        style={{ transform: checked ? 'translateX(22px)' : 'translateX(2px)' }}
      />
    </button>
  )
}

export function SettingsPage() {
  const { theme, setTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const { storeId, user, logout } = useAuth()
  const navigate = useNavigate()

  const [dailyGoal, setDailyGoal] = useState(() => Number(localStorage.getItem(DAILY_GOAL_KEY)) || 150)
  const [notify, setNotify] = useState<NotifyPrefs>(loadNotifyPrefs)
  const [saved, setSaved] = useState(false)

  function persistNotify(next: NotifyPrefs) {
    setNotify(next)
    localStorage.setItem(NOTIFY_KEY, JSON.stringify(next))
  }

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

      <Card className="p-6">
        <div className="mb-1 flex items-center gap-2">
          <BellIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Уведомления</span>
        </div>
        <p className="mb-4 text-[11.5px] text-[color:var(--admin-text-tertiary)]">
          Бэкенд пока не отправляет уведомления — переключатели сохраняются только в этом браузере как заготовка под будущую фичу
        </p>
        <div className="flex flex-col divide-y divide-[color:var(--admin-border)]">
          <div className="flex items-center justify-between py-3.5">
            <div>
              <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">Низкий остаток товара</div>
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">Уведомлять, когда остаток ниже минимума</div>
            </div>
            <Toggle checked={notify.lowStock} onChange={(v) => persistNotify({ ...notify, lowStock: v })} />
          </div>
          <div className="flex items-center justify-between py-3.5">
            <div>
              <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">Ежедневный отчёт</div>
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">Присылать сводку продаж в конце дня</div>
            </div>
            <Toggle checked={notify.dailyReport} onChange={(v) => persistNotify({ ...notify, dailyReport: v })} />
          </div>
          <div className="flex items-center justify-between py-3.5">
            <div>
              <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">Отмены и возвраты</div>
              <div className="text-[11px] text-[color:var(--admin-text-tertiary)]">Уведомлять о каждой отменённой продаже</div>
            </div>
            <Toggle checked={notify.voids} onChange={(v) => persistNotify({ ...notify, voids: v })} />
          </div>
        </div>
      </Card>

      <Card className="p-6">
        <div className="mb-5 flex items-center gap-2">
          <KeyIcon width={18} height={18} className="text-[color:var(--admin-accent)]" />
          <span className="text-[16px] font-bold text-[color:var(--admin-text)]">Аккаунт</span>
        </div>
        <div className="flex items-center justify-between rounded-xl bg-[color:var(--admin-hover)] px-4 py-3">
          <div>
            <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
            <div className="mt-0.5 text-[11px] text-[color:var(--admin-text-tertiary)]">
              Смена пароля пока не поддерживается бэкендом
            </div>
          </div>
          <button
            onClick={logout}
            className="shrink-0 rounded-lg bg-[color:var(--admin-card)] px-3 py-1.5 text-[11px] font-semibold text-[#f87171] ring-1 ring-[color:var(--admin-border)] hover:bg-[#f87171]/10"
          >
            Выйти
          </button>
        </div>
      </Card>
    </div>
  )
}
