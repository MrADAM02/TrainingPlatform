<script setup lang="ts">
const { t } = useI18n()
</script>

<template>
  <div class="max-w-2xl">
    <h1 class="text-xl font-semibold mb-6">
      {{ t('admin.hub.title') }}
    </h1>

    <div class="grid gap-4 sm:grid-cols-2">
      <NuxtLink to="/admin/users">
        <UCard class="hover:ring-primary transition-shadow">
          <template #header>
            <span class="font-medium">{{ t('nav.adminUsers') }}</span>
          </template>
          <p class="text-sm text-muted">
            {{ t('admin.hub.usersDescription') }}
          </p>
        </UCard>
      </NuxtLink>

      <NuxtLink to="/admin/activity">
        <UCard class="hover:ring-primary transition-shadow">
          <template #header>
            <span class="font-medium">{{ t('nav.adminActivity') }}</span>
          </template>
          <p class="text-sm text-muted">
            {{ t('admin.hub.activityDescription') }}
          </p>
        </UCard>
      </NuxtLink>
    </div>
  </div>
</template>
