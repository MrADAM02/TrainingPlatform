<script setup lang="ts">
defineProps<{
  icon: string
  value: number | string
  label: string
}>()
</script>

<template>
  <UCard :ui="{ body: 'p-4 sm:p-4' }">
    <div class="flex items-center gap-3">
      <div class="flex items-center justify-center size-10 rounded-lg bg-primary/10 text-primary shrink-0">
        <UIcon :name="icon" class="size-5" />
      </div>
      <div class="min-w-0">
        <p class="text-2xl font-semibold leading-tight">
          {{ value }}
        </p>
        <p class="text-sm text-muted truncate">
          {{ label }}
        </p>
      </div>
    </div>
  </UCard>
</template>
