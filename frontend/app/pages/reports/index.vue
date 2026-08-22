<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { CourseCompletionReportItem, OrgSummaryReport, ProblemDetails } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const authStore = useAuthStore()
const toast = useToast()

const isAdmin = computed(() => authStore.hasRole('Administrator'))

const summary = ref<OrgSummaryReport | null>(null)
const courses = ref<CourseCompletionReportItem[]>([])
const loading = ref(true)
const isExporting = ref(false)

async function load() {
  loading.value = true
  try {
    const coursesPromise = request<CourseCompletionReportItem[]>('/reports/courses')
    const summaryPromise = isAdmin.value ? request<OrgSummaryReport>('/reports/summary') : Promise.resolve(null)
    const results = await Promise.all([coursesPromise, summaryPromise])
    courses.value = results[0]
    summary.value = results[1]
  }
  catch (error) {
    const problem = (error as { data?: ProblemDetails })?.data
    toast.add({ title: problem?.detail ?? t('common.error'), color: 'error' })
  }
  finally {
    loading.value = false
  }
}

await load()

async function exportCsv() {
  isExporting.value = true
  try {
    const blob = await request<Blob>('/reports/courses/export', { responseType: 'blob' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `course-completion-report-${new Date().toISOString().slice(0, 10)}.csv`
    link.click()
    URL.revokeObjectURL(url)
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
  finally {
    isExporting.value = false
  }
}

function formatDays(value: number | null): string {
  if (value === null) return '—'
  return t('reports.courses.avgDays', { days: value })
}

const columns = computed<TableColumn<CourseCompletionReportItem>[]>(() => [
  { accessorKey: 'courseTitle', header: t('reports.courses.course') },
  { accessorKey: 'enrolledCount', header: t('reports.courses.enrolled') },
  { accessorKey: 'completedCount', header: t('reports.courses.completed') },
  { accessorKey: 'completionPercent', header: t('reports.courses.completionRate') },
  { accessorKey: 'avgCompletionDays', header: t('reports.courses.avgCompletionTime') },
  { id: 'actions', header: '' },
])
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-xl font-semibold">
        {{ t('reports.title') }}
      </h1>
      <UButton
        variant="soft" icon="i-lucide-download" :loading="isExporting"
        :label="t('reports.courses.export')" @click="exportCsv"
      />
    </div>

    <div v-if="isAdmin && summary" class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4 mb-8">
      <StatCard icon="i-lucide-book-open" :value="summary.totalCourses" :label="t('reports.summary.totalCourses')" />
      <StatCard icon="i-lucide-eye" :value="summary.publishedCourses" :label="t('reports.summary.publishedCourses')" />
      <StatCard icon="i-lucide-users" :value="summary.totalEnrollments" :label="t('reports.summary.totalEnrollments')" />
      <StatCard icon="i-lucide-check-circle" :value="summary.completedEnrollments" :label="t('reports.summary.completedEnrollments')" />
      <StatCard icon="i-lucide-award" :value="summary.totalCertificatesIssued" :label="t('reports.summary.totalCertificatesIssued')" />
      <StatCard icon="i-lucide-activity" :value="summary.activeTraineesLast30Days" :label="t('reports.summary.activeTrainees')" />
    </div>

    <h2 class="text-lg font-semibold mb-4">
      {{ t('reports.courses.title') }}
    </h2>

    <p v-if="!loading && courses.length === 0" class="text-muted">
      {{ t('reports.courses.empty') }}
    </p>

    <div v-else class="overflow-x-auto">
      <UTable :data="courses" :columns="columns" :loading="loading">
        <template #courseTitle-cell="{ row }">
          <span class="font-medium">{{ row.original.courseTitle }}</span>
          <UBadge :color="row.original.isPublished ? 'success' : 'neutral'" variant="subtle" size="sm" class="ms-2">
            {{ row.original.isPublished ? t('courses.published') : t('courses.draft') }}
          </UBadge>
        </template>
        <template #completionPercent-cell="{ row }">
          {{ row.original.completionPercent }}%
        </template>
        <template #avgCompletionDays-cell="{ row }">
          {{ formatDays(row.original.avgCompletionDays) }}
        </template>
        <template #actions-cell="{ row }">
          <UButton
            size="xs" variant="soft" icon="i-lucide-users"
            :label="t('reports.courses.viewTrainees')"
            :to="`/reports/courses/${row.original.courseId}?title=${encodeURIComponent(row.original.courseTitle)}`"
          />
        </template>
      </UTable>
    </div>
  </div>
</template>
