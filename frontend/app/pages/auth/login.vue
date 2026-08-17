<script setup lang="ts">
import type { FormError, FormSubmitEvent } from '@nuxt/ui'

const { t } = useI18n()
const authStore = useAuthStore()
const router = useRouter()

const state = reactive({ email: '', password: '' })
const isSubmitting = ref(false)
const errorMessage = ref<string | null>(null)

function validate(formState: typeof state): FormError[] {
  const errors: FormError[] = []
  if (!formState.email) errors.push({ name: 'email', message: t('admin.users.email') })
  if (!formState.password) errors.push({ name: 'password', message: t('auth.login.password') })
  return errors
}

async function handleSubmit(event: FormSubmitEvent<typeof state>) {
  errorMessage.value = null
  isSubmitting.value = true

  try {
    await authStore.login(event.data.email, event.data.password)
    await router.push('/dashboard')
  }
  catch {
    errorMessage.value = t('auth.login.error')
  }
  finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="flex justify-center pt-16">
    <UCard class="w-full max-w-sm">
      <template #header>
        <h1 class="text-lg font-semibold">
          {{ t('auth.login.title') }}
        </h1>
      </template>

      <UForm :state="state" :validate="validate" @submit="handleSubmit">
        <div class="space-y-4">
          <UFormField :label="t('auth.login.email')" name="email">
            <UInput v-model="state.email" type="email" autocomplete="username" class="w-full" />
          </UFormField>

          <UFormField :label="t('auth.login.password')" name="password">
            <UInput v-model="state.password" type="password" autocomplete="current-password" class="w-full" />
          </UFormField>

          <UAlert v-if="errorMessage" color="error" variant="subtle" :title="errorMessage" />

          <UButton
            type="submit" block :loading="isSubmitting"
            :label="isSubmitting ? t('auth.login.submitting') : t('auth.login.submit')"
          />
        </div>
      </UForm>
    </UCard>
  </div>
</template>
