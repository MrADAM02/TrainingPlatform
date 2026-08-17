<script setup lang="ts">
const { t } = useI18n()
const authStore = useAuthStore()
</script>

<template>
  <div>
    <h1>{{ t('dashboard.welcome', { name: authStore.user?.fullName }) }}</h1>
    <p>{{ t('dashboard.role', { role: authStore.user?.roles.join(', ') }) }}</p>
  </div>
</template>
