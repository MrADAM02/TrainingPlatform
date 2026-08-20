<script setup lang="ts">
import type { CertificateSummary } from '~/types/api'

const { t } = useI18n()
const { request } = useApi()
const toast = useToast()

const certificates = ref<CertificateSummary[]>([])

try {
  certificates.value = await request<CertificateSummary[]>('/certificates')
}
catch {
  toast.add({ title: t('common.error'), color: 'error' })
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString()
}
</script>

<template>
  <div>
    <h1 class="text-xl font-semibold mb-6">
      {{ t('certificates.title') }}
    </h1>

    <p v-if="certificates.length === 0" class="text-muted">
      {{ t('certificates.empty') }}
    </p>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <UCard v-for="certificate in certificates" :key="certificate.id">
        <div class="flex items-start gap-3">
          <div class="flex items-center justify-center size-10 rounded-lg bg-primary/10 text-primary shrink-0">
            <UIcon name="i-lucide-award" class="size-5" />
          </div>
          <div class="min-w-0">
            <h3 class="font-medium truncate">
              {{ certificate.courseTitle }}
            </h3>
            <p class="text-sm text-muted">
              {{ t('certificates.issuedOn', { date: formatDate(certificate.issuedAtUtc) }) }}
            </p>
          </div>
        </div>

        <UButton
          class="mt-4" variant="soft" block :label="t('certificates.view')"
          :to="`/certificates/${certificate.id}`"
        />
      </UCard>
    </div>
  </div>
</template>
