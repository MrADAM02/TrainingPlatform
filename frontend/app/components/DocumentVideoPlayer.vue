<script setup lang="ts">
const props = defineProps<{
  documentId: string
}>()

const { t } = useI18n()
const { request } = useApi()

type PlayerState = 'loading' | 'ready' | 'error'

const state = ref<PlayerState>('loading')
const videoUrl = ref<string | null>(null)

async function load() {
  state.value = 'loading'
  try {
    const result = await request<{ url: string }>(`/documents/${props.documentId}/download-url`)
    videoUrl.value = result.url
    state.value = 'ready'
  }
  catch {
    state.value = 'error'
  }
}

await load()
</script>

<template>
  <div>
    <video v-if="state === 'ready' && videoUrl" controls preload="metadata" class="w-full rounded-md">
      <source :src="videoUrl">
    </video>

    <div v-else-if="state === 'loading'" class="flex items-center gap-2 text-sm text-muted py-2">
      <UIcon name="i-lucide-loader-2" class="animate-spin" />
      {{ t('common.loading') }}
    </div>

    <UAlert
      v-else color="error" variant="subtle" icon="i-lucide-triangle-alert"
      :title="t('courses.documents.watchError')"
    >
      <template #actions>
        <UButton size="xs" color="error" :label="t('courses.documents.retry')" @click="load" />
      </template>
    </UAlert>
  </div>
</template>
