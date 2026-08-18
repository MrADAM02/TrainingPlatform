<script setup lang="ts">
import type { CourseSummary, DocumentSearchResult, DocumentTypeName, PaginatedList } from '~/types/api'
import { documentTypeLabels, documentTypeNames } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const toast = useToast()

const keyword = ref('')
const courseId = ref<string | undefined>(undefined)
const contentType = ref<DocumentTypeName | undefined>(undefined)
const page = ref(1)
const pageSize = 20

const results = ref<PaginatedList<DocumentSearchResult> | null>(null)
const courses = ref<CourseSummary[]>([])

async function loadCourses() {
  try {
    const list = await request<PaginatedList<CourseSummary>>('/courses?page=1&pageSize=100')
    courses.value = list.items
  }
  catch {
    // Non-fatal: the course filter just stays empty.
  }
}

const courseItems = computed(() => [
  { label: t('search.allCourses'), value: undefined },
  ...courses.value.map(c => ({ label: c.title, value: c.id })),
])

const contentTypeItems = computed(() => [
  { label: t('search.allTypes'), value: undefined },
  ...documentTypeNames.map((name, index) => ({ label: documentTypeLabels[index], value: name })),
])

async function runSearch() {
  const params = new URLSearchParams()
  if (keyword.value) params.set('keyword', keyword.value)
  if (courseId.value) params.set('courseId', courseId.value)
  if (contentType.value) params.set('contentType', contentType.value)
  params.set('page', String(page.value))
  params.set('pageSize', String(pageSize))

  try {
    results.value = await request<PaginatedList<DocumentSearchResult>>(`/search/documents?${params.toString()}`)
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
}

watch([courseId, contentType, page], runSearch)

await Promise.all([loadCourses(), runSearch()])

async function downloadDocument(result: DocumentSearchResult) {
  try {
    const response = await request<{ url: string }>(`/documents/${result.documentId}/download-url`)
    window.open(response.url, '_blank')
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
}
</script>

<template>
  <div>
    <h1 class="text-xl font-semibold mb-6">
      {{ t('search.title') }}
    </h1>

    <div class="grid gap-4 sm:grid-cols-3 mb-6">
      <UInput
        v-model="keyword" :placeholder="t('search.keyword')" icon="i-lucide-search"
        @keyup.enter="page = 1; runSearch()"
      />
      <USelect v-model="courseId" :items="courseItems" :placeholder="t('search.course')" />
      <USelect v-model="contentType" :items="contentTypeItems" :placeholder="t('search.contentType')" />
    </div>

    <p v-if="results && results.items.length === 0" class="text-muted">
      {{ t('search.noResults') }}
    </p>

    <ul v-else class="space-y-2">
      <li
        v-for="item in results?.items ?? []" :key="item.documentId"
        class="flex items-center justify-between border-b border-default pb-2 text-sm"
      >
        <span>
          {{ item.documentTitle }}
          <UBadge variant="subtle" size="sm" class="ms-2">
            {{ documentTypeLabels[item.fileType] }}
          </UBadge>
          <span class="text-muted ms-2">{{ t('search.inCourse', { course: item.courseTitle }) }}</span>
        </span>
        <UButton
          size="xs" variant="soft" :disabled="!item.canDownload"
          :label="t('courses.documents.download')" @click="downloadDocument(item)"
        />
      </li>
    </ul>

    <div v-if="results && results.totalPages > 1" class="flex items-center justify-between mt-4">
      <span class="text-sm text-muted">{{ t('pagination.pageOf', { page: results.page, totalPages: results.totalPages }) }}</span>
      <UPagination v-model:page="page" :total="results.totalCount" :items-per-page="pageSize" />
    </div>
  </div>
</template>
