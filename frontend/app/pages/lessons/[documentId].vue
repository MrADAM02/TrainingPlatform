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

const previewState = ref<'idle' | 'loading' | 'open' | 'error'>('idle')
const previewUrl = ref<string | null>(null)

async function togglePreview() {
  if (previewState.value === 'open') {
    previewState.value = 'idle'
    return
  }

  previewState.value = 'loading'
  try {
    const result = await request<{ url: string }>(`/documents/${documentId}/download-url`)
    previewUrl.value = result.url
    previewState.value = 'open'
  }
  catch {
    previewState.value = 'error'
  }
}

async function downloadCurrentLesson() {
  try {
    const result = await request<{ url: string }>(`/documents/${documentId}/download-url`)
    window.open(result.url, '_blank')
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
}

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
const nextLesson = computed<DocumentSummary | null>(() => lessons.value[currentIndex.value + 1] ?? null)
const isVideo = computed(() => currentLesson.value?.fileType === 1)
const isImage = computed(() => currentLesson.value?.fileType === 4)
const isText = computed(() => currentLesson.value?.fileType === 5)

async function markConsumed() {
  try {
    // Text lessons have no file to request a download URL for — mark-viewed is the equivalent
    // progress-recording trigger for that path (see MarkLessonViewedCommand on the backend).
    if (isText.value) {
      await request(`/documents/${documentId}/mark-viewed`, { method: 'POST' })
    }
    else {
      await request<{ url: string }>(`/documents/${documentId}/download-url`)
    }
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

const wordCount = computed(() => {
  const body = currentLesson.value?.transcriptText ?? ''
  return body.trim().length === 0 ? 0 : body.trim().split(/\s+/).length
})
// ~200 words/minute is a common average reading-speed estimate — labeled as an estimate in the
// UI (t('lessons.readingTime')), not presented as an exact figure.
const readingMinutes = computed(() => Math.max(1, Math.ceil(wordCount.value / 200)))

const fontScale = ref<'sm' | 'md' | 'lg'>('md')
const fontScaleClass = computed(() => ({ sm: 'text-sm', md: 'text-base', lg: 'text-lg' }[fontScale.value]))

function increaseFontScale() {
  if (fontScale.value === 'sm') fontScale.value = 'md'
  else if (fontScale.value === 'md') fontScale.value = 'lg'
}

function decreaseFontScale() {
  if (fontScale.value === 'lg') fontScale.value = 'md'
  else if (fontScale.value === 'md') fontScale.value = 'sm'
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
        <!-- Video: player + a Video/Transcript/Summary switcher, since all three are genuinely
             different content worth tabbing between. -->
        <template v-if="isVideo">
          <DocumentVideoPlayer
            :key="currentLesson.id" :document-id="currentLesson.id"
            :resume-from-seconds="currentLesson.lastPositionSeconds ?? undefined" class="mb-4"
          />

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
        </template>

        <!-- Image: zoomable viewer, caption from SummaryText. No extra thumbnail strip for
             sibling images — the lesson-list sidebar already covers cross-lesson navigation
             generically, a second image-only strip would just duplicate it. -->
        <template v-else-if="isImage">
          <DocumentImageViewer
            :key="currentLesson.id" :document-id="currentLesson.id" :caption="currentLesson.summaryText"
            class="mb-4"
          />
        </template>

        <!-- Text: a reading pane. transcriptText doubles as the lesson body (same "written
             content of the lesson" semantics as a video transcript, just fileless). -->
        <template v-else-if="isText">
          <UCard class="mb-4">
            <div :class="fontScaleClass" class="whitespace-pre-line leading-relaxed">
              {{ currentLesson.transcriptText }}
            </div>

            <blockquote
              v-if="currentLesson.quote" class="border-e-2 border-accent-400 pe-3 my-4 italic text-brand-700"
            >
              {{ currentLesson.quote }}
            </blockquote>

            <div class="flex items-center justify-between border-t border-default pt-3 mt-4">
              <p class="text-xs text-muted">
                {{ t('lessons.readingTime', { minutes: readingMinutes }) }} · {{ t('lessons.wordCount', { count: wordCount }) }}
              </p>
              <div class="flex gap-1">
                <UButton
                  size="xs" variant="soft" square :disabled="fontScale === 'sm'"
                  :aria-label="t('lessons.smallerText')" label="A-" @click="decreaseFontScale"
                />
                <UButton
                  size="xs" variant="soft" square :disabled="fontScale === 'lg'"
                  :aria-label="t('lessons.largerText')" label="A+" @click="increaseFontScale"
                />
              </div>
            </div>
          </UCard>
        </template>

        <!-- Every other type today (Pdf/Presentation/Other): a document card, since there's no
             transcript-equivalent to tab against — the summary/takeaway just render directly
             below, matching how little there actually is to switch between. -->
        <template v-else>
          <UCard class="mb-4">
            <div class="flex items-center gap-3 mb-4">
              <div class="flex items-center justify-center size-12 rounded-lg bg-accent-100 text-accent-700 shrink-0">
                <UIcon name="i-lucide-file-text" class="size-6" />
              </div>
              <div class="min-w-0">
                <p class="font-medium truncate">
                  {{ currentLesson.title }}
                </p>
                <p class="text-xs text-muted">
                  {{ formatBytes(currentLesson.sizeBytes) }}
                  <span v-if="currentLesson.pageCount">
                    · {{ t('lessons.pageCount', { count: currentLesson.pageCount }) }}
                  </span>
                </p>
              </div>
            </div>

            <div class="flex gap-2 mb-3">
              <UButton
                class="flex-1 justify-center" color="primary" icon="i-lucide-download"
                :label="t('courses.documents.download')" @click="downloadCurrentLesson"
              />
              <UButton
                class="flex-1 justify-center" variant="soft" :loading="previewState === 'loading'"
                :icon="previewState === 'open' ? 'i-lucide-x' : 'i-lucide-eye'"
                :label="previewState === 'open' ? t('lessons.closePreview') : t('lessons.quickPreview')"
                @click="togglePreview"
              />
            </div>

            <div v-if="currentLesson.pageCount" class="flex gap-1.5 mb-1">
              <div
                v-for="page in currentLesson.pageCount" :key="page"
                class="flex-1 h-10 rounded-md border border-default bg-elevated flex items-center justify-center text-xs text-muted"
                :class="page === 1 ? 'border-accent-400 bg-accent-50 text-accent-700' : ''"
              >
                {{ page }}
              </div>
            </div>

            <UAlert
              v-if="previewState === 'error'" color="error" variant="subtle" icon="i-lucide-triangle-alert"
              :title="t('courses.documents.watchError')" class="mt-3"
            />
          </UCard>

          <iframe
            v-if="previewState === 'open' && previewUrl" :src="previewUrl"
            class="w-full h-[420px] rounded-lg border border-default mb-4"
          />

          <div v-if="currentLesson.summaryText" class="text-sm whitespace-pre-line mb-4">
            {{ currentLesson.summaryText }}
          </div>
        </template>

        <UAlert
          v-if="currentLesson.keyTakeaway" color="primary" variant="subtle" icon="i-lucide-lightbulb"
          :title="t('lessons.keyTakeaway')" :description="currentLesson.keyTakeaway" class="mt-4"
        />

        <div v-if="nextLesson" class="flex items-center justify-between gap-3 bg-brand-700 rounded-lg p-4 mt-4">
          <div class="min-w-0">
            <p class="text-xs text-brand-200">
              {{ t('lessons.nextLesson') }}
            </p>
            <p class="text-white font-medium truncate">
              {{ nextLesson.title }}
            </p>
          </div>
          <UButton
            color="secondary" :icon="locale === 'ar' ? 'i-lucide-arrow-left' : 'i-lucide-arrow-right'"
            trailing :label="t('lessons.next')" :to="`/lessons/${nextLesson.id}?courseId=${courseId}`"
          />
        </div>
      </div>
    </div>
  </div>
</template>
