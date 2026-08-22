<script setup lang="ts">
import type { FormError } from '@nuxt/ui'
import type { ProblemDetails, QuestionInput, QuizManagementDetails } from '~/types/api'

const { t, locale } = useI18n()
const { request } = useApi()
const toast = useToast()
const route = useRoute()
const router = useRouter()

const backIcon = computed(() => (locale.value === 'ar' ? 'i-lucide-arrow-right' : 'i-lucide-arrow-left'))

const routeId = route.params.id as string
const isNew = routeId === 'new'
const moduleId = route.query.moduleId as string | undefined

const quizId = ref<string | null>(isNew ? null : routeId)
const loading = ref(true)

const state = reactive({
  title: '',
  passingScorePercent: 70,
  isRequiredForCompletion: false,
})

const questions = reactive<QuestionInput[]>([])

async function loadQuiz() {
  if (!quizId.value) {
    loading.value = false
    return
  }

  try {
    const quiz = await request<QuizManagementDetails>(`/quizzes/${quizId.value}/manage`)
    state.title = quiz.title
    state.passingScorePercent = quiz.passingScorePercent
    state.isRequiredForCompletion = quiz.isRequiredForCompletion
    questions.splice(0, questions.length, ...quiz.questions.map(q => ({
      text: q.text,
      order: q.order,
      choices: q.choices.map(c => ({ text: c.text, isCorrect: c.isCorrect, order: c.order })),
    })))
  }
  catch (error) {
    const problem = (error as { data?: ProblemDetails })?.data
    toast.add({ title: problem?.detail ?? t('common.error'), color: 'error' })
    await router.push('/trainer')
  }
  finally {
    loading.value = false
  }
}

await loadQuiz()

function addQuestion() {
  questions.push({ text: '', order: questions.length + 1, choices: [] })
}

function removeQuestion(index: number) {
  questions.splice(index, 1)
}

function addChoice(questionIndex: number) {
  const question = questions[questionIndex]
  if (!question) return
  question.choices.push({ text: '', isCorrect: question.choices.length === 0, order: question.choices.length + 1 })
}

function removeChoice(questionIndex: number, choiceIndex: number) {
  questions[questionIndex]?.choices.splice(choiceIndex, 1)
}

function setCorrectChoice(questionIndex: number, choiceIndex: number) {
  const question = questions[questionIndex]
  if (!question) return
  question.choices.forEach((c, i) => { c.isCorrect = i === choiceIndex })
}

function validateDetails(formState: typeof state): FormError[] {
  const errors: FormError[] = []
  if (!formState.title) errors.push({ name: 'title', message: t('courses.quizzes.quizTitle') })
  if (formState.passingScorePercent < 0 || formState.passingScorePercent > 100) {
    errors.push({ name: 'passingScorePercent', message: t('courses.quizzes.passingScore') })
  }
  return errors
}

function questionsAreValid(): boolean {
  return questions.every(q =>
    q.text.trim().length > 0
    && q.choices.length >= 2
    && q.choices.filter(c => c.isCorrect).length === 1
    && q.choices.every(c => c.text.trim().length > 0),
  )
}

const saving = ref(false)

async function save() {
  if (validateDetails(state).length > 0) return
  if (!questionsAreValid()) {
    toast.add({ title: t('courses.quizzes.noQuestions'), color: 'error' })
    return
  }

  saving.value = true
  try {
    if (!quizId.value) {
      const created = await request<{ id: string }>(`/modules/${moduleId}/quizzes`, {
        method: 'POST',
        body: {
          title: state.title,
          passingScorePercent: state.passingScorePercent,
          isRequiredForCompletion: state.isRequiredForCompletion,
        },
      })
      quizId.value = created.id
      router.replace(`/trainer/quizzes/${created.id}`)
    }

    await request(`/quizzes/${quizId.value}`, {
      method: 'PUT',
      body: {
        title: state.title,
        passingScorePercent: state.passingScorePercent,
        isRequiredForCompletion: state.isRequiredForCompletion,
        questions,
      },
    })

    toast.add({ title: t('courses.quizzes.updateSuccess'), color: 'success' })
  }
  catch (error) {
    const problem = (error as { data?: ProblemDetails })?.data
    toast.add({ title: problem?.detail ?? t('common.error'), color: 'error' })
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <div v-if="!loading">
    <UButton variant="ghost" :icon="backIcon" :label="t('courses.quizzes.backToCourse')" to="/trainer" class="mb-4" />

    <h1 class="text-xl font-semibold mb-6">
      {{ isNew ? t('courses.quizzes.create') : t('courses.quizzes.edit') }}
    </h1>

    <UCard class="mb-6">
      <div class="space-y-4">
        <UFormField :label="t('courses.quizzes.quizTitle')" name="title">
          <UInput v-model="state.title" class="w-full" />
        </UFormField>
        <UFormField :label="t('courses.quizzes.passingScore')" name="passingScorePercent">
          <UInput v-model.number="state.passingScorePercent" type="number" :min="0" :max="100" class="w-full" />
        </UFormField>
        <UFormField>
          <UCheckbox v-model="state.isRequiredForCompletion" :label="t('courses.quizzes.requiredForCompletion')" />
        </UFormField>
      </div>
    </UCard>

    <div class="flex items-center justify-between mb-4">
      <h2 class="text-lg font-semibold">
        {{ t('courses.quizzes.questions') }}
      </h2>
      <UButton size="sm" icon="i-lucide-plus" :label="t('courses.quizzes.addQuestion')" @click="addQuestion" />
    </div>

    <p v-if="questions.length === 0" class="text-muted mb-6">
      {{ t('courses.quizzes.noQuestions') }}
    </p>

    <UCard v-for="(question, qIndex) in questions" :key="qIndex" class="mb-4">
      <div class="flex items-start justify-between gap-3 mb-3">
        <UFormField :label="t('courses.quizzes.questionText')" class="flex-1">
          <UInput v-model="question.text" class="w-full" />
        </UFormField>
        <UButton
          class="mt-6" size="xs" variant="ghost" color="error" icon="i-lucide-trash-2"
          :aria-label="t('courses.quizzes.removeQuestion')" @click="removeQuestion(qIndex)"
        />
      </div>

      <p class="text-sm font-medium mb-2">
        {{ t('courses.quizzes.choices') }}
      </p>

      <div v-for="(choice, cIndex) in question.choices" :key="cIndex" class="flex items-center gap-2 mb-2">
        <UButton
          :icon="choice.isCorrect ? 'i-lucide-check-circle' : 'i-lucide-circle'"
          :color="choice.isCorrect ? 'success' : 'neutral'"
          variant="ghost" size="sm" square
          :aria-label="t('courses.quizzes.markCorrect')"
          @click="setCorrectChoice(qIndex, cIndex)"
        />
        <UInput v-model="choice.text" class="flex-1" :placeholder="t('courses.quizzes.choiceText')" />
        <UButton
          icon="i-lucide-x" variant="ghost" color="error" size="sm" square
          :aria-label="t('courses.quizzes.removeChoice')" @click="removeChoice(qIndex, cIndex)"
        />
      </div>

      <UButton
        size="xs" variant="outline" icon="i-lucide-plus" class="mt-1"
        :label="t('courses.quizzes.addChoice')" @click="addChoice(qIndex)"
      />
    </UCard>

    <div class="flex justify-end mt-6">
      <UButton :label="t('courses.save')" :loading="saving" @click="save" />
    </div>
  </div>
</template>
