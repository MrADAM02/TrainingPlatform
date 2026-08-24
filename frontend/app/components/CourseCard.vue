<script setup lang="ts">
// Shared card for course grids (browse, dashboard, trainer management). `to` makes the whole
// card a link (browse/dashboard); omit it for management mode, where the #footer slot's own
// action buttons (Manage/Publish/Delete) own navigation instead. `isPublished` and `isEnrolled`
// are independent optional badges — pass whichever is relevant to the calling page's context,
// matching the two badge behaviors the pre-redesign pages already had.
const props = withDefaults(defineProps<{
  id: string
  title: string
  description?: string | null
  to?: string
  isPublished?: boolean
  isEnrolled?: boolean
  // undefined = don't render the bookmark toggle at all (e.g. trainer management mode).
  isBookmarked?: boolean
}>(), {
  description: null,
  to: undefined,
  isPublished: undefined,
  isEnrolled: undefined,
  isBookmarked: undefined,
})

const emit = defineEmits<{
  toggleBookmark: [id: string]
}>()

const { t } = useI18n()

function onToggleBookmark(event: Event) {
  event.preventDefault()
  event.stopPropagation()
  emit('toggleBookmark', props.id)
}

// resolveComponent, not the string 'NuxtLink', so :is actually resolves to the component
// instead of rendering an inert <NuxtLink> custom-element tag with no click behavior.
const NuxtLinkComponent = resolveComponent('NuxtLink')
</script>

<template>
  <component :is="to ? NuxtLinkComponent : 'div'" :to="to" class="block h-full">
    <UCard class="h-full overflow-hidden p-0 gap-0" :class="to ? 'hover:ring-primary transition-shadow' : ''" :ui="{ body: 'p-4 sm:p-4' }">
      <CourseCoverPlaceholder :id="id" :title="title" size="md">
        <UBadge
          v-if="isPublished !== undefined"
          :color="isPublished ? 'success' : 'neutral'" variant="solid"
          class="absolute top-2 inset-s-2"
        >
          {{ isPublished ? t('courses.published') : t('courses.draft') }}
        </UBadge>
        <UBadge v-else-if="isEnrolled" color="success" variant="solid" class="absolute top-2 inset-s-2">
          {{ t('courses.enrolled') }}
        </UBadge>

        <button
          v-if="isBookmarked !== undefined" type="button"
          class="absolute top-2 inset-e-2 flex items-center justify-center size-7 rounded-full bg-default/90 shadow-sm"
          :aria-label="isBookmarked ? t('courses.unbookmark') : t('courses.bookmark')"
          @click="onToggleBookmark"
        >
          <UIcon
            name="i-lucide-bookmark" class="size-4"
            :class="isBookmarked ? 'fill-current text-primary' : 'text-muted'"
          />
        </button>
      </CourseCoverPlaceholder>

      <div class="p-4">
        <h3 class="font-medium truncate">
          {{ title }}
        </h3>
        <p v-if="description" class="text-sm text-muted line-clamp-3 mt-1">
          {{ description }}
        </p>
      </div>

      <div v-if="$slots.footer" class="px-4 pb-4">
        <slot name="footer" />
      </div>
    </UCard>
  </component>
</template>
