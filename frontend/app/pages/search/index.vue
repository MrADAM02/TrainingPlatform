<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
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

const resultColumns = computed<TableColumn<DocumentSearchResult>[]>(() => [
  { accessorKey: 'documentTitle', header: t('courses.documents.title') },
  { accessorKey: 'courseTitle', header: t('search.course') },
  { accessorKey: 'uploadedAtUtc', header: t('search.uploadedAt') },
  { id: 'actions', header: '' },
])

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString()
}

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

    <div v-else class="overflow-x-auto">
      <UTable :data="results?.items ?? []" :columns="resultColumns">
        <template #documentTitle-cell="{ row }">
          {{ row.original.documentTitle }}
          <UBadge variant="subtle" size="sm" class="ms-2">
            {{ documentTypeLabels[row.original.fileType] }}
          </UBadge>
        </template>
        <template #uploadedAtUtc-cell="{ row }">
          {{ formatDate(row.original.uploadedAtUtc) }}
        </template>
        <template #actions-cell="{ row }">
          <div class="flex justify-end">
            <UButton
              size="xs" variant="soft" :disabled="!row.original.canDownload"
              :label="t('courses.documents.download')" @click="downloadDocument(row.original)"
            />
          </div>
        </template>
      </UTable>
    </div>

    <div v-if="results && results.totalPages > 1" class="flex items-center justify-between mt-4">
      <span class="text-sm text-muted">{{ t('pagination.pageOf', { page: results.page, totalPages: results.totalPages }) }}</span>
      <UPagination v-model:page="page" :total="results.totalCount" :items-per-page="pageSize" />
    </div>
  </div>
</template>
