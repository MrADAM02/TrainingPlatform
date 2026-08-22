<script setup lang="ts">
import type { ProblemDetails, QuizAttemptResult, QuizAttemptView } from '~/types/api'

const { t, locale } = useI18n()
const { request } = useApi()
const toast = useToast()
const route = useRoute()
const router = useRouter()

const quizId = route.params.id as string
const backIcon = computed(() => (locale.value === 'ar' ? 'i-lucide-arrow-right' : 'i-lucide-arrow-left'))

const quiz = ref<QuizAttemptView | null>(null)
const answers = reactive<Record<string, string | undefined>>({})
const result = ref<QuizAttemptResult | null>(null)
const submitting = ref(false)

async function loadQuiz() {
  try {
    quiz.value = await request<QuizAttemptView>(`/quizzes/${quizId}/attempt`)
  }
  catch (error) {
    const problem = (error as { data?: ProblemDetails })?.data
    toast.add({ title: problem?.detail ?? t('common.error'), color: 'error' })
    await router.push('/courses')
  }
}

await loadQuiz()

const allQuestionsAnswered = computed(() =>
  quiz.value !== null && quiz.value.questions.every(q => answers[q.id]),
)

function choiceItems(questionId: string) {
  const question = quiz.value?.questions.find(q => q.id === questionId)
  return (question?.choices ?? []).map(c => ({ label: c.text, value: c.id }))
}

async function submit() {
  if (!quiz.value || !allQuestionsAnswered.value) {
    toast.add({ title: t('courses.quizzes.answerAllQuestions'), color: 'error' })
    return
  }

  submitting.value = true
  try {
    result.value = await request<QuizAttemptResult>(`/quizzes/${quizId}/attempts`, {
      method: 'POST',
      body: {
        answers: Object.entries(answers)
          .filter((entry): entry is [string, string] => entry[1] !== undefined)
          .map(([questionId, selectedChoiceId]) => ({ questionId, selectedChoiceId })),
      },
    })
  }
  catch (error) {
    const problem = (error as { data?: ProblemDetails })?.data
    toast.add({ title: problem?.detail ?? t('common.error'), color: 'error' })
  }
  finally {
    submitting.value = false
  }
}

function retake() {
  result.value = null
  for (const key of Object.keys(answers)) {
    answers[key] = undefined
  }
  loadQuiz()
}
</script>

<template>
  <div v-if="quiz">
    <UButton variant="ghost" :icon="backIcon" :label="t('courses.quizzes.backToCourse')" to="/courses" class="mb-4" />

    <h1 class="text-xl font-semibold mb-1">
      {{ quiz.title }}
    </h1>
    <p v-if="quiz.hasPassed" class="text-sm text-muted mb-6">
      {{ t('courses.quizzes.passed') }} — {{ t('courses.quizzes.yourScore', { score: quiz.bestScorePercent }) }}
    </p>
    <p v-else class="text-muted mb-6" />

    <template v-if="!result">
      <UCard v-for="question in quiz.questions" :key="question.id" class="mb-4">
        <p class="font-medium mb-3">
          {{ question.text }}
        </p>
        <URadioGroup v-model="answers[question.id]" :items="choiceItems(question.id)" />
      </UCard>

      <div class="flex justify-end mt-6">
        <UButton
          :label="t('courses.quizzes.submit')" :loading="submitting"
          :disabled="!allQuestionsAnswered" @click="submit"
        />
      </div>
    </template>

    <template v-else>
      <UCard>
        <div class="text-center py-4">
          <UIcon
            :name="result.passed ? 'i-lucide-check-circle' : 'i-lucide-x-circle'"
            :class="result.passed ? 'text-success' : 'text-error'" class="size-12 mx-auto mb-3"
          />
          <p class="text-lg font-semibold mb-1">
            {{ t('courses.quizzes.yourScore', { score: result.scorePercent }) }}
          </p>
          <UBadge :color="result.passed ? 'success' : 'error'" variant="subtle">
            {{ result.passed ? t('courses.quizzes.passed') : t('courses.quizzes.failed') }}
          </UBadge>
        </div>

        <div class="flex justify-center gap-2 mt-4">
          <UButton variant="soft" :label="t('courses.quizzes.retakeQuiz')" @click="retake" />
          <UButton variant="ghost" :label="t('courses.quizzes.backToCourse')" to="/courses" />
        </div>
      </UCard>

      <UAlert
        v-if="result.courseCompleted && result.certificateIssued" color="success" variant="subtle"
        icon="i-lucide-award" class="mt-4" :title="t('courses.quizzes.courseCompleted')"
        :description="t('courses.quizzes.certificateIssued')"
      >
        <template #actions>
          <UButton
            v-if="result.certificateId" size="xs" color="success"
            :label="t('courses.quizzes.viewCertificate')" :to="`/certificates/${result.certificateId}`"
          />
        </template>
      </UAlert>
    </template>
  </div>
</template>
