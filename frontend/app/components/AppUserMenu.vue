<script setup lang="ts">
const { t, locale, locales, setLocale } = useI18n()
const authStore = useAuthStore()
const router = useRouter()

const otherLocales = computed(() => locales.value.filter(l => l.code !== locale.value))

async function switchLocale(code: string) {
  await setLocale(code as 'ar' | 'en')
}

async function handleLogout() {
  await authStore.logout()
  await router.push('/auth/login')
}
</script>

<template>
  <div class="flex items-center gap-3">
    <span class="hidden sm:inline text-sm font-medium">
      {{ t('nav.greeting', { name: authStore.user?.fullName }) }}
    </span>

    <UDropdownMenu
      :items="[
        otherLocales.map(l => ({ label: l.name as string, onSelect: () => switchLocale(l.code as string) })),
        [{ label: t('nav.logout'), icon: 'i-lucide-log-out', onSelect: handleLogout }],
      ]"
    >
      <UAvatar :alt="authStore.user?.fullName" size="sm" class="cursor-pointer" />
    </UDropdownMenu>
  </div>
</template>
