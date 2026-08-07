import { createContext, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { meApi } from '../lib/api'
import { getTokens } from '../lib/api/client'

// Market languages only — Russian and Tajik (see the task this shipped under: "Языки: русский и
// таджикский"). English was dropped from the switcher on purpose, not an oversight.
export type Language = 'ru' | 'tg'

const STORAGE_KEY = 'sarfkor-language'

const LANGUAGES: { value: Language; label: string }[] = [
  { value: 'ru', label: 'RU' },
  { value: 'tg', label: 'TJ' },
]

function isLanguage(v: unknown): v is Language {
  return v === 'ru' || v === 'tg'
}

function getInitialLanguage(): Language {
  if (typeof window === 'undefined') return 'ru'
  const stored = window.localStorage.getItem(STORAGE_KEY)
  return isLanguage(stored) ? stored : 'ru'
}

interface LanguageContextValue {
  language: Language
  setLanguage: (language: Language) => void
  options: typeof LANGUAGES
}

const LanguageContext = createContext<LanguageContextValue | null>(null)

/**
 * Single source of truth for the active UI language, shared by every authenticated shell (platform
 * Admin, StorePartner, Cashier) and the public site. localStorage is only a fast paint cache —
 * mounted above AuthProvider (main.tsx) so it works for anonymous/public pages too, but the real
 * source of truth for a signed-in user is their server-side profile (UserProfile.PreferredLanguage,
 * already wired end-to-end on the backend): on load, if a token exists, the server value overwrites
 * whatever localStorage had (picks up a change made on another device); on every setLanguage call
 * while signed in, the new value is pushed back to the server, fire-and-forget.
 */
export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<Language>(getInitialLanguage)
  // Guards against clobbering a profile fetched mid-flight with a stale full-profile PUT (the
  // profile endpoint is a full replace, not a PATCH — see persistLanguage below).
  const profileRef = useRef<{ displayName: string; avatarReference?: string } | null>(null)

  useEffect(() => {
    if (!getTokens()) return
    let cancelled = false
    meApi
      .getProfile()
      .then((profile) => {
        if (cancelled || !profile.found) return
        profileRef.current = { displayName: profile.displayName ?? '', avatarReference: profile.avatarReference }
        if (isLanguage(profile.preferredLanguage) && profile.preferredLanguage !== language) {
          window.localStorage.setItem(STORAGE_KEY, profile.preferredLanguage)
          setLanguageState(profile.preferredLanguage)
        }
      })
      .catch(() => {
        // No profile yet / request failed — localStorage's value (or the 'ru' default) stands.
      })
    return () => {
      cancelled = true
    }
    // Intentionally once per mount (per sign-in), not per language change — this effect exists to
    // pull the server's value in, setLanguage below is what pushes local changes back out.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const value = useMemo<LanguageContextValue>(
    () => ({
      language,
      options: LANGUAGES,
      setLanguage: (l: Language) => {
        window.localStorage.setItem(STORAGE_KEY, l)
        setLanguageState(l)
        // updateProfile is a full replace, not a patch — only fire it once the profile fetch above
        // has actually told us a real displayName to carry through; an empty one would both fail
        // NotEmpty validation server-side and, if it ever didn't, would wipe a real display name.
        const known = profileRef.current
        if (!getTokens() || !known) return
        meApi.updateProfile(known.displayName, known.avatarReference, l).catch(() => {
          // Best-effort — the switch already applied locally; a failed save just means it may not
          // follow the user to their next device until they change it again or a later save succeeds.
        })
      },
    }),
    [language],
  )

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>
}

export function useLanguage() {
  const ctx = useContext(LanguageContext)
  if (!ctx) throw new Error('useLanguage must be used within LanguageProvider')
  return ctx
}
