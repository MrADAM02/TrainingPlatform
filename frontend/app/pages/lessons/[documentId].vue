<script setup lang="ts">
import type { CourseDetails, DocumentSummary, ProblemDetails } from '~/types/api'

definePageMeta({
  // Sibling lessons share this same route component; without a param-derived key, Vue Router
  // reuses the instance across navigations and every top-level `await` below would only ever
  // run once, leaving progress-recording and the fetched lesson stuck on the first lesson opened.
  key: route => route.fullPath,
})

const { t, locale } = useI18n()
const { request } = useApi()
const toast = useToast()
const route = useRoute()
const router = useRouter()

const documentId = route.params.documentId as string
const courseId = route.query.courseId as string | undefined
const backIcon = computed(() => (locale.value === 'ar' ? 'i-lucide-arrow-right' : 'i-lucide-arrow-left'))

const course = ref<CourseDetails | null>(null)
const activeTab = ref<'video' | 'transcript' | 'summary'>('video')

if (!courseId) {
  toast.add({ title: t('lessons.notFound'), color: 'error' })
  await router.push('/courses')
}
else {
  try {
    course.value = await request<CourseDetails>(`/courses/${courseId}`)
  }
  catch (error) {
    const problem = (error as { data?: ProblemDetails })?.data
    toast.add({ title: problem?.detail ?? t('common.error'), color: 'error' })
    await router.push('/courses')
  }
}

const currentModule = computed(() =>
  course.value?.modules.find(m => m.documents.some(d => d.id === documentId)) ?? null,
)

const lessons = computed<DocumentSummary[]>(() => currentModule.value?.documents ?? [])
const currentIndex = computed(() => lessons.value.findIndex(d => d.id === documentId))
const currentLesson = computed<DocumentSummary | null>(() => lessons.value[currentIndex.value] ?? null)

async function markConsumed() {
  try {
    await request<{ url: string }>(`/documents/${documentId}/download-url`)
  }
  catch {
    // Handled by DocumentVideoPlayer's own error state — this call only exists to record
    // progress the same way it already does for every other document type.
  }
}

if (course.value) {
  await markConsumed()
}

function formatDuration(minutes: number | null): string {
  return minutes ? t('courses.documents.durationMinutes', { minutes }) : ''
}
</script>

<template>
  <div v-if="course && currentModule && currentLesson">
    <UButton
      variant="ghost" :icon="backIcon" :label="t('lessons.backToCourse')"
      :to="`/courses/${courseId}`" class="mb-4"
    />

    <p class="text-sm text-muted mb-1">
      {{ course.title }} / {{ currentModule.title }}
    </p>
    <h1 class="text-xl font-semibold mb-1">
      {{ currentLesson.title }}
    </h1>
    <p class="text-sm text-muted mb-6">
      {{ t('lessons.lessonOf', { current: currentIndex + 1, total: lessons.length }) }}
    </p>

    <div class="grid lg:grid-cols-3 gap-6">
      <div class="lg:order-2 lg:col-span-1">
        <UCard class="lg:sticky lg:top-20" :ui="{ body: 'p-0 sm:p-0' }">
          <ul class="divide-y divide-default">
            <li v-for="(lesson, index) in lessons" :key="lesson.id">
              <NuxtLink
                :to="`/lessons/${lesson.id}?courseId=${courseId}`"
                class="flex items-center gap-3 p-3 text-sm hover:bg-elevated/50"
                :class="lesson.id === documentId ? 'bg-elevated/50 font-medium' : ''"
              >
                <UIcon
                  :name="lesson.isCompleted ? 'i-lucide-check-circle' : 'i-lucide-circle'"
                  :class="lesson.isCompleted ? 'text-success' : 'text-muted'" class="shrink-0"
                />
                <span class="flex-1 min-w-0 truncate">{{ index + 1 }}. {{ lesson.title }}</span>
                <span v-if="lesson.durationMinutes" class="text-muted text-xs shrink-0">
                  {{ formatDuration(lesson.durationMinutes) }}
                </span>
              </NuxtLink>
            </li>
          </ul>
        </UCard>
      </div>

      <div class="lg:order-1 lg:col-span-2">
        <DocumentVideoPlayer :key="currentLesson.id" :document-id="currentLesson.id" class="mb-4" />

        <div class="flex gap-2 border-b border-default mb-4">
          <button
            type="button" class="px-3 py-2 text-sm border-b-2 -mb-px"
            :class="activeTab === 'video' ? 'border-primary text-primary font-medium' : 'border-transparent text-muted'"
            @click="activeTab = 'video'"
          >
            {{ t('lessons.tabVideo') }}
          </button>
          <button
            type="button" class="px-3 py-2 text-sm border-b-2 -mb-px"
            :class="activeTab === 'transcript' ? 'border-primary text-primary font-medium' : 'border-transparent text-muted'"
            @click="activeTab = 'transcript'"
          >
            {{ t('lessons.tabTranscript') }}
          </button>
          <button
            type="button" class="px-3 py-2 text-sm border-b-2 -mb-px"
            :class="activeTab === 'summary' ? 'border-primary text-primary font-medium' : 'border-transparent text-muted'"
            @click="activeTab = 'summary'"
          >
            {{ t('lessons.tabSummary') }}
          </button>
        </div>

        <div v-if="activeTab === 'transcript'" class="text-sm whitespace-pre-line">
          {{ currentLesson.transcriptText || t('lessons.noTranscript') }}
        </div>
        <div v-else-if="activeTab === 'summary'" class="text-sm whitespace-pre-line">
          {{ currentLesson.summaryText || t('lessons.noSummary') }}
        </div>

        <UAlert
          v-if="currentLesson.keyTakeaway" color="primary" variant="subtle" icon="i-lucide-lightbulb"
          :title="t('lessons.keyTakeaway')" :description="currentLesson.keyTakeaway" class="mt-4"
        />
      </div>
    </div>
  </div>
</template>
