import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { meApi, ApiError } from '../../lib/api'
import { useAuth } from '../../auth/AuthContext'
import { useProfile as useSharedProfile } from '../../lib/useProfile'
import { useAvatarUrl } from '../../lib/useAvatarUrl'
import { Button, ErrorState, LINE, Reveal, Skeleton, Spinner, TXT, useAsync } from '../ui'

const LANGS = [
  { value: 'ru', label: 'Русский' },
  { value: 'tg', label: 'Тоҷикӣ' },
  { value: 'en', label: 'English' },
]

const MAX_AVATAR_BYTES = 2 * 1024 * 1024

function initials(email: string) {
  return (email.trim()[0] ?? '?').toUpperCase()
}

function AvatarEditor({ hasAvatar, onUploaded }: { hasAvatar: boolean; onUploaded: () => void }) {
  const { user } = useAuth()
  const [version, setVersion] = useState(0)
  const avatarUrl = useAvatarUrl(hasAvatar, version)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const fileInputRef = useRef<HTMLInputElement>(null)

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
      setVersion((v) => v + 1)
      onUploaded()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось загрузить фото')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mb-10 flex items-center gap-5">
      <div
        className="grid h-16 w-16 shrink-0 place-items-center overflow-hidden rounded-full text-[20px] font-bold"
        style={{ background: 'var(--app-line)', color: TXT.primary }}
      >
        {avatarUrl ? <img src={avatarUrl} alt="" className="h-full w-full object-cover" /> : initials(user?.email ?? '')}
      </div>
      <div>
        <input ref={fileInputRef} type="file" accept="image/jpeg,image/png" className="hidden" onChange={handleFileChange} />
        <Button type="button" variant="ghost" className="!px-4 !py-2 text-[12.5px]" onClick={() => fileInputRef.current?.click()} disabled={busy}>
          {busy && <Spinner />}
          {busy ? 'Загружаем…' : 'Изменить фото'}
        </Button>
        {error && (
          <p className="mt-2 text-[12px]" style={{ color: '#e05252' }}>
            {error}
          </p>
        )}
      </div>
    </div>
  )
}

function ChangePasswordSection() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [msg, setMsg] = useState('')

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setMsg('')
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
      setMsg('Пароль изменён. Другие сессии выйдут из аккаунта.')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Не удалось сменить пароль')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Reveal i={2}>
      <form onSubmit={handleSubmit} className="mt-14 flex max-w-[420px] flex-col gap-9">
        <p className="text-[10px] font-bold uppercase tracking-[0.28em]" style={{ color: TXT.rest }}>
          Пароль
        </p>

        {(['Текущий пароль', 'Новый пароль', 'Повторите новый пароль'] as const).map((label, i) => {
          const value = i === 0 ? currentPassword : i === 1 ? newPassword : confirmPassword
          const setValue = i === 0 ? setCurrentPassword : i === 1 ? setNewPassword : setConfirmPassword
          return (
            <label key={label} className="flex flex-col">
              <span className="mb-2.5 text-[10px] font-bold uppercase tracking-[0.16em]" style={{ color: TXT.rest }}>
                {label}
              </span>
              <input
                type="password"
                autoComplete={i === 0 ? 'current-password' : 'new-password'}
                value={value}
                onChange={(e) => setValue(e.target.value)}
                minLength={i === 0 ? undefined : 8}
                required
                className="border-0 bg-transparent pb-3 text-[16px] text-[color:var(--app-text-primary)] caret-[color:var(--app-text-primary)]"
                style={{ borderBottom: `1px solid ${LINE}` }}
              />
            </label>
          )
        })}

        <div className="flex items-center gap-5">
          <Button type="submit" disabled={busy}>
            {busy && <Spinner dark />}
            Сменить пароль
          </Button>
          {(msg || error) && (
            <span className="text-[13px]" style={{ color: error ? '#e05252' : TXT.secondary }}>
              {error || msg}
            </span>
          )}
        </div>
      </form>
    </Reveal>
  )
}

