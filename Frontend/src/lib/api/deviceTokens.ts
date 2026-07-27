import { apiFetch } from './client'

export type DevicePlatform = 'Android' | 'iOS' | 'Web'

export function registerDeviceToken(token: string, platform: DevicePlatform) {
  return apiFetch<{ deviceTokenId: number }>('/api/device-tokens', {
    method: 'POST',
    body: { token, platform },
  })
}
