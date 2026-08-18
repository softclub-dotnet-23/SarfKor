import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import { storesApi } from '../../lib/api'
import { describeError } from '../../lib/errorKind'
import { useT } from '../../i18n/translations'
import { LogoMark } from '../../components/Logo'
import { useTheme } from '../../theme/ThemeProvider'
import { useThemeTransition } from '../../theme/useThemeTransition'
import { SunIcon, MoonIcon } from '../../components/icons'

const DEFAULT_LAT = 38.5598
const DEFAULT_LNG = 68.787

export function StoreOnboardingPage() {
  const { hasRole, storeId, myStores, setStoreId, refreshRoles, refreshMyStores, logout } = useAuth()
  const navigate = useNavigate()
  const t = useT()
  const { theme, toggleTheme } = useTheme()
  const { runThemeTransition } = useThemeTransition()
  const isDark = theme === 'dark'

  const [name, setName] = useState('')
  const [address, setAddress] = useState('')
  const [lat, setLat] = useState(DEFAULT_LAT)
  const [lng, setLng] = useState(DEFAULT_LNG)
  const [locating, setLocating] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [storesLoading, setStoresLoading] = useState(false)

  useEffect(() => {
    if (!hasRole('StorePartner') || myStores !== null) return
    setStoresLoading(true)
    refreshMyStores().finally(() => setStoresLoading(false))
  }, [hasRole, myStores, refreshMyStores])

  function pickStore(id: number) {
    setStoreId(id)
    navigate('/admin', { replace: true })
  }

  function useMyLocation() {
    if (!navigator.geolocation) return
    setLocating(true)
    navigator.geolocation.getCurrentPosition(
      (pos) => { setLat(pos.coords.latitude); setLng(pos.coords.longitude); setLocating(false) },
      () => setLocating(false),
      { timeout: 8000 },
    )
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    setError('')
    if (!name.trim() || !address.trim()) {
      setError('Заполните название и адрес магазина')
      return
    }
    setLoading(true)
    try {
      const result = await storesApi.createStore({ name: name.trim(), address: address.trim(), latitude: lat, longitude: lng })
      setStoreId(result.storeId)
      await refreshRoles()
      // myStores must include the new store before navigating, or RequireStore sees
      // storeId set but myStores still empty, calls that stale, and bounces back here.
      await refreshMyStores()
      navigate('/admin', { replace: true })
    } catch (err) {
      setError(describeError(err, t))
    } finally {
      setLoading(false)
    }
  }

  const alreadyPartnerWithoutStore = hasRole('StorePartner') && storeId === null
  const hasPickableStores = !!myStores && myStores.length > 0

  return (
    <div className="admin-shell flex min-h-screen flex-col bg-[color:var(--admin-content)] text-[color:var(--admin-text)]">
      {/* Top bar */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-[color:var(--admin-border)]">
        <div className="flex items-center gap-2.5">
          <LogoMark size={24} />
          <span className="text-[15px] font-extrabold tracking-tight">Sarfkor</span>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={(e) => runThemeTransition(e.currentTarget, toggleTheme)}
            aria-label="Переключить тему"
            className="grid h-8 w-8 place-items-center rounded-full text-[color:var(--admin-text-secondary)] hover:bg-[color:var(--admin-hover)] transition-colors"
          >
            {isDark ? <SunIcon width={15} height={15} /> : <MoonIcon width={15} height={15} />}
          </button>
          <button
            onClick={logout}
            className="text-[12px] font-medium text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)] transition-colors"
          >
            Выйти
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="flex flex-1 items-center justify-center p-6">
        <div
          className="w-full max-w-[440px] rounded-[28px] border border-[color:var(--admin-border)] bg-[color:var(--admin-card)] p-8"
          style={{ boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.06), var(--admin-shadow-lift)' }}
        >
          {/* Header */}
          <div className="mb-8">
            <div className="text-[10.5px] font-bold uppercase tracking-[0.16em] text-[color:var(--admin-text-tertiary)] mb-2">
              Sarfkor / Партнёр
            </div>
            <h1 className="text-[28px] font-black tracking-tighter text-[color:var(--admin-text)]">
              {hasPickableStores ? 'Выберите магазин' : 'Создайте магазин'}
            </h1>
            <p className="mt-1.5 text-[13px] text-[color:var(--admin-text-secondary)]">
              {hasPickableStores
                ? 'У вас уже есть магазины — выберите один или создайте новый'
                : 'Это займёт минуту — после этого сразу откроется панель управления'}
            </p>
          </div>

          {storesLoading && (
            <div className="mb-6 text-[12.5px] text-[color:var(--admin-text-tertiary)]">Ищем ваши магазины…</div>
          )}

          {/* Existing stores picker */}
          {hasPickableStores && (
            <div className="mb-6">
              <div className="flex flex-col gap-2">
                {myStores!.map((s) => (
                  <button
                    key={s.storeId}
                    onClick={() => pickStore(s.storeId)}
                    className="group flex items-center justify-between gap-3 rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-4 py-3.5 text-left transition-all hover:border-[color:var(--admin-border-strong)] hover:bg-[color:var(--admin-card)]"
                  >
                    <div>
                      <div className="text-[14px] font-bold text-[color:var(--admin-text)]">{s.name}</div>
                    </div>
                    <span className="shrink-0 rounded-full border border-[color:var(--admin-border)] px-2.5 py-1 text-[10.5px] font-semibold text-[color:var(--admin-text-secondary)]">
                      {s.role === 'Owner' ? 'Владелец' : 'Кассир'}
                    </span>
                  </button>
                ))}
              </div>

              <div className="my-6 flex items-center gap-3">
                <div className="h-px flex-1 bg-[color:var(--admin-border)]" />
                <span className="text-[11px] text-[color:var(--admin-text-tertiary)]">или</span>
                <div className="h-px flex-1 bg-[color:var(--admin-border)]" />
              </div>

              <h2 className="mb-5 text-[16px] font-extrabold tracking-tight text-[color:var(--admin-text)]">
                Создать ещё один
              </h2>
            </div>
          )}

          {alreadyPartnerWithoutStore && !hasPickableStores && !storesLoading && (
            <div className="mb-6 rounded-2xl border border-[color:var(--admin-warning-dim)] bg-[color:var(--admin-warning-dim)] px-4 py-3 text-[12.5px] leading-relaxed text-[color:var(--admin-text-secondary)]">
              У аккаунта есть права партнёра, но магазин не найден — создайте новый ниже.
            </div>
          )}

          {/* Create form */}
          <form onSubmit={handleCreate} className="flex flex-col gap-4">
            <label className="flex flex-col gap-1.5">
              <span className="text-[11px] font-semibold uppercase tracking-[0.10em] text-[color:var(--admin-text-tertiary)]">
                Название
              </span>
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Магазин «Дилшод»"
                className="rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-4 py-3 text-[14px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)] transition-colors"
              />
            </label>

            <label className="flex flex-col gap-1.5">
              <span className="text-[11px] font-semibold uppercase tracking-[0.10em] text-[color:var(--admin-text-tertiary)]">
                Адрес
              </span>
              <input
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                placeholder="ул. Рудаки, 123, Душанбе"
                className="rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-4 py-3 text-[14px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-border-strong)] transition-colors"
              />
            </label>

            <div className="grid grid-cols-2 gap-3">
              <label className="flex flex-col gap-1.5">
                <span className="text-[11px] font-semibold uppercase tracking-[0.10em] text-[color:var(--admin-text-tertiary)]">Широта</span>
                <input
                  type="number"
                  step="0.0001"
                  value={lat}
                  onChange={(e) => setLat(Number(e.target.value))}
                  className="rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-4 py-3 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-border-strong)] transition-colors"
                />
              </label>
              <label className="flex flex-col gap-1.5">
                <span className="text-[11px] font-semibold uppercase tracking-[0.10em] text-[color:var(--admin-text-tertiary)]">Долгота</span>
                <input
                  type="number"
                  step="0.0001"
                  value={lng}
                  onChange={(e) => setLng(Number(e.target.value))}
                  className="rounded-2xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-4 py-3 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-border-strong)] transition-colors"
                />
              </label>
            </div>

            <button
              type="button"
              onClick={useMyLocation}
              disabled={locating}
              className="self-start text-[12px] font-semibold text-[color:var(--admin-text-secondary)] underline underline-offset-2 hover:text-[color:var(--admin-text)] disabled:opacity-40 transition-colors"
            >
              {locating ? 'Определяем…' : '↗ Использовать моё местоположение'}
            </button>

            {error && (
              <div className="rounded-2xl border border-[color:var(--admin-danger-dim)] bg-[color:var(--admin-danger-dim)] px-4 py-3 text-[12.5px] font-medium text-[color:var(--admin-danger)]">
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="mt-1 rounded-2xl bg-[color:var(--admin-accent)] py-3.5 text-[14px] font-bold text-[color:var(--admin-accent-fg)] transition-all hover:opacity-90 active:scale-[0.98] disabled:opacity-40"
            >
              {loading ? 'Создаём…' : 'Создать магазин →'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
