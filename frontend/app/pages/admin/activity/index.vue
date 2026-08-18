<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { ActivityLogItem, PaginatedList } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const toast = useToast()

const page = ref(1)
const pageSize = 25
const data = ref<PaginatedList<ActivityLogItem> | null>(null)
const pending = ref(false)
const isExporting = ref(false)

async function fetchLog() {
  pending.value = true
  try {
    data.value = await request<PaginatedList<ActivityLogItem>>(`/activity-log?page=${page.value}&pageSize=${pageSize}`)
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
  finally {
    pending.value = false
  }
}

watch(page, fetchLog)
await fetchLog()

function formatDate(value: string): string {
  return new Date(value).toLocaleString()
}

const columns = computed<TableColumn<ActivityLogItem>[]>(() => [
  { accessorKey: 'timestampUtc', header: t('admin.activity.timestamp') },
  { accessorKey: 'userEmail', header: t('admin.activity.actor') },
  { accessorKey: 'action', header: t('admin.activity.action') },
  { id: 'entity', header: t('admin.activity.entity') },
  { accessorKey: 'ipAddress', header: t('admin.activity.ipAddress') },
])

async function exportCsv() {
  isExporting.value = true
  try {
    const blob = await request<Blob>('/activity-log/export', { responseType: 'blob' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `activity-log-${new Date().toISOString().slice(0, 10)}.csv`
    link.click()
    URL.revokeObjectURL(url)
  }
  catch {
    toast.add({ title: t('common.error'), color: 'error' })
  }
  finally {
    isExporting.value = false
  }
}
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-xl font-semibold">
        {{ t('admin.activity.title') }}
      </h1>
      <UButton
        variant="soft" icon="i-lucide-download" :loading="isExporting"
        :label="t('admin.activity.export')" @click="exportCsv"
      />
    </div>

    <div class="overflow-x-auto">
      <UTable :data="data?.items ?? []" :columns="columns" :loading="pending">
        <template #timestampUtc-cell="{ row }">
          <span class="whitespace-nowrap">{{ formatDate(row.original.timestampUtc) }}</span>
        </template>
        <template #userEmail-cell="{ row }">
          {{ row.original.userEmail ?? row.original.userId }}
        </template>
        <template #action-cell="{ row }">
          <UBadge variant="subtle">
            {{ row.original.action }}
          </UBadge>
        </template>
        <template #entity-cell="{ row }">
          {{ row.original.entityType }}<span v-if="row.original.entityId" class="text-muted"> #{{ row.original.entityId.slice(0, 8) }}</span>
        </template>
      </UTable>
    </div>

    <div v-if="data && data.totalPages > 1" class="flex items-center justify-between mt-4">
      <span class="text-sm text-muted">{{ t('pagination.pageOf', { page: data.page, totalPages: data.totalPages }) }}</span>
      <UPagination v-model:page="page" :total="data.totalCount" :items-per-page="pageSize" />
    </div>
  </div>
</template>
