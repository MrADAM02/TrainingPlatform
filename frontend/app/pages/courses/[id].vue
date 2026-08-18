<script setup lang="ts">
import type { CourseDetails, DocumentSummary, ProblemDetails } from '~/types/api'
import { documentTypeLabels } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const toast = useToast()
const route = useRoute()
const router = useRouter()

const courseId = route.params.id as string
const course = ref<CourseDetails | null>(null)

try {
  course.value = await request<CourseDetails>(`/courses/${courseId}`)
}
catch (error) {
  const problem = (error as { data?: ProblemDetails })?.data
  toast.add({ title: problem?.detail ?? t('common.error'), color: 'error' })
  await router.push('/courses')
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  const units = ['KB', 'MB', 'GB']
  let value = bytes / 1024
  let unitIndex = 0
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024
    unitIndex += 1
  }
  return `${value.toFixed(1)} ${units[unitIndex]}`
}

async function downloadDocument(doc: DocumentSummary) {
  try {
    const result = await request<{ url: string }>(`/documents/${doc.id}/download-url`)
    window.open(result.url, '_blank')
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
}
</script>

<template>
  <div v-if="course">
    <UButton variant="ghost" icon="i-lucide-arrow-left" :label="t('courses.back')" to="/courses" class="mb-4" />

    <div class="flex items-center gap-2 mb-2">
      <h1 class="text-xl font-semibold">
        {{ course.title }}
      </h1>
      <UBadge v-if="course.isEnrolled" color="success" variant="subtle">
        {{ t('courses.enrolled') }}
      </UBadge>
    </div>
    <p class="text-muted mb-4">
      {{ course.description }}
    </p>

    <UAlert
      v-if="!course.canDownload" color="neutral" variant="subtle" icon="i-lucide-lock"
      :description="t('courses.notEnrolledNotice')" class="mb-6"
    />

    <div v-for="module in course.modules" :key="module.id" class="mb-6 rounded-lg border border-default p-4">
      <span class="font-medium block mb-3">{{ module.order }}. {{ module.title }}</span>

      <ul v-if="module.documents.length > 0" class="space-y-2">
        <li v-for="doc in module.documents" :key="doc.id" class="flex items-center justify-between text-sm">
          <span>
            {{ doc.title }}
            <UBadge variant="subtle" size="sm" class="ms-2">
              {{ documentTypeLabels[doc.fileType] }}
            </UBadge>
            <span class="text-muted ms-2">{{ formatBytes(doc.sizeBytes) }}</span>
          </span>
          <UButton
            size="xs" variant="soft" :disabled="!course.canDownload"
            :label="t('courses.documents.download')" @click="downloadDocument(doc)"
          />
        </li>
      </ul>
      <p v-else class="text-sm text-muted">
        {{ t('courses.documents.empty') }}
      </p>
    </div>
  </div>
</template>
