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
  <div class="app-shell">
    <header class="app-header">
      <span class="app-title">{{ t('app.name') }}</span>

      <nav v-if="authStore.isAuthenticated" class="app-nav">
        <NuxtLink to="/dashboard">
          {{ t('nav.dashboard') }}
        </NuxtLink>
        <NuxtLink v-if="authStore.hasRole('Trainer') || authStore.hasRole('Administrator')" to="/trainer">
          {{ t('nav.trainer') }}
        </NuxtLink>
        <NuxtLink v-if="authStore.hasRole('Administrator')" to="/admin">
          {{ t('nav.admin') }}
        </NuxtLink>
        <button type="button" @click="handleLogout">
          {{ t('nav.logout') }}
        </button>
      </nav>

      <div class="locale-switch">
        <button
          v-for="l in otherLocales"
          :key="l.code"
          type="button"
          @click="switchLocale(l.code as string)"
        >
          {{ l.name }}
        </button>
      </div>
    </header>

    <main class="app-main">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.75rem 1.5rem;
  border-bottom: 1px solid #e2e2e2;
}

.app-title {
  font-weight: 600;
}

.app-nav {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.locale-switch {
  display: flex;
  gap: 0.5rem;
}

.app-main {
  flex: 1;
  padding: 1.5rem;
}
</style>
