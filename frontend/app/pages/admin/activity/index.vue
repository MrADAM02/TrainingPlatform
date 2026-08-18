<script setup lang="ts">
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

    <div class="overflow-x-auto rounded-lg border border-default">
      <table class="w-full text-sm">
        <thead class="bg-elevated/50">
          <tr>
            <th class="text-start p-3 font-medium">
              {{ t('admin.activity.timestamp') }}
            </th>
            <th class="text-start p-3 font-medium">
              {{ t('admin.activity.actor') }}
            </th>
            <th class="text-start p-3 font-medium">
              {{ t('admin.activity.action') }}
            </th>
            <th class="text-start p-3 font-medium">
              {{ t('admin.activity.entity') }}
            </th>
            <th class="text-start p-3 font-medium">
              {{ t('admin.activity.ipAddress') }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="entry in data?.items ?? []" :key="entry.id" class="border-t border-default">
            <td class="p-3 whitespace-nowrap">
              {{ formatDate(entry.timestampUtc) }}
            </td>
            <td class="p-3">
              {{ entry.userEmail ?? entry.userId }}
            </td>
            <td class="p-3">
              <UBadge variant="subtle">
                {{ entry.action }}
              </UBadge>
            </td>
            <td class="p-3">
              {{ entry.entityType }}<span v-if="entry.entityId" class="text-muted"> #{{ entry.entityId.slice(0, 8) }}</span>
            </td>
            <td class="p-3">
              {{ entry.ipAddress }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="data && data.totalPages > 1" class="flex items-center justify-between mt-4">
      <span class="text-sm text-muted">{{ t('pagination.pageOf', { page: data.page, totalPages: data.totalPages }) }}</span>
      <UPagination v-model:page="page" :total="data.totalCount" :items-per-page="pageSize" />
    </div>
  </div>
</template>
