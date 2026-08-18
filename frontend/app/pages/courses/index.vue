<script setup lang="ts">
import type { CourseSummary, PaginatedList } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const toast = useToast()

const page = ref(1)
const pageSize = 20
const data = ref<PaginatedList<CourseSummary> | null>(null)

async function fetchCourses() {
  try {
    data.value = await request<PaginatedList<CourseSummary>>(`/courses?page=${page.value}&pageSize=${pageSize}`)
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
}

watch(page, fetchCourses)
await fetchCourses()
</script>

<template>
  <div>
    <h1 class="text-xl font-semibold mb-6">
      {{ t('courses.browseTitle') }}
    </h1>

    <p v-if="data && data.items.length === 0" class="text-muted">
      {{ t('courses.empty') }}
    </p>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <CourseCard
        v-for="course in data?.items ?? []" :id="course.id" :key="course.id"
        :title="course.title" :description="course.description" :to="`/courses/${course.id}`"
        :is-enrolled="course.isEnrolled"
      />
    </div>

    <div v-if="data && data.totalPages > 1" class="flex items-center justify-between mt-4">
      <span class="text-sm text-muted">{{ t('pagination.pageOf', { page: data.page, totalPages: data.totalPages }) }}</span>
      <UPagination v-model:page="page" :total="data.totalCount" :items-per-page="pageSize" />
    </div>
  </div>
</template>
