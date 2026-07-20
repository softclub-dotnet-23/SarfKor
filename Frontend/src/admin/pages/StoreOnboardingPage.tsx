import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import { storesApi, ApiError } from '../../lib/api'
import { StoreIcon } from '../components/icons'

// Dushanbe city center — a sane default so the form is usable without
// waiting on geolocation permission.
const DEFAULT_LAT = 38.5598
const DEFAULT_LNG = 68.787

export function StoreOnboardingPage() {
  const { hasRole, storeId, setStoreId, refreshRoles, logout } = useAuth()
  const navigate = useNavigate()

  const [name, setName] = useState('')
  const [address, setAddress] = useState('')
  const [lat, setLat] = useState(DEFAULT_LAT)
  const [lng, setLng] = useState(DEFAULT_LNG)
  const [locating, setLocating] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const [manualStoreId, setManualStoreId] = useState('')

  function useMyLocation() {
    if (!navigator.geolocation) return
    setLocating(true)
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLat(pos.coords.latitude)
        setLng(pos.coords.longitude)
        setLocating(false)
      },
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
      // Creating a store grants StorePartner server-side, but the JWT already
      // in hand doesn't carry that claim yet — refresh to get a token that does.
      await refreshRoles()
      navigate('/admin', { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось создать магазин')
    } finally {
      setLoading(false)
    }
  }

  function handleUseManualId(e: FormEvent) {
    e.preventDefault()
    const id = Number(manualStoreId)
    if (!Number.isFinite(id) || id <= 0) {
      setError('Введите корректный ID магазина')
      return
    }
    setStoreId(id)
    navigate('/admin', { replace: true })
  }

  const alreadyPartnerWithoutStore = hasRole('StorePartner') && storeId === null
  const canPickManually = hasRole('StorePartner')

  return (
    <div className="admin-shell flex min-h-screen items-center justify-center bg-[color:var(--admin-content)] p-6 text-[color:var(--admin-text)]">
      <div className="w-full max-w-md rounded-[22px] bg-[color:var(--admin-card)] p-8 ring-1 ring-[color:var(--admin-border)]">
        <span
          className="mb-5 grid h-12 w-12 place-items-center rounded-2xl text-white"
          style={{ background: 'linear-gradient(135deg,#38bdf8,#0ea5e9)' }}
        >
          <StoreIcon width={22} height={22} />
        </span>

        {canPickManually && (
          <div className="mb-6 rounded-xl bg-[#fbbf2418] p-4 text-[12.5px] leading-relaxed text-[color:var(--admin-text-secondary)]">
            {alreadyPartnerWithoutStore
              ? 'У вашего аккаунта уже есть права партнёра, но в этом браузере не сохранён ID вашего магазина — сервер пока не даёт способа получить список своих магазинов.'
              : 'Если у вас несколько магазинов, сервер пока не даёт способа получить их список.'}{' '}
            Если вы знаете ID нужного магазина, введите его ниже, либо создайте новый.
          </div>
        )}

        {canPickManually && (
          <form onSubmit={handleUseManualId} className="mb-6 flex gap-2 border-b border-[color:var(--admin-border)] pb-6">
            <input
              value={manualStoreId}
              onChange={(e) => setManualStoreId(e.target.value)}
              placeholder="ID магазина"
              inputMode="numeric"
              className="flex-1 rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
            />
            <button
              type="submit"
              className="shrink-0 rounded-xl bg-[color:var(--admin-hover)] px-4 py-2.5 text-[13px] font-semibold text-[color:var(--admin-text)] hover:bg-[color:var(--admin-border)]"
            >
              Продолжить
            </button>
          </form>
        )}

        <h1 className="mb-1.5 text-[20px] font-extrabold tracking-tight">Создайте свой магазин</h1>
        <p className="mb-6 text-[13px] text-[color:var(--admin-text-tertiary)]">
          Это займёт минуту — после создания сразу откроется панель управления
        </p>

        <form onSubmit={handleCreate} className="flex flex-col gap-4">
          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Название магазина</span>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Магазин «Дилшод»"
              className="rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]"
            />
          </label>

          <label className="flex flex-col gap-1.5">
            <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Адрес</span>
            <input
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              placeholder="ул. Рудаки, 123, Душанбе"
              className="rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none placeholder:text-[color:var(--admin-text-tertiary)] focus:border-[color:var(--admin-accent)]"
            />
          </label>

          <div className="grid grid-cols-2 gap-3">
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Широта</span>
              <input
                type="number"
                step="0.0001"
                value={lat}
                onChange={(e) => setLat(Number(e.target.value))}
                className="rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className="text-[12px] font-medium text-[color:var(--admin-text-secondary)]">Долгота</span>
              <input
                type="number"
                step="0.0001"
                value={lng}
                onChange={(e) => setLng(Number(e.target.value))}
                className="rounded-xl border border-[color:var(--admin-border)] bg-[color:var(--admin-hover)] px-3.5 py-2.5 text-[13px] text-[color:var(--admin-text)] outline-none focus:border-[color:var(--admin-accent)]"
              />
            </label>
          </div>

          <button
            type="button"
            onClick={useMyLocation}
            disabled={locating}
            className="self-start text-[12px] font-semibold text-[color:var(--admin-accent)] hover:opacity-80 disabled:opacity-50"
          >
            {locating ? 'Определяем…' : 'Определить моё местоположение'}
          </button>

          {error && (
            <div className="rounded-lg bg-[#f8717118] px-3.5 py-2.5 text-[12.5px] font-medium text-[#f87171]">{error}</div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="mt-1 rounded-xl bg-[color:var(--admin-accent)] py-3 text-[14px] font-bold text-white transition-transform hover:scale-[1.01] active:scale-[0.98] disabled:opacity-60"
          >
            {loading ? 'Создаём…' : 'Создать магазин'}
          </button>
        </form>

        <button
          onClick={logout}
          className="mt-5 w-full text-center text-[12px] font-medium text-[color:var(--admin-text-tertiary)] hover:text-[color:var(--admin-text)]"
        >
          Выйти из аккаунта
        </button>
      </div>
    </div>
  )
}
