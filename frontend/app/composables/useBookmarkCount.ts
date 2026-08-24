import type { CourseSummary } from '~/types/api'

/** Shared across the app (nav badge, any page that needs it) via Nuxt's SSR-safe useState. */
export function useBookmarkCount() {
  const count = useState<number>('bookmark-count', () => 0)

  async function refresh() {
    const authStore = useAuthStore()
    if (!authStore.isAuthenticated) {
      count.value = 0
      return
    }

    const { request } = useApi()
    try {
      const bookmarks = await request<CourseSummary[]>('/bookmarks')
      count.value = bookmarks.length
    }
    catch {
      count.value = 0
    }
  }

  return { count, refresh }
}
