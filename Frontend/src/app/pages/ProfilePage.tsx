import { useEffect, useRef, useState, type FormEvent } from 'react'
import { meApi } from '../../lib/api'
import { useAuth } from '../../auth/AuthContext'
import { Button, ErrorState, LINE, Reveal, Skeleton, Spinner, TXT, useAsync } from '../ui'

function CameraIcon() {
  return (
    <svg width={16} height={16} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
      <path d="M23 19a2 2 0 01-2 2H3a2 2 0 01-2-2V8a2 2 0 012-2h4l2-3h6l2 3h4a2 2 0 012 2z"/>
      <circle cx="12" cy="13" r="4"/>
    </svg>
  )
}

const LANGS = [
  { value: 'ru', label: 'Русский' },
  { value: 'tg', label: 'Тоҷикӣ' },
  { value: 'en', label: 'English' },
]

export function ProfilePage() {
  const { user } = useAuth()
  const profile = useAsync(() => meApi.getProfile(), [])
  const fileRef = useRef<HTMLInputElement>(null)

  const [displayName, setDisplayName] = useState('')
  const [lang, setLang] = useState('ru')
  const [saving, setSaving] = useState(false)
  const [msg, setMsg] = useState('')
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null)
  const [avatarBusy, setAvatarBusy] = useState(false)
  const [avatarMsg, setAvatarMsg] = useState('')

  // Seed the form once the profile lands; the fields stay editable afterwards.
  useEffect(() => {
    if (!profile.data) return
    setDisplayName(profile.data.displayName ?? '')
    setLang(profile.data.preferredLanguage || 'ru')
  }, [profile.data])

  async function handleAvatarFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    if (file.size > 2 * 1024 * 1024) { setAvatarMsg('Максимальный размер: 2 МБ'); return }
    setAvatarPreview(URL.createObjectURL(file))
    setAvatarBusy(true)
    setAvatarMsg('')
    try {
      await meApi.uploadAvatar(file)
      setAvatarMsg('Аватар обновлён')
    } catch {
      setAvatarMsg('Не удалось загрузить')
      setAvatarPreview(null)
    } finally {
      setAvatarBusy(false)
      if (fileRef.current) fileRef.current.value = ''
    }
  }

  async function save(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setMsg('')
    try {
      await meApi.updateProfile(displayName.trim(), undefined, lang)
      setMsg('Сохранено')
      profile.reload()
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
        <Reveal i={1}>
          {/* Avatar upload */}
          <div className="mb-10 flex items-center gap-5">
            <button
              onClick={() => fileRef.current?.click()}
              disabled={avatarBusy}
              className="group relative h-[68px] w-[68px] shrink-0 overflow-hidden rounded-full transition-opacity hover:opacity-80 disabled:cursor-not-allowed disabled:opacity-50"
              style={{ background: 'var(--app-line)', outline: `2px solid ${LINE}`, outlineOffset: 3 }}
              title="Загрузить фото"
            >
              {avatarPreview ? (
                <img src={avatarPreview} alt="Аватар" className="h-full w-full object-cover" />
              ) : (
                <span className="flex h-full w-full items-center justify-center text-[24px] font-extrabold" style={{ color: TXT.primary }}>
                  {(user?.email ?? '?').charAt(0).toUpperCase()}
                </span>
              )}
              <span className="absolute inset-0 flex items-center justify-center opacity-0 transition-opacity group-hover:opacity-100" style={{ background: 'rgba(0,0,0,0.45)', color: 'white' }}>
                <CameraIcon />
              </span>
            </button>
            <input ref={fileRef} type="file" accept="image/jpeg,image/png,image/webp" className="hidden" onChange={handleAvatarFile} />
            <div>
              <button onClick={() => fileRef.current?.click()} disabled={avatarBusy} className="text-[13px] font-semibold text-[color:var(--app-text-primary)] hover:opacity-70 disabled:opacity-40">
                {avatarBusy ? 'Загружаем…' : 'Изменить фото'}
              </button>
              <div className="mt-0.5 text-[11.5px]" style={{ color: TXT.rest }}>JPEG, PNG или WebP · макс. 2 МБ</div>
              {avatarMsg && <div className="mt-0.5 text-[11.5px]" style={{ color: TXT.secondary }}>{avatarMsg}</div>}
            </div>
          </div>

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
      )}
    </>
  )
}
