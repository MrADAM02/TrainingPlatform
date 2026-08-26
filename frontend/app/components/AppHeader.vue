<script setup lang="ts">
const { t } = useI18n()
const navigation = useNavigation()
const route = useRoute()
const router = useRouter()
const { refresh: refreshBookmarkCount } = useBookmarkCount()

const isMenuOpen = ref(false)
const isSearchOpen = ref(false)
const searchKeyword = ref('')

// Same auto-close-on-navigate behavior the sidebar used to get for free from
// UDashboardSidebar's `autoClose` prop.
watch(() => route.fullPath, () => {
  isMenuOpen.value = false
  isSearchOpen.value = false
})

function submitSearch() {
  const keyword = searchKeyword.value.trim()
  router.push({ path: '/search', query: keyword ? { keyword } : undefined })
  isSearchOpen.value = false
}

// Client-only and non-blocking on purpose — this is a nav badge, not page content, so it
// shouldn't delay SSR for every single page just to show a count.
onMounted(() => {
  refreshBookmarkCount()
})

const menuItems = computed(() => [
  ...navigation.value.main,
  ...navigation.value.trainer,
  ...navigation.value.admin,
].map(item => ({ label: t(item.labelKey), icon: item.icon, to: item.to, badge: item.badge })))
</script>

<template>
  <header class="print:hidden sticky top-0 z-40 bg-default border-b border-default">
    <div class="flex items-center justify-between gap-4 px-4 sm:px-6 h-16">
      <div class="flex items-center gap-2 shrink-0">
        <UButton
          :icon="isMenuOpen ? 'i-lucide-x' : 'i-lucide-menu'"
          variant="ghost" color="neutral" square
          :aria-label="t('nav.menu')"
          @click="isMenuOpen = !isMenuOpen; isSearchOpen = false"
        />
        <span class="font-semibold truncate">{{ t('app.name') }}</span>
      </div>

      <form class="hidden sm:flex flex-1 max-w-sm" @submit.prevent="submitSearch">
        <UInput
          v-model="searchKeyword" icon="i-lucide-search" :placeholder="t('search.keyword')"
          class="w-full" :aria-label="t('search.title')"
        />
      </form>

      <div class="flex items-center gap-2 shrink-0">
        <UButton
          icon="i-lucide-search" variant="ghost" color="neutral" square class="sm:hidden"
          :aria-label="t('search.title')"
          @click="isSearchOpen = !isSearchOpen; isMenuOpen = false"
        />
        <AppUserMenu />
      </div>
    </div>

    <div v-if="isSearchOpen" class="sm:hidden border-t border-default p-2">
      <form @submit.prevent="submitSearch">
        <UInput
          v-model="searchKeyword" icon="i-lucide-search" :placeholder="t('search.keyword')"
          class="w-full" autofocus :aria-label="t('search.title')"
        />
      </form>
    </div>

    <div v-if="isMenuOpen" class="border-t border-default">
      <UNavigationMenu :items="menuItems" orientation="vertical" class="p-2 sm:px-4" />
    </div>
  </header>
</template>
