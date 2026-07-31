import { useEffect, useState, type FormEvent } from 'react'
import { meApi } from '../../lib/api'
import { useAuth } from '../../auth/AuthContext'
import { Button, ErrorState, LINE, Reveal, Skeleton, Spinner, TXT, useAsync } from '../ui'

const LANGS = [
  { value: 'ru', label: 'Русский' },
  { value: 'tg', label: 'Тоҷикӣ' },
  { value: 'en', label: 'English' },
]

export function ProfilePage() {
  const { user } = useAuth()
  const profile = useAsync(() => meApi.getProfile(), [])

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
                className="border-0 bg-transparent pb-3 text-[16px] text-white caret-white placeholder:text-white/25"
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
                className="border-0 bg-transparent pb-3 text-[16px] text-white"
                style={{ borderBottom: `1px solid ${LINE}`, colorScheme: 'dark' }}
              >
                {LANGS.map((l) => (
                  <option key={l.value} value={l.value} style={{ background: '#000' }}>
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
