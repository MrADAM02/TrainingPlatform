<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { ProblemDetails, TraineeProgressReportItem } from '~/types/api'

const { t, locale } = useI18n()
const { request } = useApi()
const toast = useToast()
const route = useRoute()
const router = useRouter()

const courseId = route.params.id as string
const courseTitle = route.query.title as string | undefined
const backIcon = computed(() => (locale.value === 'ar' ? 'i-lucide-arrow-right' : 'i-lucide-arrow-left'))

const trainees = ref<TraineeProgressReportItem[]>([])
const loading = ref(true)

try {
  trainees.value = await request<TraineeProgressReportItem[]>(`/reports/courses/${courseId}/trainees`)
}
catch (error) {
  const problem = (error as { data?: ProblemDetails })?.data
  toast.add({ title: problem?.detail ?? t('common.error'), color: 'error' })
  await router.push('/reports')
}
finally {
  loading.value = false
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString()
}

const columns = computed<TableColumn<TraineeProgressReportItem>[]>(() => [
  { accessorKey: 'userFullName', header: t('reports.trainees.trainee') },
  { accessorKey: 'status', header: t('reports.trainees.status') },
  { accessorKey: 'completedDocuments', header: t('reports.trainees.documents') },
  { accessorKey: 'requiredQuizzesPassed', header: t('reports.trainees.quizzes') },
  { accessorKey: 'certificateIssued', header: t('reports.trainees.certificate') },
  { accessorKey: 'enrolledAtUtc', header: t('courses.enrollments.enrolledAt') },
])
</script>

<template>
  <div>
    <UButton variant="ghost" :icon="backIcon" :label="t('reports.trainees.backToReports')" to="/reports" class="mb-4" />

    <h1 class="text-xl font-semibold mb-6">
      {{ courseTitle ?? t('reports.trainees.title') }}
    </h1>

    <p v-if="!loading && trainees.length === 0" class="text-muted">
      {{ t('reports.trainees.empty') }}
    </p>

    <div v-else class="overflow-x-auto">
      <UTable :data="trainees" :columns="columns" :loading="loading">
        <template #userFullName-cell="{ row }">
          <span class="font-medium">{{ row.original.userFullName }}</span>
          <span class="text-muted block text-xs">{{ row.original.userEmail }}</span>
        </template>
        <template #status-cell="{ row }">
          <UBadge :color="row.original.status === 1 ? 'success' : 'primary'" variant="subtle">
            {{ row.original.status === 1 ? t('courses.enrollments.statusCompleted') : t('courses.enrollments.statusActive') }}
          </UBadge>
        </template>
        <template #completedDocuments-cell="{ row }">
          {{ row.original.completedDocuments }} / {{ row.original.totalDocuments }}
        </template>
        <template #requiredQuizzesPassed-cell="{ row }">
          <span v-if="row.original.requiredQuizzesTotal === 0" class="text-muted">{{ t('reports.trainees.noRequiredQuizzes') }}</span>
          <span v-else>{{ row.original.requiredQuizzesPassed }} / {{ row.original.requiredQuizzesTotal }}</span>
        </template>
        <template #certificateIssued-cell="{ row }">
          <UIcon
            :name="row.original.certificateIssued ? 'i-lucide-check-circle' : 'i-lucide-minus'"
            :class="row.original.certificateIssued ? 'text-success' : 'text-muted'"
          />
        </template>
        <template #enrolledAtUtc-cell="{ row }">
          {{ formatDate(row.original.enrolledAtUtc) }}
        </template>
      </UTable>
    </div>
  </div>
</template>