export function ProfilePage() {
  const { user } = useAuth()
  const profile = useAsync(() => meApi.getProfile(), [])
  // Separate from `profile` above: that's this page's own loading/error-tracked fetch, this is
  // the shared instance AppShell's header avatar reads from. Both are reloaded after a mutation
  // so the header updates immediately instead of waiting for a future page load.
  const { reload: reloadSharedProfile } = useSharedProfile()

  const [displayName, setDisplayName] = useState('')
  const [lang, setLang] = useState('ru')
  const [saving, setSaving] = useState(false)
  const [msg, setMsg] = useState('')

  // Seed the form once the profile lands; the fields stay editable afterwards.
  useEffect(() => {
    if (!profile.data) return
    setDisplayName(profile.data.displayName ?? '')
    setLang(profile.data.preferredLanguage || 'ru')
  }, [profile.data])

  async function save(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setMsg('')
    try {
      // Threads the existing avatarReference through unchanged — this PUT replaces the whole
      // profile row, so passing undefined here would silently wipe out whatever AvatarEditor
      // (POST /api/me/avatar, a separate endpoint) had just set.
      await meApi.updateProfile(displayName.trim(), profile.data?.avatarReference, lang)
      setMsg('Сохранено')
      profile.reload()
      reloadSharedProfile()
    } catch {
      setMsg('Не удалось сохранить')
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <Reveal>
        <p
          className="mb-5 text-[10px] font-bold uppercase tracking-[0.28em]"
          style={{ color: TXT.rest }}
        >
          Профиль
        </p>
        <h1 className="mb-3 text-[clamp(30px,4.6vw,46px)] font-extrabold leading-[1.04] tracking-[-0.04em]">
          Ваш аккаунт
        </h1>
        <p className="mb-12 text-[14px]" style={{ color: TXT.secondary }}>
          {user?.email}
        </p>
      </Reveal>

      {profile.loading && (
        <div className="flex flex-col gap-8">
          <Skeleton h={52} />
          <Skeleton h={52} />
          <Skeleton h={44} w={160} className="rounded-full" />
        </div>
      )}

      {profile.error && <ErrorState message={profile.error} onRetry={profile.reload} />}

      {!profile.loading && !profile.error && (
        <>
          <Reveal i={1}>
            <AvatarEditor
              hasAvatar={!!profile.data?.avatarReference}
              onUploaded={() => {
                profile.reload()
                reloadSharedProfile()
              }}
            />

            <form onSubmit={save} className="flex max-w-[420px] flex-col gap-9">
              <label className="flex flex-col">
                <span
                  className="mb-2.5 text-[10px] font-bold uppercase tracking-[0.16em]"
                  style={{ color: TXT.rest }}
                >
                  Отображаемое имя
                </span>
                <input
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  maxLength={100}
                  placeholder="Как к вам обращаться"
                  className="border-0 bg-transparent pb-3 text-[16px] text-[color:var(--app-text-primary)] caret-[color:var(--app-text-primary)] placeholder:text-[color:var(--app-text-rest)]"
                  style={{ borderBottom: `1px solid ${LINE}` }}
                />
              </label>

              <label className="flex flex-col">
                <span
                  className="mb-2.5 text-[10px] font-bold uppercase tracking-[0.16em]"
                  style={{ color: TXT.rest }}
                >
                  Язык
                </span>
                <select
                  value={lang}
                  onChange={(e) => setLang(e.target.value)}
                  className="border-0 bg-transparent pb-3 text-[16px] text-[color:var(--app-text-primary)]"
                  style={{ borderBottom: `1px solid ${LINE}` }}
                >
                  {LANGS.map((l) => (
                    <option key={l.value} value={l.value} style={{ background: 'var(--bg-app)' }}>
                      {l.label}
                    </option>
                  ))}
                </select>
              </label>

              <div className="flex items-center gap-5">
                <Button type="submit" disabled={saving}>
                  {saving && <Spinner dark />}
                  Сохранить
                </Button>
                {msg && (
                  <span className="text-[13px]" style={{ color: TXT.secondary }}>
                    {msg}
                  </span>
                )}
              </div>
            </form>
          </Reveal>

          <ChangePasswordSection />
        </>
      )}
    </>
  )
}
