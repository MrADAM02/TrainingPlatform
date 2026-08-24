interface BookmarkableCourse {
  id: string
  isBookmarked: boolean
}

/** Optimistic toggle shared by every page that renders a bookmark-able CourseCard. Mutates the
 * passed course in place and reverts on failure, so callers don't each re-implement the same
 * try/catch. */
export function useBookmarks() {
  const { request } = useApi()
  const toast = useToast()
  const { t } = useI18n()
  const { refresh: refreshBookmarkCount } = useBookmarkCount()

  async function toggleBookmark(course: BookmarkableCourse) {
    const wasBookmarked = course.isBookmarked
    course.isBookmarked = !wasBookmarked
    try {
      await request(`/courses/${course.id}/bookmark`, { method: wasBookmarked ? 'DELETE' : 'POST' })
      await refreshBookmarkCount()
    }
    catch {
      course.isBookmarked = wasBookmarked
      toast.add({ title: t('common.error'), color: 'error' })
    }
  }

  return { toggleBookmark }
}
