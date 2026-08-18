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
  <div class="min-h-screen flex flex-col">
    <header class="flex items-center justify-between gap-4 px-6 py-3 border-b border-default">
      <span class="font-semibold">{{ t('app.name') }}</span>

      <nav v-if="authStore.isAuthenticated" class="flex items-center gap-1">
        <UButton to="/dashboard" variant="ghost" :label="t('nav.dashboard')" />
        <UButton to="/courses" variant="ghost" :label="t('nav.courses')" />
        <UButton to="/search" variant="ghost" :label="t('nav.search')" />
        <UButton
          v-if="authStore.hasRole('Trainer') || authStore.hasRole('Administrator')"
          to="/trainer" variant="ghost" :label="t('nav.trainer')"
        />
        <UButton v-if="authStore.hasRole('Administrator')" to="/admin" variant="ghost" :label="t('nav.admin')" />
        <UButton variant="ghost" :label="t('nav.logout')" @click="handleLogout" />
      </nav>

      <div class="flex gap-1">
        <UButton
          v-for="l in otherLocales"
          :key="l.code"
          variant="outline"
          size="sm"
          :label="l.name"
          @click="switchLocale(l.code as string)"
        />
      </div>
    </header>

    <main class="flex-1 p-6">
      <slot />
    </main>
  </div>
</template>
