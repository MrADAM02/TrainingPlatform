<script setup lang="ts">
import type { CourseSummary } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const toast = useToast()
const { toggleBookmark } = useBookmarks()

const courses = ref<CourseSummary[]>([])
const loading = ref(true)

async function fetchBookmarks() {
  loading.value = true
  try {
    courses.value = await request<CourseSummary[]>('/bookmarks')
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
  finally {
    loading.value = false
  }
}

await fetchBookmarks()

async function handleToggle(course: CourseSummary) {
  await toggleBookmark(course)
  if (!course.isBookmarked) {
    courses.value = courses.value.filter(c => c.id !== course.id)
  }
}
</script>

<template>
  <div>
    <h1 class="text-xl font-semibold mb-6">
      {{ t('library.title') }}
    </h1>

    <p v-if="!loading && courses.length === 0" class="text-muted">
      {{ t('library.empty') }}
    </p>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <CourseCard
        v-for="course in courses" :id="course.id" :key="course.id"
        :title="course.title" :description="course.description" :to="`/courses/${course.id}`"
        :is-enrolled="course.isEnrolled" :is-bookmarked="course.isBookmarked"
        @toggle-bookmark="handleToggle(course)"
      />
    </div>
  </div>
</template>
