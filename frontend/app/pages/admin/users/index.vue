<script setup lang="ts">
import type { FormError, FormSubmitEvent } from '@nuxt/ui'
import type { BulkImportUsersResult, PaginatedList, ProblemDetails, UserRole, UserSummary } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const toast = useToast()

const page = ref(1)
const pageSize = 20
const data = ref<PaginatedList<UserSummary> | null>(null)
const pending = ref(false)

const roleItems: UserRole[] = ['Administrator', 'Trainer', 'Trainee']

async function fetchUsers() {
  pending.value = true
  try {
    data.value = await request<PaginatedList<UserSummary>>(`/users?page=${page.value}&pageSize=${pageSize}`)
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
  finally {
    pending.value = false
  }
}

watch(page, fetchUsers)
await fetchUsers()

function errorDetail(error: unknown, fallback: string): string {
  const problem = (error as { data?: ProblemDetails })?.data
  return problem?.detail ?? fallback
}

// --- Create ---
const isCreateOpen = ref(false)
const createState = reactive({ email: '', fullName: '', password: '', role: 'Trainee' as UserRole })

function resetCreateForm() {
  createState.email = ''
  createState.fullName = ''
  createState.password = ''
  createState.role = 'Trainee'
}

function validateCreate(state: typeof createState): FormError[] {
  const errors: FormError[] = []
  if (!state.email) errors.push({ name: 'email', message: t('admin.users.email') })
  if (!state.fullName) errors.push({ name: 'fullName', message: t('admin.users.fullName') })
  if (!state.password || state.password.length < 8) errors.push({ name: 'password', message: t('admin.users.password') })
  return errors
}

async function submitCreate(event: FormSubmitEvent<typeof createState>) {
  try {
    await request('/users', { method: 'POST', body: event.data })
    toast.add({ title: t('admin.users.createSuccess'), color: 'success' })
    isCreateOpen.value = false
    resetCreateForm()
    await fetchUsers()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}

// --- Edit ---
const isEditOpen = ref(false)
const editingUser = ref<UserSummary | null>(null)
const editState = reactive({ fullName: '' })

function openEdit(user: UserSummary) {
  editingUser.value = user
  editState.fullName = user.fullName
  isEditOpen.value = true
}

function validateEdit(state: typeof editState): FormError[] {
  return state.fullName ? [] : [{ name: 'fullName', message: t('admin.users.fullName') }]
}

async function submitEdit() {
  if (!editingUser.value) return

  try {
    await request(`/users/${editingUser.value.id}`, { method: 'PUT', body: editState })
    toast.add({ title: t('admin.users.updateSuccess'), color: 'success' })
    isEditOpen.value = false
    await fetchUsers()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
}

// --- Activate / Deactivate / Delete confirmation ---
type ConfirmActionType = 'activate' | 'deactivate' | 'delete'
const confirmAction = ref<{ type: ConfirmActionType, user: UserSummary } | null>(null)
const isConfirmOpen = computed({
  get: () => confirmAction.value !== null,
  set: (value: boolean) => {
    if (!value) confirmAction.value = null
  },
})

function askConfirm(type: ConfirmActionType, user: UserSummary) {
  confirmAction.value = { type, user }
}

async function runConfirmedAction() {
  if (!confirmAction.value) return
  const { type, user } = confirmAction.value

  try {
    if (type === 'activate') {
      await request(`/users/${user.id}/activate`, { method: 'POST' })
      toast.add({ title: t('admin.users.activateSuccess'), color: 'success' })
    }
    else if (type === 'deactivate') {
      await request(`/users/${user.id}/deactivate`, { method: 'POST' })
      toast.add({ title: t('admin.users.deactivateSuccess'), color: 'success' })
    }
    else {
      await request(`/users/${user.id}`, { method: 'DELETE' })
      toast.add({ title: t('admin.users.deleteSuccess'), color: 'success' })
    }
    confirmAction.value = null
    await fetchUsers()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
    confirmAction.value = null
  }
}

// --- Reset password ---
const isResetConfirmOpen = ref(false)
const resettingUser = ref<UserSummary | null>(null)
const isTemporaryPasswordOpen = ref(false)
const temporaryPassword = ref('')

function askResetPassword(user: UserSummary) {
  resettingUser.value = user
  isResetConfirmOpen.value = true
}

async function runResetPassword() {
  if (!resettingUser.value) return

  try {
    const result = await request<{ temporaryPassword: string }>(
      `/users/${resettingUser.value.id}/reset-password`, { method: 'POST' },
    )
    temporaryPassword.value = result.temporaryPassword
    isResetConfirmOpen.value = false
    isTemporaryPasswordOpen.value = true
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
    isResetConfirmOpen.value = false
  }
}

function formatDate(value: string | null): string {
  return value ? new Date(value).toLocaleString() : t('admin.users.never')
}

// --- Bulk import ---
const isBulkImportOpen = ref(false)
const bulkImportFile = ref<File | null>(null)
const bulkImporting = ref(false)
const bulkImportResult = ref<BulkImportUsersResult | null>(null)

function openBulkImport() {
  bulkImportFile.value = null
  bulkImportResult.value = null
  isBulkImportOpen.value = true
}

function handleBulkImportFileSelected(file: File | File[] | null | undefined) {
  bulkImportFile.value = Array.isArray(file) ? (file[0] ?? null) : (file ?? null)
}

async function submitBulkImport() {
  if (!bulkImportFile.value) return

  bulkImporting.value = true
  try {
    const formData = new FormData()
    formData.append('file', bulkImportFile.value)
    bulkImportResult.value = await request<BulkImportUsersResult>('/users/bulk-import', {
      method: 'POST',
      body: formData,
    })
    await fetchUsers()
  }
  catch (error) {
    toast.add({ title: errorDetail(error, t('common.error')), color: 'error' })
  }
  finally {
    bulkImporting.value = false
  }
}
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-xl font-semibold">
        {{ t('admin.users.title') }}
      </h1>
      <div class="flex gap-2">
        <UButton :label="t('admin.users.bulkImport')" icon="i-lucide-upload" variant="soft" @click="openBulkImport" />
        <UButton :label="t('admin.users.create')" icon="i-lucide-plus" @click="isCreateOpen = true" />
      </div>
    </div>

    <div class="overflow-x-auto rounded-lg border border-default">
      <table class="w-full text-sm">
        <thead class="bg-elevated/50">
          <tr>
            <th class="text-start p-3 font-medium">
              {{ t('admin.users.email') }}
            </th>
            <th class="text-start p-3 font-medium">
              {{ t('admin.users.fullName') }}
            </th>
            <th class="text-start p-3 font-medium">
              {{ t('admin.users.role') }}
            </th>
            <th class="text-start p-3 font-medium">
              {{ t('admin.users.status') }}
            </th>
            <th class="text-start p-3 font-medium">
              {{ t('admin.users.lastLogin') }}
            </th>
            <th class="text-start p-3 font-medium">
              {{ t('admin.users.actions') }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in data?.items ?? []" :key="user.id" class="border-t border-default hover:bg-elevated/30">
            <td class="p-3">
              {{ user.email }}
            </td>
            <td class="p-3">
              {{ user.fullName }}
            </td>
            <td class="p-3">
              <UBadge v-for="role in user.roles" :key="role" variant="subtle" class="me-1">
                {{ role }}
              </UBadge>
            </td>
            <td class="p-3">
              <UBadge :color="user.isActive ? 'success' : 'neutral'" variant="subtle">
                {{ user.isActive ? t('admin.users.active') : t('admin.users.inactive') }}
              </UBadge>
            </td>
            <td class="p-3">
              {{ formatDate(user.lastLoginAtUtc) }}
            </td>
            <td class="p-3">
              <div class="flex flex-wrap gap-2">
                <UButton size="xs" variant="soft" :label="t('admin.users.edit')" @click="openEdit(user)" />
                <UButton
                  size="xs" variant="soft" :color="user.isActive ? 'warning' : 'success'"
                  :label="user.isActive ? t('admin.users.deactivate') : t('admin.users.activate')"
                  @click="askConfirm(user.isActive ? 'deactivate' : 'activate', user)"
                />
                <UButton size="xs" variant="soft" :label="t('admin.users.resetPassword')" @click="askResetPassword(user)" />
                <UButton size="xs" variant="soft" color="error" :label="t('admin.users.delete')" @click="askConfirm('delete', user)" />
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="data && data.totalPages > 1" class="flex items-center justify-between mt-4">
      <span class="text-sm text-muted">{{ t('pagination.pageOf', { page: data.page, totalPages: data.totalPages }) }}</span>
      <UPagination v-model:page="page" :total="data.totalCount" :items-per-page="pageSize" />
    </div>

    <!-- Create user -->
    <UModal v-model:open="isCreateOpen" :title="t('admin.users.create')">
      <template #body>
        <UForm :state="createState" :validate="validateCreate" @submit="submitCreate">
          <div class="space-y-4">
            <UFormField :label="t('admin.users.email')" name="email">
              <UInput v-model="createState.email" type="email" class="w-full" />
            </UFormField>
            <UFormField :label="t('admin.users.fullName')" name="fullName">
              <UInput v-model="createState.fullName" class="w-full" />
            </UFormField>
            <UFormField :label="t('admin.users.password')" name="password">
              <UInput v-model="createState.password" type="password" class="w-full" />
            </UFormField>
            <UFormField :label="t('admin.users.role')" name="role">
              <USelect v-model="createState.role" :items="roleItems" class="w-full" />
            </UFormField>
          </div>
          <div class="flex justify-end gap-2 mt-6">
            <UButton variant="ghost" :label="t('admin.users.cancel')" @click="isCreateOpen = false" />
            <UButton type="submit" :label="t('admin.users.save')" />
          </div>
        </UForm>
      </template>
    </UModal>

    <!-- Edit user -->
    <UModal v-model:open="isEditOpen" :title="t('admin.users.edit')">
      <template #body>
        <UForm :state="editState" :validate="validateEdit" @submit="submitEdit">
          <UFormField :label="t('admin.users.fullName')" name="fullName">
            <UInput v-model="editState.fullName" class="w-full" />
          </UFormField>
          <div class="flex justify-end gap-2 mt-6">
            <UButton variant="ghost" :label="t('admin.users.cancel')" @click="isEditOpen = false" />
            <UButton type="submit" :label="t('admin.users.save')" />
          </div>
        </UForm>
      </template>
    </UModal>

    <!-- Activate / Deactivate / Delete confirmation -->
    <UModal
      v-model:open="isConfirmOpen"
      :title="confirmAction?.type === 'delete' ? t('admin.users.confirmDeleteTitle') : t('admin.users.confirmDeactivateTitle')"
    >
      <template #body>
        <p v-if="confirmAction?.type === 'delete'">
          {{ t('admin.users.confirmDeleteBody') }}
        </p>
        <p v-else-if="confirmAction?.type === 'deactivate'">
          {{ t('admin.users.confirmDeactivateBody') }}
        </p>
        <div class="flex justify-end gap-2 mt-6">
          <UButton variant="ghost" :label="t('common.cancel')" @click="confirmAction = null" />
          <UButton
            :color="confirmAction?.type === 'delete' ? 'error' : 'primary'"
            :label="t('common.confirm')"
            @click="runConfirmedAction"
          />
        </div>
      </template>
    </UModal>

    <!-- Reset password confirmation -->
    <UModal v-model:open="isResetConfirmOpen" :title="t('admin.users.confirmResetTitle')">
      <template #body>
        <p>{{ t('admin.users.confirmResetBody') }}</p>
        <div class="flex justify-end gap-2 mt-6">
          <UButton variant="ghost" :label="t('common.cancel')" @click="isResetConfirmOpen = false" />
          <UButton :label="t('common.confirm')" @click="runResetPassword" />
        </div>
      </template>
    </UModal>

    <!-- Temporary password display -->
    <UModal v-model:open="isTemporaryPasswordOpen" :title="t('admin.users.temporaryPasswordTitle')">
      <template #body>
        <p class="mb-3 text-sm text-muted">
          {{ t('admin.users.temporaryPasswordBody') }}
        </p>
        <UInput :model-value="temporaryPassword" readonly class="w-full font-mono" />
        <div class="flex justify-end mt-6">
          <UButton :label="t('admin.users.close')" @click="isTemporaryPasswordOpen = false" />
        </div>
      </template>
    </UModal>

    <!-- Bulk import -->
    <UModal v-model:open="isBulkImportOpen" :title="t('admin.users.bulkImport')">
      <template #body>
        <template v-if="!bulkImportResult">
          <p class="text-sm text-muted mb-4">
            {{ t('admin.users.bulkImportInstructions') }}
          </p>
          <UFileUpload accept=".csv,text/csv" :model-value="null" @update:model-value="handleBulkImportFileSelected">
            <template #default="{ open }">
              <UButton
                variant="outline" icon="i-lucide-file-up"
                :label="bulkImportFile ? bulkImportFile.name : t('admin.users.chooseFile')"
                @click="() => open()"
              />
            </template>
          </UFileUpload>
          <div class="flex justify-end gap-2 mt-6">
            <UButton variant="ghost" :label="t('admin.users.cancel')" @click="isBulkImportOpen = false" />
            <UButton
              :label="t('admin.users.import')" :loading="bulkImporting" :disabled="!bulkImportFile"
              @click="submitBulkImport"
            />
          </div>
        </template>
        <template v-else>
          <p class="text-sm mb-3">
            {{ t('admin.users.bulkImportSummary', { created: bulkImportResult.created.length, failed: bulkImportResult.failed.length }) }}
          </p>
          <div v-if="bulkImportResult.created.length" class="mb-4">
            <p class="text-sm font-medium mb-1">
              {{ t('admin.users.bulkImportCreated') }}
            </p>
            <div class="max-h-48 overflow-y-auto rounded border border-default text-sm">
              <table class="w-full">
                <thead class="bg-elevated/50">
                  <tr>
                    <th class="text-start p-2 font-medium">
                      {{ t('admin.users.email') }}
                    </th>
                    <th class="text-start p-2 font-medium">
                      {{ t('admin.users.role') }}
                    </th>
                    <th class="text-start p-2 font-medium">
                      {{ t('admin.users.password') }}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="row in bulkImportResult.created" :key="row.email" class="border-t border-default">
                    <td class="p-2">
                      {{ row.email }}
                    </td>
                    <td class="p-2">
                      {{ row.role }}
                    </td>
                    <td class="p-2 font-mono">
                      {{ row.temporaryPassword }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
          <div v-if="bulkImportResult.failed.length">
            <p class="text-sm font-medium mb-1 text-error">
              {{ t('admin.users.bulkImportFailed') }}
            </p>
            <div class="max-h-48 overflow-y-auto rounded border border-default text-sm">
              <table class="w-full">
                <thead class="bg-elevated/50">
                  <tr>
                    <th class="text-start p-2 font-medium">
                      {{ t('admin.users.row') }}
                    </th>
                    <th class="text-start p-2 font-medium">
                      {{ t('admin.users.email') }}
                    </th>
                    <th class="text-start p-2 font-medium">
                      {{ t('admin.users.reason') }}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="row in bulkImportResult.failed" :key="row.rowNumber" class="border-t border-default">
                    <td class="p-2">
                      {{ row.rowNumber }}
                    </td>
                    <td class="p-2">
                      {{ row.email }}
                    </td>
                    <td class="p-2">
                      {{ row.reason }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
          <div class="flex justify-end mt-6">
            <UButton :label="t('admin.users.close')" @click="isBulkImportOpen = false" />
          </div>
        </template>
      </template>
    </UModal>
  </div>
</template>
