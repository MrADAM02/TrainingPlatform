interface AuthTokens {
  accessToken: string
  accessTokenExpiresAtUtc: string
  refreshToken: string
  refreshTokenExpiresAtUtc: string
}

interface DecodedUser {
  id: string
  email: string
  fullName: string
  roles: string[]
}

function decodeUser(accessToken: string | null | undefined): DecodedUser | null {
  if (!accessToken) return null

  try {
    const payloadSegment = accessToken.split('.')[1]
    if (!payloadSegment) return null

    const base64 = payloadSegment.replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => `%${c.charCodeAt(0).toString(16).padStart(2, '0')}`)
        .join(''),
    )
    const payload = JSON.parse(json)

    const role = payload.role as string | string[] | undefined
    const roles = Array.isArray(role) ? role : role ? [role] : []

    return {
      id: payload.sub,
      email: payload.email,
      fullName: payload.full_name,
      roles,
    }
  } catch {
    return null
  }
}

const cookieOptions = {
  sameSite: 'strict' as const,
  secure: import.meta.env.PROD,
  path: '/',
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = useCookie<string | null>('tp_access_token', { ...cookieOptions, default: () => null })
  const refreshToken = useCookie<string | null>('tp_refresh_token', { ...cookieOptions, default: () => null })

  const user = computed(() => decodeUser(accessToken.value))
  const isAuthenticated = computed(() => user.value !== null)

  function hasRole(role: string): boolean {
    return user.value?.roles.includes(role) ?? false
  }

  function setTokens(tokens: AuthTokens) {
    accessToken.value = tokens.accessToken
    refreshToken.value = tokens.refreshToken
  }

  function clearTokens() {
    accessToken.value = null
    refreshToken.value = null
  }

  async function login(email: string, password: string) {
    const config = useRuntimeConfig()
    const tokens = await $fetch<AuthTokens>('/auth/login', {
      baseURL: config.public.apiBase,
      method: 'POST',
      body: { email, password },
    })
    setTokens(tokens)
  }

  // Concurrent requests can 401 at the same time; without de-duplication each would call
  // refresh with the same (soon-to-be-rotated) token, and the backend's reuse-detection would
  // treat the second call as token theft and revoke the whole session. Sharing one in-flight
  // promise (kept as store state, not a module-level variable, so it stays per-request under
  // Nuxt SSR) avoids that.
  const refreshPromise = shallowRef<Promise<boolean> | null>(null)

  async function refreshAccessToken(): Promise<boolean> {
    if (!refreshToken.value) return false
    if (refreshPromise.value) return refreshPromise.value

    const attempt = (async () => {
      try {
        const config = useRuntimeConfig()
        const tokens = await $fetch<AuthTokens>('/auth/refresh-token', {
          baseURL: config.public.apiBase,
          method: 'POST',
          body: { refreshToken: refreshToken.value },
        })
        setTokens(tokens)
        return true
      }
      catch {
        clearTokens()
        return false
      }
    })()

    refreshPromise.value = attempt
    try {
      return await attempt
    }
    finally {
      refreshPromise.value = null
    }
  }

  async function logout() {
    const config = useRuntimeConfig()
    const currentRefreshToken = refreshToken.value

    clearTokens()

    if (currentRefreshToken) {
      try {
        await $fetch('/auth/logout', {
          baseURL: config.public.apiBase,
          method: 'POST',
          body: { refreshToken: currentRefreshToken },
        })
      }
      catch {
        // Best-effort: local session is already cleared regardless of server-side outcome.
      }
    }
  }

  return {
    accessToken,
    refreshToken,
    user,
    isAuthenticated,
    hasRole,
    login,
    refreshAccessToken,
    logout,
  }
})
