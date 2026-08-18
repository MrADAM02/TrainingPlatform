<script setup lang="ts">
import type { FormError, FormSubmitEvent } from '@nuxt/ui'
import type { CourseDetails, DocumentSummary, EnrollmentSummary, ModuleDetails, ProblemDetails, UploadTicket, UserSummary } from '~/types/api'
import { documentTypeLabels } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const toast = useToast()
const route = useRoute()
const router = useRouter()

const courseId = route.params.id as string
const course = ref<CourseDetails | null>(null)

async function fetchCourse() {
  try {
    course.value = await request<CourseDetails>(`/courses/${courseId}`)
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
    await router.push('/trainer')
  }
}

await fetchCourse()

function errorDetail(error: unknown, fallback: string): string {
  return (error as { data?: ProblemDetails })?.data?.detail ?? fallback
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

// --- Edit course ---
const isEditOpen = ref(false)
const editState = reactive({ title: '', description: '' })

function openEdit() {
  if (!course.value) return
  editState.title = course.value.title
  editState.description = course.value.description
  isEditOpen.value = true
}

function validateCourse(state: typeof editState): FormError[] {
  const errors: FormError[] = []
  if (!state.title) errors.push({ name: 'title', message: t('courses.courseTitle') })
  if (!state.description) errors.push({ name: 'description', message: t('courses.description') })
  return errors
}

async function submitEdit(event: FormSubmitEvent<typeof editState>) {
  try {
    await request(`/courses/${courseId}`, { method: 'PUT', body: event.data })
    toast.add({ title: t('courses.updateSuccess'), color: 'success' })
    isEditOpen.value = false
    await fetchCourse()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}

async function togglePublish() {
  if (!course.value) return
  try {
    await request(`/courses/${courseId}/${course.value.isPublished ? 'unpublish' : 'publish'}`, { method: 'POST' })
    toast.add({ title: t(course.value.isPublished ? 'courses.unpublishSuccess' : 'courses.publishSuccess'), color: 'success' })
    await fetchCourse()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}

async function deleteCourse() {
  try {
    await request(`/courses/${courseId}`, { method: 'DELETE' })
    toast.add({ title: t('courses.deleteSuccess'), color: 'success' })
    await router.push('/trainer')
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}
const isDeleteCourseOpen = ref(false)

// --- Modules ---
const isModuleFormOpen = ref(false)
const editingModule = ref<ModuleDetails | null>(null)
const moduleState = reactive({ title: '', order: 1 })

function openCreateModule() {
  editingModule.value = null
  moduleState.title = ''
  moduleState.order = (course.value?.modules.length ?? 0) + 1
  isModuleFormOpen.value = true
}

function openEditModule(module: ModuleDetails) {
  editingModule.value = module
  moduleState.title = module.title
  moduleState.order = module.order
  isModuleFormOpen.value = true
}

function validateModule(state: typeof moduleState): FormError[] {
  return state.title ? [] : [{ name: 'title', message: t('courses.modules.moduleTitle') }]
}

async function submitModule(event: FormSubmitEvent<typeof moduleState>) {
  try {
    if (editingModule.value) {
      await request(`/modules/${editingModule.value.id}`, { method: 'PUT', body: event.data })
      toast.add({ title: t('courses.modules.updateSuccess'), color: 'success' })
    }
    else {
      await request(`/courses/${courseId}/modules`, { method: 'POST', body: event.data })
      toast.add({ title: t('courses.modules.createSuccess'), color: 'success' })
    }
    isModuleFormOpen.value = false
    await fetchCourse()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}

const deletingModule = ref<ModuleDetails | null>(null)
const isDeleteModuleOpen = computed({
  get: () => deletingModule.value !== null,
  set: (value: boolean) => { if (!value) deletingModule.value = null },
})

async function confirmDeleteModule() {
  if (!deletingModule.value) return
  try {
    await request(`/modules/${deletingModule.value.id}`, { method: 'DELETE' })
    toast.add({ title: t('courses.modules.deleteSuccess'), color: 'success' })
    deletingModule.value = null
    await fetchCourse()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
    deletingModule.value = null
  }
}

// --- Documents ---
const uploadingModuleId = ref<string | null>(null)

async function handleFileSelected(moduleId: string, file: File | File[] | null | undefined) {
  if (!file || Array.isArray(file)) return

  uploadingModuleId.value = moduleId
  try {
    const contentType = file.type || 'application/octet-stream'
    const ticket = await request<UploadTicket>(`/modules/${moduleId}/documents/upload-url`, {
      method: 'POST',
      body: { title: file.name, fileName: file.name, contentType, sizeBytes: file.size },
    })

    // Uploads directly to object storage — must NOT go through useApi()/$api, which would
    // attach our own API's bearer token and base URL to a request meant for a different host,
    // already pre-authorized via the presigned URL's query-string signature.
    await $fetch(ticket.uploadUrl, {
      method: 'PUT',
      body: file,
      headers: { 'Content-Type': contentType },
    })

    toast.add({ title: t('courses.documents.uploadSuccess'), color: 'success' })
    await fetchCourse()
  }
  catch {
    toast.add({ title: t('courses.documents.uploadError'), color: 'error' })
  }
  finally {
    uploadingModuleId.value = null
  }
}

async function downloadDocument(doc: DocumentSummary) {
  try {
    const result = await request<{ url: string }>(`/documents/${doc.id}/download-url`)
    window.open(result.url, '_blank')
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}

const deletingDocument = ref<DocumentSummary | null>(null)
const isDeleteDocumentOpen = computed({
  get: () => deletingDocument.value !== null,
  set: (value: boolean) => { if (!value) deletingDocument.value = null },
})

async function confirmDeleteDocument() {
  if (!deletingDocument.value) return
  try {
    await request(`/documents/${deletingDocument.value.id}`, { method: 'DELETE' })
    toast.add({ title: t('courses.documents.deleteSuccess'), color: 'success' })
    deletingDocument.value = null
    await fetchCourse()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
    deletingDocument.value = null
  }
}

// --- Enrollments ---
const enrollments = ref<EnrollmentSummary[]>([])

async function fetchEnrollments() {
  try {
    enrollments.value = await request<EnrollmentSummary[]>(`/courses/${courseId}/enrollments`)
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
}

await fetchEnrollments()

const isEnrollOpen = ref(false)
const traineeKeyword = ref('')
const traineeResults = ref<UserSummary[]>([])
const selectedTraineeIds = ref<Set<string>>(new Set())
let traineeSearchTimer: ReturnType<typeof setTimeout> | undefined

function openEnroll() {
  traineeKeyword.value = ''
  traineeResults.value = []
  selectedTraineeIds.value = new Set()
  isEnrollOpen.value = true
}

async function searchTrainees() {
  try {
    traineeResults.value = await request<UserSummary[]>(
      `/users/trainees?keyword=${encodeURIComponent(traineeKeyword.value)}`,
    )
  }
  catch {
    traineeResults.value = []
  }
}

watch(traineeKeyword, () => {
  clearTimeout(traineeSearchTimer)
  traineeSearchTimer = setTimeout(searchTrainees, 300)
})

// Populate the list immediately when the modal opens, before the user types anything.
watch(isEnrollOpen, (open) => {
  if (open) searchTrainees()
})

function toggleTraineeSelection(userId: string) {
  if (selectedTraineeIds.value.has(userId)) {
    selectedTraineeIds.value.delete(userId)
  }
  else {
    selectedTraineeIds.value.add(userId)
  }
}

async function submitEnroll() {
  if (selectedTraineeIds.value.size === 0) return

  try {
    await request(`/courses/${courseId}/enrollments`, {
      method: 'POST',
      body: { userIds: [...selectedTraineeIds.value] },
    })
    toast.add({ title: t('courses.enrollments.enrollSuccess'), color: 'success' })
    isEnrollOpen.value = false
    await fetchEnrollments()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}

const removingEnrollment = ref<EnrollmentSummary | null>(null)
const isRemoveEnrollmentOpen = computed({
  get: () => removingEnrollment.value !== null,
  set: (value: boolean) => { if (!value) removingEnrollment.value = null },
})

async function confirmRemoveEnrollment() {
  if (!removingEnrollment.value) return
  try {
    await request(`/enrollments/${removingEnrollment.value.id}`, { method: 'DELETE' })
    toast.add({ title: t('courses.enrollments.unenrollSuccess'), color: 'success' })
    removingEnrollment.value = null
    await fetchEnrollments()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
    removingEnrollment.value = null
  }
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString()
}
</script>

<template>
  <div v-if="course">
    <UButton variant="ghost" icon="i-lucide-arrow-left" :label="t('courses.back')" to="/trainer" class="mb-4" />

    <div class="flex items-start justify-between gap-4 mb-2">
      <div>
        <h1 class="text-xl font-semibold">
          {{ course.title }}
        </h1>
        <p class="text-muted mt-1">
          {{ course.description }}
        </p>
      </div>
      <UBadge :color="course.isPublished ? 'success' : 'neutral'" variant="subtle">
        {{ course.isPublished ? t('courses.published') : t('courses.draft') }}
      </UBadge>
    </div>

    <div class="flex flex-wrap gap-2 mb-8">
      <UButton size="sm" variant="soft" :label="t('courses.edit')" @click="openEdit" />
      <UButton
        size="sm" variant="soft" :color="course.isPublished ? 'warning' : 'primary'"
        :label="course.isPublished ? t('courses.unpublish') : t('courses.publish')"
        @click="togglePublish"
      />
      <UButton size="sm" variant="soft" color="error" :label="t('courses.delete')" @click="isDeleteCourseOpen = true" />
    </div>

    <div class="flex items-center justify-between mb-4">
      <h2 class="text-lg font-semibold">
        {{ t('courses.modules.title') }}
      </h2>
      <UButton size="sm" :label="t('courses.modules.create')" icon="i-lucide-plus" @click="openCreateModule" />
    </div>

    <p v-if="course.modules.length === 0" class="text-muted">
      {{ t('courses.modules.empty') }}
    </p>

    <div v-for="module in course.modules" :key="module.id" class="mb-6 rounded-lg border border-default p-4">
      <div class="flex items-center justify-between mb-3">
        <span class="font-medium">{{ module.order }}. {{ module.title }}</span>
        <div class="flex gap-2">
          <UButton size="xs" variant="soft" :label="t('courses.modules.edit')" @click="openEditModule(module)" />
          <UButton size="xs" variant="soft" color="error" :label="t('courses.modules.delete')" @click="deletingModule = module" />
        </div>
      </div>

      <table v-if="module.documents.length > 0" class="w-full text-sm mb-3">
        <thead>
          <tr class="text-start text-muted">
            <th class="text-start p-2 font-medium">
              {{ t('courses.documents.title') }}
            </th>
            <th class="text-start p-2 font-medium">
              {{ t('courses.documents.size') }}
            </th>
            <th class="text-start p-2 font-medium" />
          </tr>
        </thead>
        <tbody>
          <tr v-for="doc in module.documents" :key="doc.id" class="border-t border-default">
            <td class="p-2">
              {{ doc.title }}
              <UBadge variant="subtle" size="sm" class="ms-2">
                {{ documentTypeLabels[doc.fileType] }}
              </UBadge>
            </td>
            <td class="p-2">
              {{ formatBytes(doc.sizeBytes) }}
            </td>
            <td class="p-2">
              <div class="flex justify-end gap-2">
                <UButton size="xs" variant="soft" :label="t('courses.documents.download')" @click="downloadDocument(doc)" />
                <UButton size="xs" variant="soft" color="error" :label="t('courses.documents.delete')" @click="deletingDocument = doc" />
              </div>
            </td>
          </tr>
        </tbody>
      </table>
      <p v-else class="text-sm text-muted mb-3">
        {{ t('courses.documents.empty') }}
      </p>

      <UFileUpload
        :model-value="null"
        :disabled="uploadingModuleId === module.id"
        @update:model-value="(file) => handleFileSelected(module.id, file)"
      >
        <template #default="{ open }">
          <UButton
            size="xs" variant="outline" icon="i-lucide-upload"
            :loading="uploadingModuleId === module.id"
            :label="uploadingModuleId === module.id ? t('courses.documents.uploading') : t('courses.documents.upload')"
            @click="() => open()"
          />
        </template>
      </UFileUpload>
    </div>

    <div class="flex items-center justify-between mb-4 mt-8">
      <h2 class="text-lg font-semibold">
        {{ t('courses.enrollments.title') }}
      </h2>
      <UButton size="sm" :label="t('courses.enrollments.enroll')" icon="i-lucide-user-plus" @click="openEnroll" />
    </div>

    <p v-if="enrollments.length === 0" class="text-muted">
      {{ t('courses.enrollments.empty') }}
    </p>

    <table v-else class="w-full text-sm">
      <thead>
        <tr class="text-muted">
          <th class="text-start p-2 font-medium">
            {{ t('admin.users.email') }}
          </th>
          <th class="text-start p-2 font-medium">
            {{ t('admin.users.fullName') }}
          </th>
          <th class="text-start p-2 font-medium">
            {{ t('courses.status') }}
          </th>
          <th class="text-start p-2 font-medium">
            {{ t('courses.enrollments.enrolledAt') }}
          </th>
          <th class="text-start p-2 font-medium" />
        </tr>
      </thead>
      <tbody>
        <tr v-for="enrollment in enrollments" :key="enrollment.id" class="border-t border-default">
          <td class="p-2">
            {{ enrollment.userEmail }}
          </td>
          <td class="p-2">
            {{ enrollment.userFullName }}
          </td>
          <td class="p-2">
            <UBadge :color="enrollment.status === 1 ? 'success' : 'primary'" variant="subtle">
              {{ enrollment.status === 1 ? t('courses.enrollments.statusCompleted') : t('courses.enrollments.statusActive') }}
            </UBadge>
          </td>
          <td class="p-2">
            {{ formatDate(enrollment.enrolledAtUtc) }}
          </td>
          <td class="p-2 text-end">
            <UButton size="xs" variant="soft" color="error" :label="t('courses.enrollments.unenroll')" @click="removingEnrollment = enrollment" />
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Edit course -->
    <UModal v-model:open="isEditOpen" :title="t('courses.edit')">
      <template #body>
        <UForm :state="editState" :validate="validateCourse" @submit="submitEdit">
          <div class="space-y-4">
            <UFormField :label="t('courses.courseTitle')" name="title">
              <UInput v-model="editState.title" class="w-full" />
            </UFormField>
            <UFormField :label="t('courses.description')" name="description">
              <UTextarea v-model="editState.description" class="w-full" :rows="4" />
            </UFormField>
          </div>
          <div class="flex justify-end gap-2 mt-6">
            <UButton variant="ghost" :label="t('courses.cancel')" @click="isEditOpen = false" />
            <UButton type="submit" :label="t('courses.save')" />
          </div>
        </UForm>
      </template>
    </UModal>

    <!-- Delete course -->
    <UModal v-model:open="isDeleteCourseOpen" :title="t('courses.confirmDeleteTitle')">
      <template #body>
        <p>{{ t('courses.confirmDeleteBody') }}</p>
        <div class="flex justify-end gap-2 mt-6">
          <UButton variant="ghost" :label="t('common.cancel')" @click="isDeleteCourseOpen = false" />
          <UButton color="error" :label="t('common.confirm')" @click="deleteCourse" />
        </div>
      </template>
    </UModal>

    <!-- Create/edit module -->
    <UModal v-model:open="isModuleFormOpen" :title="editingModule ? t('courses.modules.edit') : t('courses.modules.create')">
      <template #body>
        <UForm :state="moduleState" :validate="validateModule" @submit="submitModule">
          <div class="space-y-4">
            <UFormField :label="t('courses.modules.moduleTitle')" name="title">
              <UInput v-model="moduleState.title" class="w-full" />
            </UFormField>
            <UFormField :label="t('courses.modules.order')" name="order">
              <UInput v-model.number="moduleState.order" type="number" class="w-full" />
            </UFormField>
          </div>
          <div class="flex justify-end gap-2 mt-6">
            <UButton variant="ghost" :label="t('courses.cancel')" @click="isModuleFormOpen = false" />
            <UButton type="submit" :label="t('courses.save')" />
          </div>
        </UForm>
      </template>
    </UModal>

    <!-- Delete module -->
    <UModal v-model:open="isDeleteModuleOpen" :title="t('courses.modules.confirmDeleteTitle')">
      <template #body>
        <p>{{ t('courses.modules.confirmDeleteBody') }}</p>
        <div class="flex justify-end gap-2 mt-6">
          <UButton variant="ghost" :label="t('common.cancel')" @click="deletingModule = null" />
          <UButton color="error" :label="t('common.confirm')" @click="confirmDeleteModule" />
        </div>
      </template>
    </UModal>

    <!-- Delete document -->
    <UModal v-model:open="isDeleteDocumentOpen" :title="t('courses.documents.confirmDeleteTitle')">
      <template #body>
        <p>{{ t('courses.documents.confirmDeleteBody') }}</p>
        <div class="flex justify-end gap-2 mt-6">
          <UButton variant="ghost" :label="t('common.cancel')" @click="deletingDocument = null" />
          <UButton color="error" :label="t('common.confirm')" @click="confirmDeleteDocument" />
        </div>
      </template>
    </UModal>

    <!-- Enroll trainees -->
    <UModal v-model:open="isEnrollOpen" :title="t('courses.enrollments.enroll')">
      <template #body>
        <UInput
          v-model="traineeKeyword" :placeholder="t('courses.enrollments.searchPlaceholder')"
          icon="i-lucide-search" class="w-full mb-4"
        />

        <p v-if="traineeResults.length === 0" class="text-muted text-sm">
          {{ t('courses.enrollments.noResults') }}
        </p>

        <ul v-else class="space-y-1 max-h-64 overflow-y-auto">
          <li v-for="trainee in traineeResults" :key="trainee.id">
            <label class="flex items-center gap-2 py-1 cursor-pointer">
              <UCheckbox
                :model-value="selectedTraineeIds.has(trainee.id)"
                @update:model-value="toggleTraineeSelection(trainee.id)"
              />
              <span class="text-sm">{{ trainee.fullName }} · {{ trainee.email }}</span>
            </label>
          </li>
        </ul>

        <div class="flex justify-end gap-2 mt-6">
          <UButton variant="ghost" :label="t('courses.cancel')" @click="isEnrollOpen = false" />
          <UButton :disabled="selectedTraineeIds.size === 0" :label="t('courses.save')" @click="submitEnroll" />
        </div>
      </template>
    </UModal>

    <!-- Remove enrollment -->
    <UModal v-model:open="isRemoveEnrollmentOpen" :title="t('courses.enrollments.confirmUnenrollTitle')">
      <template #body>
        <p>{{ t('courses.enrollments.confirmUnenrollBody') }}</p>
        <div class="flex justify-end gap-2 mt-6">
          <UButton variant="ghost" :label="t('common.cancel')" @click="removingEnrollment = null" />
          <UButton color="error" :label="t('common.confirm')" @click="confirmRemoveEnrollment" />
        </div>
      </template>
    </UModal>
  </div>
</template>
