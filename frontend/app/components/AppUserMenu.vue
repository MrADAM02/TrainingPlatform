<script setup lang="ts">
const { t, locale, locales, setLocale } = useI18n()
const authStore = useAuthStore()
const router = useRouter()
const colorMode = useColorMode()

async function switchLocale(code: string) {
  await setLocale(code as 'ar' | 'en')
}

async function handleLogout() {
  await authStore.logout()
  await router.push('/auth/login')
}

// Each toggle shows the option you'd switch TO, not the current state — clicking "دارك"/"Dark"
// while in light mode switches to dark, etc. — rather than listing both options with a checkmark.
const otherLocale = computed(() => locales.value.find(l => l.code !== locale.value))

const localeItems = computed(() => {
  const other = otherLocale.value
  if (!other) return []
  return [{ label: other.name as string, icon: 'i-lucide-languages', onSelect: () => switchLocale(other.code as string) }]
})

const colorModeItems = computed(() => {
  const isDark = colorMode.preference === 'dark'
  return [{
    label: isDark ? t('nav.colorMode.light') : t('nav.colorMode.dark'),
    icon: isDark ? 'i-lucide-sun' : 'i-lucide-moon',
    onSelect: () => { colorMode.preference = isDark ? 'light' : 'dark' },
  }]
})
</script>

<template>
  <div class="flex items-center gap-3">
    <span class="hidden sm:inline text-sm font-medium">
      {{ t('nav.greeting', { name: authStore.user?.fullName }) }}
    </span>

    <UDropdownMenu
      :items="[
        localeItems,
        colorModeItems,
        [{ label: t('nav.logout'), icon: 'i-lucide-log-out', onSelect: handleLogout }],
      ]"
    >
      <UAvatar :alt="authStore.user?.fullName" size="sm" class="cursor-pointer" />
    </UDropdownMenu>
  </div>
</template>
