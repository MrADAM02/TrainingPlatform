<script setup lang="ts">
// Deterministic gradient+icon cover for a course card, in place of a real thumbnail (see
// useCourseCover.ts for why). This is the seam to swap in a real <img> later.
const props = withDefaults(defineProps<{
  id: string
  title: string
  size?: 'sm' | 'md' | 'hero'
}>(), {
  size: 'md',
})

const cover = computed(() => useCourseCover(props.id, props.title))

const sizeClasses: Record<string, string> = {
  sm: 'aspect-square rounded-md',
  md: 'aspect-video rounded-t-lg',
  hero: 'aspect-3/1 sm:aspect-4/1 rounded-lg',
}

const iconSizeClasses: Record<string, string> = {
  sm: 'size-5',
  md: 'size-10',
  hero: 'size-14',
}
</script>

<template>
  <div
    class="relative flex items-center justify-center overflow-hidden bg-linear-to-br"
    :class="[cover.gradient, sizeClasses[size]]"
  >
    <span class="font-bold text-white/25 select-none" :class="size === 'hero' ? 'text-6xl' : 'text-3xl'">
      {{ cover.initials }}
    </span>
    <UIcon :name="cover.icon" class="absolute text-white/90" :class="iconSizeClasses[size]" />
    <slot />
  </div>
</template>
