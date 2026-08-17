<script setup lang="ts">
const { t } = useI18n()
const authStore = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const isSubmitting = ref(false)
const errorMessage = ref<string | null>(null)

async function handleSubmit() {
  errorMessage.value = null
  isSubmitting.value = true

  try {
    await authStore.login(email.value, password.value)
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
  <div class="login-page">
    <form class="login-form" @submit.prevent="handleSubmit">
      <h1>{{ t('auth.login.title') }}</h1>

      <label>
        {{ t('auth.login.email') }}
        <input v-model="email" type="email" name="email" autocomplete="username" required>
      </label>

      <label>
        {{ t('auth.login.password') }}
        <input v-model="password" type="password" name="password" autocomplete="current-password" required>
      </label>

      <p v-if="errorMessage" class="error" role="alert">
        {{ errorMessage }}
      </p>

      <button type="submit" :disabled="isSubmitting">
        {{ isSubmitting ? t('auth.login.submitting') : t('auth.login.submit') }}
      </button>
    </form>
  </div>
</template>

<style scoped>
.login-page {
  display: flex;
  justify-content: center;
  padding-top: 4rem;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  width: 100%;
  max-width: 20rem;
}

label {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.error {
  color: #b91c1c;
}
</style>
