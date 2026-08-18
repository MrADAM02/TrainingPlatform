<script setup lang="ts">
import type { FormError, FormSubmitEvent } from '@nuxt/ui'
import type { CourseSummary, PaginatedList, ProblemDetails } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const toast = useToast()
const authStore = useAuthStore()
const router = useRouter()

const page = ref(1)
const pageSize = 20
const data = ref<PaginatedList<CourseSummary> | null>(null)

async function fetchCourses() {
  try {
    data.value = await request<PaginatedList<CourseSummary>>(`/courses?page=${page.value}&pageSize=${pageSize}&mine=true`)
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
}

watch(page, fetchCourses)
await fetchCourses()

function errorDetail(error: unknown, fallback: string): string {
  return (error as { data?: ProblemDetails })?.data?.detail ?? fallback
}

// --- Create ---
const isCreateOpen = ref(false)
const createState = reactive({ title: '', description: '' })

function validateCreate(state: typeof createState): FormError[] {
  const errors: FormError[] = []
  if (!state.title) errors.push({ name: 'title', message: t('courses.courseTitle') })
  if (!state.description) errors.push({ name: 'description', message: t('courses.description') })
  return errors
}

async function submitCreate(event: FormSubmitEvent<typeof createState>) {
  try {
    const created = await request<{ id: string }>('/courses', {
      method: 'POST',
      body: { ...event.data, trainerId: authStore.user?.id },
    })
    toast.add({ title: t('courses.createSuccess'), color: 'success' })
    isCreateOpen.value = false
    createState.title = ''
    createState.description = ''
    await router.push(`/trainer/${created.id}`)
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}

// --- Publish toggle ---
async function togglePublish(course: CourseSummary) {
  try {
    await request(`/courses/${course.id}/${course.isPublished ? 'unpublish' : 'publish'}`, { method: 'POST' })
    toast.add({ title: t(course.isPublished ? 'courses.unpublishSuccess' : 'courses.publishSuccess'), color: 'success' })
    await fetchCourses()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}

// --- Delete ---
const deletingCourse = ref<CourseSummary | null>(null)
const isDeleteOpen = computed({
  get: () => deletingCourse.value !== null,
  set: (value: boolean) => { if (!value) deletingCourse.value = null },
})

async function confirmDelete() {
  if (!deletingCourse.value) return
  try {
    await request(`/courses/${deletingCourse.value.id}`, { method: 'DELETE' })
    toast.add({ title: t('courses.deleteSuccess'), color: 'success' })
    deletingCourse.value = null
    await fetchCourses()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
    deletingCourse.value = null
  }
}
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-xl font-semibold">
        {{ t('courses.manageTitle') }}
      </h1>
      <UButton :label="t('courses.create')" icon="i-lucide-plus" @click="isCreateOpen = true" />
    </div>

    <p v-if="data && data.items.length === 0" class="text-muted">
      {{ t('courses.empty') }}
    </p>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <UCard v-for="course in data?.items ?? []" :key="course.id">
        <template #header>
          <div class="flex items-center justify-between gap-2">
            <span class="font-medium truncate">{{ course.title }}</span>
            <UBadge :color="course.isPublished ? 'success' : 'neutral'" variant="subtle">
              {{ course.isPublished ? t('courses.published') : t('courses.draft') }}
            </UBadge>
          </div>
        </template>

        <p class="text-sm text-muted line-clamp-3">
          {{ course.description }}
        </p>

        <template #footer>
          <div class="flex flex-wrap gap-2">
            <UButton size="xs" variant="soft" :label="t('courses.manage')" :to="`/trainer/${course.id}`" />
            <UButton
              size="xs" variant="soft" :color="course.isPublished ? 'warning' : 'primary'"
              :label="course.isPublished ? t('courses.unpublish') : t('courses.publish')"
              @click="togglePublish(course)"
            />
            <UButton size="xs" variant="soft" color="error" :label="t('courses.delete')" @click="deletingCourse = course" />
          </div>
        </template>
      </UCard>
    </div>

    <div v-if="data && data.totalPages > 1" class="flex items-center justify-between mt-4">
      <span class="text-sm text-muted">{{ t('pagination.pageOf', { page: data.page, totalPages: data.totalPages }) }}</span>
      <UPagination v-model:page="page" :total="data.totalCount" :items-per-page="pageSize" />
    </div>

    <UModal v-model:open="isCreateOpen" :title="t('courses.create')">
      <template #body>
        <UForm :state="createState" :validate="validateCreate" @submit="submitCreate">
          <div class="space-y-4">
            <UFormField :label="t('courses.courseTitle')" name="title">
              <UInput v-model="createState.title" class="w-full" />
            </UFormField>
            <UFormField :label="t('courses.description')" name="description">
              <UTextarea v-model="createState.description" class="w-full" :rows="4" />
            </UFormField>
          </div>
          <div class="flex justify-end gap-2 mt-6">
            <UButton variant="ghost" :label="t('courses.cancel')" @click="isCreateOpen = false" />
            <UButton type="submit" :label="t('courses.save')" />
          </div>
        </UForm>
      </template>
    </UModal>

    <UModal v-model:open="isDeleteOpen" :title="t('courses.confirmDeleteTitle')">
      <template #body>
        <p>{{ t('courses.confirmDeleteBody') }}</p>
        <div class="flex justify-end gap-2 mt-6">
          <UButton variant="ghost" :label="t('common.cancel')" @click="deletingCourse = null" />
          <UButton color="error" :label="t('common.confirm')" @click="confirmDelete" />
        </div>
      </template>
    </UModal>
  </div>
</template>
