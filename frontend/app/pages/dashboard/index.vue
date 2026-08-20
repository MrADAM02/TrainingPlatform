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

const stats = computed(() => {
  const courses = dashboard.value?.courses ?? []
  return {
    enrolled: courses.length,
    active: courses.filter(c => c.status === 0).length,
    completed: courses.filter(c => c.status === 1).length,
    materials: courses.reduce((sum, c) => sum + c.totalDocuments, 0),
  }
})
</script>

<template>
  <div>
    <h1 class="text-xl font-semibold mb-1">
      {{ t('dashboard.welcome', { name: authStore.user?.fullName }) }}
    </h1>
    <p class="text-muted mb-6">
      {{ t('dashboard.role', { role: authStore.user?.roles.join(', ') }) }}
    </p>

    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-8">
      <StatCard icon="i-lucide-book-open" :value="stats.enrolled" :label="t('dashboard.stats.enrolled')" />
      <StatCard icon="i-lucide-play-circle" :value="stats.active" :label="t('dashboard.stats.active')" />
      <StatCard icon="i-lucide-check-circle" :value="stats.completed" :label="t('dashboard.stats.completed')" />
      <StatCard icon="i-lucide-file-text" :value="stats.materials" :label="t('dashboard.stats.materials')" />
    </div>

    <h2 class="text-lg font-semibold mb-4">
      {{ t('dashboard.continueLearning') }}
    </h2>

    <p v-if="dashboard && dashboard.courses.length === 0" class="text-muted mb-8">
      {{ t('dashboard.noCourses') }}
    </p>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 mb-8">
      <!-- The certificate button below lives outside CourseCard's own link wrapper — nesting
           an interactive element inside it would produce an invalid <a>/<button> inside <a>. -->
      <div v-for="course in dashboard?.courses ?? []" :key="course.courseId">
        <CourseCard :id="course.courseId" :title="course.courseTitle" :to="`/courses/${course.courseId}`">
          <template #footer>
            <div class="flex items-center justify-between gap-2 mb-2">
              <UBadge :color="course.status === 1 ? 'success' : 'primary'" variant="subtle">
                {{ course.status === 1 ? t('dashboard.statusCompleted') : t('dashboard.statusActive') }}
              </UBadge>
            </div>
            <UProgress :model-value="course.completedDocuments" :max="course.totalDocuments || 1" class="mb-2" />
            <p class="text-sm text-muted">
              {{ t('dashboard.progress', { completed: course.completedDocuments, total: course.totalDocuments }) }}
            </p>
          </template>
        </CourseCard>

        <UButton
          v-if="course.status === 1" variant="soft" size="xs" icon="i-lucide-award"
          :label="t('certificates.view')" to="/certificates" class="mt-2"
        />
      </div>
    </div>

    <h2 class="text-lg font-semibold mb-4">
      {{ t('dashboard.recentlyAdded') }}
    </h2>

    <p v-if="dashboard && dashboard.recentlyAdded.length === 0" class="text-muted">
      {{ t('dashboard.noRecent') }}
    </p>

    <UCard v-else :ui="{ body: 'p-0 sm:p-0' }">
      <ul class="divide-y divide-default">
        <li v-for="item in dashboard?.recentlyAdded ?? []" :key="item.documentId" class="p-3 text-sm">
          <NuxtLink :to="`/courses/${item.courseId}`" class="hover:underline font-medium">
            {{ item.documentTitle }}
          </NuxtLink>
          <span class="text-muted">
            — {{ item.courseTitle }} · {{ formatDate(item.uploadedAtUtc) }}
          </span>
        </li>
      </ul>
    </UCard>
  </div>
</template>
