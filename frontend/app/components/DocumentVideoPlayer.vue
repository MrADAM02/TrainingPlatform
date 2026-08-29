<script setup lang="ts">
const props = defineProps<{
  documentId: string
  resumeFromSeconds?: number
}>()

const { t } = useI18n()
const { request } = useApi()
const { state, url, load } = useDocumentUrl(props.documentId)

const videoEl = ref<HTMLVideoElement | null>(null)
// Only save every ~10s of actual playback, not on every timeupdate tick (which fires several
// times a second) — a coarse resume point is all this needs, and it keeps request volume sane.
const SAVE_INTERVAL_SECONDS = 10
let lastSavedAt = props.resumeFromSeconds ?? 0

await load()

function saveProgress() {
  const video = videoEl.value
  if (!video) return
  request(`/documents/${props.documentId}/video-progress`, {
    method: 'POST',
    body: { positionSeconds: Math.floor(video.currentTime) },
  }).catch(() => {
    // Best-effort — a failed progress save shouldn't interrupt playback or show an error.
  })
}

function onLoadedMetadata() {
  const video = videoEl.value
  if (video && props.resumeFromSeconds && props.resumeFromSeconds < video.duration) {
    video.currentTime = props.resumeFromSeconds
  }
}

function onTimeUpdate() {
  const video = videoEl.value
  if (!video) return
  if (video.currentTime - lastSavedAt >= SAVE_INTERVAL_SECONDS) {
    lastSavedAt = video.currentTime
    saveProgress()
  }
}

onBeforeUnmount(saveProgress)
</script>

<template>
  <div>
    <video
      v-if="state === 'ready' && url" ref="videoEl" controls preload="metadata" class="w-full rounded-md"
      @loadedmetadata="onLoadedMetadata" @timeupdate="onTimeUpdate" @pause="saveProgress"
    >
      <source :src="url">
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
