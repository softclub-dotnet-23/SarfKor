import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Panel, SectionHeader } from '../cabinet/components/primitives'
import { useTheme } from '../../theme/ThemeProvider'
import { useThemeTransition } from '../../theme/useThemeTransition'
import { useAuth } from '../../auth/AuthContext'
import { SunIcon, MoonIcon } from '../../components/icons'
import { CheckIcon } from '../components/icons'
import { SuppliersSection } from './SuppliersSection'

const DAILY_GOAL_KEY = 'sarfkor-daily-goal'

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
    <div className="mx-auto flex max-w-[820px] flex-col gap-5">
      <Panel>
        <SectionHeader title="Магазин" />
        <div className="mb-5 flex items-center justify-between rounded-2xl bg-[color:var(--admin-hover)] px-4 py-3.5">
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
            className="flex items-center justify-center gap-2 self-start rounded-full bg-[color:var(--admin-text)] px-5 py-2.5 text-[13px] font-bold text-[color:var(--admin-content)] transition-transform hover:scale-[1.02] active:scale-[0.98]"
          >
            {saved ? <CheckIcon width={15} height={15} /> : null}
            {saved ? 'Сохранено' : 'Сохранить изменения'}
          </button>
        </form>
      </Panel>

      <Panel>
        <SectionHeader title="Оформление" />
        <div className="flex gap-2 rounded-full bg-[color:var(--admin-hover)] p-1.5">
          <button
            onClick={(e) => runThemeTransition(e.currentTarget, () => setTheme('light'))}
            className={`flex flex-1 items-center justify-center gap-2 rounded-full py-2.5 text-[13px] font-semibold transition-colors duration-200 ${
              theme === 'light'
                ? 'bg-[color:var(--admin-card)] text-[color:var(--admin-text)] [box-shadow:var(--admin-shadow)]'
                : 'text-[color:var(--admin-text-tertiary)]'
            }`}
          >
            <SunIcon width={16} height={16} />
            Светлая
          </button>
          <button
            onClick={(e) => runThemeTransition(e.currentTarget, () => setTheme('dark'))}
            className={`flex flex-1 items-center justify-center gap-2 rounded-full py-2.5 text-[13px] font-semibold transition-colors duration-200 ${
              theme === 'dark'
                ? 'bg-[color:var(--admin-card)] text-[color:var(--admin-text)] [box-shadow:var(--admin-shadow)]'
                : 'text-[color:var(--admin-text-tertiary)]'
            }`}
          >
            <MoonIcon width={16} height={16} />
            Тёмная
          </button>
        </div>
      </Panel>

      <SuppliersSection storeId={storeId} />

      <Panel>
        <SectionHeader title="Аккаунт" />
        <div className="flex items-center justify-between rounded-2xl bg-[color:var(--admin-hover)] px-4 py-3.5">
          <div>
            <div className="text-[13px] font-semibold text-[color:var(--admin-text)]">{user?.email}</div>
            <div className="mt-0.5 text-[11px] text-[color:var(--admin-text-tertiary)]">
              Смена пароля пока не поддерживается бэкендом
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
