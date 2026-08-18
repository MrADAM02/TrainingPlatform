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
            <div class="flex items-center gap-2">
              <div class="flex items-center justify-center size-8 rounded-lg bg-primary/10 text-primary shrink-0">
                <UIcon name="i-lucide-users" class="size-4" />
              </div>
              <span class="font-medium">{{ t('nav.adminUsers') }}</span>
            </div>
          </template>
          <p class="text-sm text-muted">
            {{ t('admin.hub.usersDescription') }}
          </p>
        </UCard>
      </NuxtLink>

      <NuxtLink to="/admin/activity">
        <UCard class="hover:ring-primary transition-shadow">
          <template #header>
            <div class="flex items-center gap-2">
              <div class="flex items-center justify-center size-8 rounded-lg bg-primary/10 text-primary shrink-0">
                <UIcon name="i-lucide-history" class="size-4" />
              </div>
              <span class="font-medium">{{ t('nav.adminActivity') }}</span>
            </div>
          </template>
          <p class="text-sm text-muted">
            {{ t('admin.hub.activityDescription') }}
          </p>
        </UCard>
      </NuxtLink>
    </div>
  </div>
</template>
