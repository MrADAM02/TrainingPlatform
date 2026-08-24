<script setup lang="ts">
const { t } = useI18n()
const route = useRoute()

const tabs = computed(() => [
  { labelKey: 'nav.dashboard', icon: 'i-lucide-home', to: '/dashboard' },
  { labelKey: 'nav.courses', icon: 'i-lucide-layout-grid', to: '/courses' },
  { labelKey: 'nav.library', icon: 'i-lucide-bookmark', to: '/library' },
])

function isActive(to: string): boolean {
  return route.path === to || route.path.startsWith(`${to}/`)
}
</script>

<template>
  <nav
    class="sm:hidden print:hidden fixed bottom-0 inset-x-0 z-40 bg-default border-t border-default"
    role="navigation"
  >
    <div class="grid grid-cols-3">
      <NuxtLink
        v-for="tab in tabs" :key="tab.to" :to="tab.to"
        class="flex flex-col items-center gap-1 py-2 text-xs"
        :class="isActive(tab.to) ? 'text-primary font-medium' : 'text-muted'"
      >
        <UIcon :name="tab.icon" class="size-5" />
        {{ t(tab.labelKey) }}
      </NuxtLink>
    </div>
  </nav>
</template>
