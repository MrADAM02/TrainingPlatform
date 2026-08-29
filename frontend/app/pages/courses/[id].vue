<script setup lang="ts">
import type { AccordionItem } from '@nuxt/ui'
import type { CourseDetails, DocumentSummary, ProblemDetails } from '~/types/api'
import { documentTypeIcons, documentTypeLabels } from '~/types/api'

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

function formatBytes(bytes: number | null): string {
  if (bytes === null) return ''
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

function scrollToContent() {
  document.getElementById('course-content')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
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
        <UCard class="lg:sticky lg:top-20">
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

          <div class="grid grid-cols-2 gap-3 mb-4">
            <div class="flex items-center gap-2">
              <div class="flex items-center justify-center size-9 rounded-lg bg-primary/10 text-primary shrink-0">
                <UIcon name="i-lucide-layers" class="size-4" />
              </div>
              <div class="min-w-0">
                <p class="text-lg font-semibold leading-tight">
                  {{ moduleCount }}
                </p>
                <p class="text-xs text-muted truncate">
                  {{ t('courses.overview.modulesLabel') }}
                </p>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <div class="flex items-center justify-center size-9 rounded-lg bg-primary/10 text-primary shrink-0">
                <UIcon name="i-lucide-file-stack" class="size-4" />
              </div>
              <div class="min-w-0">
                <p class="text-lg font-semibold leading-tight">
                  {{ materialCount }}
                </p>
                <p class="text-xs text-muted truncate">
                  {{ t('courses.overview.materialsLabel') }}
                </p>
              </div>
            </div>
          </div>

          <UButton block variant="soft" icon="i-lucide-list" :label="t('courses.overview.viewContent')" @click="scrollToContent" />
        </UCard>

        <UAlert
          v-if="!course.canDownload" color="neutral" variant="subtle" icon="i-lucide-lock"
          :description="t('courses.notEnrolledNotice')"
        />
      </div>

      <div id="course-content" class="lg:order-1 lg:col-span-2 scroll-mt-20">
        <p class="text-muted mb-4">
          {{ course.description }}
        </p>

        <h2 class="text-lg font-semibold mb-3">
          {{ t('courses.content') }}
        </h2>

        <UAccordion :items="accordionItems" :default-value="accordionItems[0]?.value">
          <template #body="{ item }">
            <template v-if="moduleById(item.value as string)">
              <ul v-if="moduleById(item.value as string)!.documents.length > 0" class="space-y-1">
                <li
                  v-for="doc in moduleById(item.value as string)!.documents" :key="doc.id"
                  class="flex items-center justify-between flex-wrap gap-3 gap-y-2 p-3 rounded-lg hover:bg-elevated/50 transition-colors"
                >
                  <div class="flex items-center gap-3 min-w-0">
                    <div class="flex items-center justify-center size-9 rounded-lg bg-primary/10 text-primary shrink-0">
                      <UIcon :name="documentTypeIcons[doc.fileType]" class="size-4" />
                    </div>
                    <div class="min-w-0">
                      <p class="text-sm font-medium truncate">
                        {{ doc.title }}
                      </p>
                      <p class="text-xs text-muted truncate">
                        {{ documentTypeLabels[doc.fileType] }}
                        <span v-if="doc.durationMinutes"> · {{ t('courses.documents.durationMinutes', { minutes: doc.durationMinutes }) }}</span>
                        <span v-if="doc.sizeBytes !== null"> · {{ formatBytes(doc.sizeBytes) }}</span>
                      </p>
                    </div>
                  </div>
                  <div class="flex items-center gap-2 shrink-0">
                    <UButton
                      size="xs" variant="soft" :disabled="!course.canDownload"
                      :icon="doc.fileType === 1 ? 'i-lucide-play' : doc.fileType === 5 ? 'i-lucide-file-text' : 'i-lucide-book-open'"
                      :label="doc.fileType === 1 ? t('courses.documents.watch') : doc.fileType === 5 ? t('courses.documents.read') : t('courses.documents.viewLesson')"
                      :to="course.canDownload ? `/lessons/${doc.id}?courseId=${courseId}` : undefined"
                    />
                    <UButton
                      v-if="doc.fileType !== 5" size="xs" variant="soft" :disabled="!course.canDownload"
                      :label="t('courses.documents.download')" @click="downloadDocument(doc)"
                    />
                  </div>
                </li>
              </ul>
              <p v-else class="text-sm text-muted">
                {{ t('courses.documents.empty') }}
              </p>

              <template v-if="moduleById(item.value as string)!.quizzes.length > 0">
                <p class="text-sm font-medium mt-4 mb-2">
                  {{ t('courses.quizzes.title') }}
                </p>
                <ul class="space-y-1">
                  <li
                    v-for="quiz in moduleById(item.value as string)!.quizzes" :key="quiz.id"
                    class="flex items-center justify-between flex-wrap gap-3 gap-y-2 p-3 rounded-lg hover:bg-elevated/50 transition-colors"
                  >
                    <div class="flex items-center gap-3 min-w-0">
                      <div class="flex items-center justify-center size-9 rounded-lg bg-secondary/10 text-secondary shrink-0">
                        <UIcon name="i-lucide-list-checks" class="size-4" />
                      </div>
                      <div class="min-w-0">
                        <p class="text-sm font-medium truncate">
                          {{ quiz.title }}
                        </p>
                        <p v-if="quiz.isRequiredForCompletion" class="text-xs text-muted truncate">
                          {{ t('courses.quizzes.required') }}
                        </p>
                      </div>
                    </div>
                    <UButton
                      size="xs" variant="soft" icon="i-lucide-list-checks" class="shrink-0"
                      :label="t('courses.quizzes.startQuiz')" :to="`/quizzes/${quiz.id}`"
                    />
                  </li>
                </ul>
              </template>
            </template>
          </template>
        </UAccordion>
      </div>
    </div>
  </div>
</template>
