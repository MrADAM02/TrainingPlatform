<script setup lang="ts">
import type { CertificateDetails, ProblemDetails } from '~/types/api'

const { t, locale } = useI18n()
const { request } = useApi()
const toast = useToast()
const route = useRoute()
const router = useRouter()

const certificateId = route.params.id as string
const certificate = ref<CertificateDetails | null>(null)

try {
  certificate.value = await request<CertificateDetails>(`/certificates/${certificateId}`)
}
catch (error) {
  const problem = (error as { data?: ProblemDetails })?.data
  toast.add({ title: problem?.detail ?? t('certificates.notFound'), color: 'error' })
  await router.push('/certificates')
}

const backIcon = computed(() => (locale.value === 'ar' ? 'i-lucide-arrow-right' : 'i-lucide-arrow-left'))

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
}

function printCertificate() {
  window.print()
}
</script>

<template>
  <div v-if="certificate">
    <div class="print:hidden flex items-center justify-between mb-6">
      <UButton variant="ghost" :icon="backIcon" :label="t('certificates.back')" to="/certificates" />
      <UButton icon="i-lucide-printer" :label="t('certificates.print')" @click="printCertificate" />
    </div>

    <div class="max-w-3xl mx-auto rounded-lg border-4 border-double border-primary p-10 sm:p-16 text-center bg-default">
      <UIcon name="i-lucide-award" class="size-16 text-primary mx-auto mb-6" />

      <p class="text-sm tracking-widest uppercase text-muted mb-2">
        {{ t('app.name') }}
      </p>

      <p class="text-muted mb-6">
        {{ t('certificates.certifyText') }}
      </p>

      <p class="text-3xl font-semibold mb-6">
        {{ certificate.recipientFullName }}
      </p>

      <p class="text-muted mb-2">
        {{ t('certificates.completedText') }}
      </p>

      <p class="text-2xl font-medium mb-8">
        {{ certificate.courseTitle }}
      </p>

      <p class="text-sm text-muted">
        {{ formatDate(certificate.issuedAtUtc) }}
      </p>
      <p class="text-xs text-muted mt-1">
        {{ t('certificates.certificateNumber', { number: certificate.certificateNumber }) }}
      </p>
    </div>
  </div>
</template>
