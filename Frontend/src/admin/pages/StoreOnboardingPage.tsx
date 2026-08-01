import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import { storesApi, ApiError } from '../../lib/api'
import { StoreIcon } from '../components/icons'

// Dushanbe city center — a sane default so the form is usable without
// waiting on geolocation permission.
const DEFAULT_LAT = 38.5598
const DEFAULT_LNG = 68.787

export function StoreOnboardingPage() {
  const { hasRole, storeId, myStores, setStoreId, refreshRoles, refreshMyStores, logout } = useAuth()
  const navigate = useNavigate()

  const [name, setName] = useState('')
  const [address, setAddress] = useState('')
  const [lat, setLat] = useState(DEFAULT_LAT)
  const [lng, setLng] = useState(DEFAULT_LNG)
  const [locating, setLocating] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [storesLoading, setStoresLoading] = useState(false)

  const [manualStoreId, setManualStoreId] = useState('')
  const [showManualEntry, setShowManualEntry] = useState(false)

  // A StorePartner landing here means GET /api/me/stores either hasn't run yet for this
  // session or came back with 2+ stores (a single match is auto-adopted by AuthContext,
  // which skips this screen entirely) — refresh once so a stale/empty list doesn't strand
  // a real multi-store owner on the "create a new store" form.
  useEffect(() => {
    if (!hasRole('StorePartner') || myStores !== null) return
    setStoresLoading(true)
    refreshMyStores().finally(() => setStoresLoading(false))
  }, [hasRole, myStores, refreshMyStores])

  function pickStore(pickedStoreId: number) {
    setStoreId(pickedStoreId)
    navigate('/admin', { replace: true })
  }

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
  const hasPickableStores = !!myStores && myStores.length > 0

  return (
    <div className="admin-shell flex min-h-screen items-center justify-center bg-[color:var(--admin-content)] p-6 text-[color:var(--admin-text)]">
      <div className="w-full max-w-md rounded-[22px] bg-[color:var(--admin-card)] p-8 ring-1 ring-[color:var(--admin-border)] [box-shadow:var(--admin-shadow-lift)]">
        <span
          className="mb-5 grid h-12 w-12 place-items-center rounded-2xl text-white"
          style={{
            background:
              'linear-gradient(135deg, var(--admin-accent), color-mix(in srgb, var(--admin-accent) 65%, black))',
          }}
        >
          <StoreIcon width={22} height={22} />
        </span>

        {storesLoading && (
          <div className="mb-6 text-[13px] text-[color:var(--admin-text-tertiary)]">Ищем ваши магазины…</div>
        )}

        {hasPickableStores && (
          <div className="mb-6 border-b border-[color:var(--admin-border)] pb-6">
            <h2 className="mb-1 text-[15px] font-bold text-[color:var(--admin-text)]">Выберите магазин</h2>
            <p className="mb-3 text-[12.5px] text-[color:var(--admin-text-tertiary)]">
              Этот браузер их ещё не запоминал — выберите, с каким работать сейчас.
            </p>
            <div className="flex flex-col gap-2">
              {myStores!.map((s) => (
                <button
                  key={s.storeId}
                  onClick={() => pickStore(s.storeId)}
                  className="flex items-center justify-between gap-3 rounded-xl bg-[color:var(--admin-hover)] px-4 py-3 text-left hover:bg-[color:var(--admin-border)]"
                >
                  <span className="min-w-0 truncate text-[13.5px] font-semibold text-[color:var(--admin-text)]">{s.name}</span>
                  <span className="shrink-0 rounded-full bg-[color:var(--admin-accent-soft)] px-2.5 py-1 text-[11px] font-semibold text-[color:var(--admin-accent)]">
                    {s.role === 'Owner' ? 'Владелец' : 'Кассир'}
                  </span>
                </button>
              ))}
            </div>
          </div>
        )}

        {alreadyPartnerWithoutStore && !hasPickableStores && !storesLoading && (
          <div className="mb-6 rounded-xl bg-[color:var(--admin-warning-dim)] p-4 text-[12.5px] leading-relaxed text-[color:var(--admin-text-secondary)]">
            У вашего аккаунта есть права партнёра, но за ним пока не числится ни одного магазина. Создайте новый ниже
            {!showManualEntry && (
              <>
                {' '}
                или{' '}
                <button type="button" onClick={() => setShowManualEntry(true)} className="font-semibold text-[color:var(--admin-accent)] underline">
                  введите ID вручную
                </button>
              </>
            )}
            .
          </div>
        )}

        {showManualEntry && (
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

        <h1 className="mb-1.5 text-[20px] font-extrabold tracking-tight">
          {hasPickableStores ? 'Или создайте ещё один' : 'Создайте свой магазин'}
        </h1>
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
            <div className="rounded-lg bg-[color:var(--admin-danger-dim)] px-3.5 py-2.5 text-[12.5px] font-medium text-[color:var(--admin-danger)]">{error}</div>
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
