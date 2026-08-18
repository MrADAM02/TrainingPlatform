<script setup lang="ts">
import type { AccordionItem } from '@nuxt/ui'
import type { CourseDetails, DocumentSummary, ProblemDetails } from '~/types/api'
import { documentTypeLabels } from '~/types/api'

const { t, locale } = useI18n()
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

const backIcon = computed(() => (locale.value === 'ar' ? 'i-lucide-arrow-right' : 'i-lucide-arrow-left'))

const moduleCount = computed(() => course.value?.modules.length ?? 0)
const materialCount = computed(() =>
  course.value?.modules.reduce((sum, m) => sum + m.documents.length, 0) ?? 0,
)

const accordionItems = computed<AccordionItem[]>(() =>
  (course.value?.modules ?? []).map(module => ({
    label: `${module.order}. ${module.title}`,
    value: module.id,
  })),
)

function moduleById(id: string) {
  return course.value?.modules.find(m => m.id === id) ?? null
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
    <UButton variant="ghost" :icon="backIcon" :label="t('courses.back')" to="/courses" class="mb-4" />

    <CourseCoverPlaceholder :id="course.id" :title="course.title" size="hero" class="mb-6">
      <div class="absolute inset-0 bg-black/40 flex items-end p-4 sm:p-6">
        <h1 class="text-xl sm:text-2xl font-semibold text-white">
          {{ course.title }}
        </h1>
      </div>
    </CourseCoverPlaceholder>

    <div class="grid lg:grid-cols-3 gap-6">
      <div class="lg:order-2 lg:col-span-1 space-y-4">
        <UCard class="lg:sticky lg:top-4">
          <template #header>
            <span class="font-medium">{{ t('courses.overview.title') }}</span>
          </template>

          <div class="flex items-center gap-2 mb-3">
            <UBadge :color="course.isPublished ? 'success' : 'neutral'" variant="subtle">
              {{ course.isPublished ? t('courses.published') : t('courses.draft') }}
            </UBadge>
            <UBadge v-if="course.isEnrolled" color="primary" variant="subtle">
              {{ t('courses.enrolled') }}
            </UBadge>
          </div>

          <p class="text-sm text-muted mb-1">
            {{ t('courses.overview.modules', { count: moduleCount }) }}
          </p>
          <p class="text-sm text-muted mb-4">
            {{ t('courses.overview.materials', { count: materialCount }) }}
          </p>

          <UButton block variant="soft" icon="i-lucide-list" :label="t('courses.overview.viewContent')" to="#course-content" />
        </UCard>

        <UAlert
          v-if="!course.canDownload" color="neutral" variant="subtle" icon="i-lucide-lock"
          :description="t('courses.notEnrolledNotice')"
        />
      </div>

      <div id="course-content" class="lg:order-1 lg:col-span-2">
        <p class="text-muted mb-4">
          {{ course.description }}
        </p>

        <h2 class="text-lg font-semibold mb-3">
          {{ t('courses.content') }}
        </h2>

        <UAccordion :items="accordionItems" :default-value="accordionItems[0]?.value">
          <template #body="{ item }">
            <template v-if="moduleById(item.value as string)">
              <ul v-if="moduleById(item.value as string)!.documents.length > 0" class="space-y-2">
                <li
                  v-for="doc in moduleById(item.value as string)!.documents" :key="doc.id"
                  class="flex items-center justify-between text-sm"
                >
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
            </template>
          </template>
        </UAccordion>
      </div>
    </div>
  </div>
</template>
