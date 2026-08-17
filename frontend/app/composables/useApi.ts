type ApiFetchOptions = Parameters<typeof $fetch>[1]

export function useApi() {
  const config = useRuntimeConfig()
  const authStore = useAuthStore()

  async function request<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
    const doFetch = () => {
      const headers = new Headers(options.headers)
      if (authStore.accessToken) {
        headers.set('Authorization', `Bearer ${authStore.accessToken}`)
      }

      return $fetch<T>(path, {
        ...options,
        baseURL: config.public.apiBase,
        headers,
      })
    }

    try {
      return await doFetch()
    }
    catch (error: unknown) {
      const status = (error as { response?: { status?: number } })?.response?.status

      if (status === 401 && authStore.refreshToken) {
        const refreshed = await authStore.refreshAccessToken()
        if (refreshed) {
          return await doFetch()
        }
      }

      throw error
    }
  }

  return { request }
}
