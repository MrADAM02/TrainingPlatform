<script setup lang="ts">
const props = defineProps<{
  documentId: string
  caption?: string | null
}>()

const { t } = useI18n()
const { state, url, load } = useDocumentUrl(props.documentId)

await load()

const scale = ref(1)

function zoomIn() {
  scale.value = Math.min(3, scale.value + 0.5)
}

function zoomOut() {
  scale.value = Math.max(1, scale.value - 0.5)
}

async function download() {
  if (!url.value) return
  window.open(url.value, '_blank')
}
</script>

<template>
  <div>
    <div
      v-if="state === 'ready' && url"
      class="relative bg-elevated border border-default rounded-lg overflow-hidden h-90 flex items-center justify-center"
    >
      <img
        :src="url" :alt="caption ?? ''" class="max-w-none transition-transform"
        :style="{ transform: `scale(${scale})` }"
      >

      <div class="absolute top-2 inset-s-2 flex gap-1">
        <UButton size="xs" variant="solid" color="neutral" icon="i-lucide-zoom-in" square :aria-label="t('lessons.zoomIn')" @click="zoomIn" />
        <UButton size="xs" variant="solid" color="neutral" icon="i-lucide-zoom-out" square :aria-label="t('lessons.zoomOut')" @click="zoomOut" />
        <UButton size="xs" variant="solid" color="neutral" icon="i-lucide-download" square :aria-label="t('courses.documents.download')" @click="download" />
      </div>

      <div v-if="caption" class="absolute bottom-0 inset-x-0 bg-brand-900/85 text-white text-xs px-3 py-2">
        {{ caption }}
      </div>
    </div>

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
