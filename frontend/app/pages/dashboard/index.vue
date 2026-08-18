<script setup lang="ts">
import type { DashboardResponse } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const authStore = useAuthStore()
const toast = useToast()

const dashboard = ref<DashboardResponse | null>(null)

try {
  dashboard.value = await request<DashboardResponse>('/dashboard')
}
catch {
  toast.add({ title: t('common.error'), color: 'error' })
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString()
}
</script>

<template>
  <div>
    <h1 class="text-xl font-semibold mb-1">
      {{ t('dashboard.welcome', { name: authStore.user?.fullName }) }}
    </h1>
    <p class="text-muted mb-8">
      {{ t('dashboard.role', { role: authStore.user?.roles.join(', ') }) }}
    </p>

    <h2 class="text-lg font-semibold mb-4">
      {{ t('dashboard.myCourses') }}
    </h2>

    <p v-if="dashboard && dashboard.courses.length === 0" class="text-muted mb-8">
      {{ t('dashboard.noCourses') }}
    </p>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 mb-8">
      <NuxtLink v-for="course in dashboard?.courses ?? []" :key="course.courseId" :to="`/courses/${course.courseId}`">
        <UCard class="h-full hover:ring-primary transition-shadow">
          <template #header>
            <div class="flex items-center justify-between gap-2">
              <span class="font-medium truncate">{{ course.courseTitle }}</span>
              <UBadge :color="course.status === 1 ? 'success' : 'primary'" variant="subtle">
                {{ course.status === 1 ? t('dashboard.statusCompleted') : t('dashboard.statusActive') }}
              </UBadge>
            </div>
          </template>

          <UProgress :model-value="course.completedDocuments" :max="course.totalDocuments || 1" class="mb-2" />
          <p class="text-sm text-muted">
            {{ t('dashboard.progress', { completed: course.completedDocuments, total: course.totalDocuments }) }}
          </p>
        </UCard>
      </NuxtLink>
    </div>

    <h2 class="text-lg font-semibold mb-4">
      {{ t('dashboard.recentlyAdded') }}
    </h2>

    <p v-if="dashboard && dashboard.recentlyAdded.length === 0" class="text-muted">
      {{ t('dashboard.noRecent') }}
    </p>

    <ul v-else class="space-y-2">
      <li v-for="item in dashboard?.recentlyAdded ?? []" :key="item.documentId" class="text-sm">
        <NuxtLink :to="`/courses/${item.courseId}`" class="hover:underline">
          {{ item.documentTitle }}
        </NuxtLink>
        <span class="text-muted">
          — {{ item.courseTitle }} · {{ formatDate(item.uploadedAtUtc) }}
        </span>
      </li>
    </ul>
  </div>
</template>
