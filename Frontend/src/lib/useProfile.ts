import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { createElement } from 'react'
import { meApi, type UserProfile } from './api'

interface ProfileState {
  profile: UserProfile | null
  reload: () => void
}

/**
 * Context-backed rather than a bare per-caller hook: the header avatar (rendered by
 * AdminLayout/AppShell) and the avatar-upload form (rendered inside their <Outlet>, on a
 * different page) both need to see the *same* profile object. A per-caller useState would give
 * each its own copy — uploading a new avatar from the Settings page would never update the
 * header until a full page reload, which reads as a broken/laggy save. One instance per shell,
 * shared by context, means ProfilePage/SettingsPage's `reload()` updates the header instantly.
 */
const ProfileContext = createContext<ProfileState | null>(null)

export function ProfileProvider({ children }: { children: ReactNode }) {
  const [profile, setProfile] = useState<UserProfile | null>(null)

  const reload = useCallback(() => {
    meApi.getProfile().then(setProfile).catch(() => setProfile(null))
  }, [])

  useEffect(() => {
    reload()
  }, [reload])

  return createElement(ProfileContext.Provider, { value: { profile, reload } }, children)
}

export function useProfile() {
  const ctx = useContext(ProfileContext)
  if (!ctx) throw new Error('useProfile must be used within a ProfileProvider (AdminLayout/AppShell)')
  return ctx
}
