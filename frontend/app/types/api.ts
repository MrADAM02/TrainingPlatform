export interface PaginatedList<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export type UserRole = 'Administrator' | 'Trainer' | 'Trainee'

export interface UserSummary {
  id: string
  email: string
  fullName: string
  roles: UserRole[]
  isActive: boolean
  createdAtUtc: string
  lastLoginAtUtc: string | null
}

export interface BulkImportUserSuccess {
  email: string
  fullName: string
  role: UserRole
  temporaryPassword: string
}

export interface BulkImportUserFailure {
  rowNumber: number
  email: string
  reason: string
}

export interface BulkImportUsersResult {
  created: BulkImportUserSuccess[]
  failed: BulkImportUserFailure[]
}

export interface ActivityLogItem {
  id: string
  userId: string
  userEmail: string | null
  action: string
  entityType: string | null
  entityId: string | null
  ipAddress: string | null
  timestampUtc: string
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
}

export interface CourseSummary {
  id: string
  title: string
  description: string
  trainerId: string
  isPublished: boolean
  createdAtUtc: string
  isEnrolled: boolean
}

// Matches TrainingPlatform.Domain.Content.DocumentType's declaration order (serialized as int).
export const documentTypeLabels = ['PDF', 'Video', 'Presentation', 'Other'] as const

export interface DocumentSummary {
  id: string
  moduleId: string
  title: string
  fileType: number
  contentType: string
  sizeBytes: number
  version: number
  uploadedAtUtc: string
}

export interface QuizSummary {
  id: string
  moduleId: string
  title: string
  passingScorePercent: number
  isRequiredForCompletion: boolean
}

export interface ModuleDetails {
  id: string
  courseId: string
  title: string
  order: number
  documents: DocumentSummary[]
  quizzes: QuizSummary[]
}

export interface CourseDetails {
  id: string
  title: string
  description: string
  trainerId: string
  isPublished: boolean
  createdAtUtc: string
  isEnrolled: boolean
  canDownload: boolean
  modules: ModuleDetails[]
}

export interface UploadTicket {
  documentId: string
  uploadUrl: string
  expiresAtUtc: string
}

// Sent as the query-string value for the search content-type filter — ASP.NET Core's minimal
// API model binder resolves nullable enum query params by name, not by number.
export const documentTypeNames = ['Pdf', 'Video', 'Presentation', 'Other'] as const
export type DocumentTypeName = typeof documentTypeNames[number]

export type EnrollmentStatus = 0 | 1 // Active | Completed — matches the C# enum's declaration order

export interface EnrollmentSummary {
  id: string
  userId: string
  userEmail: string
  userFullName: string
  courseId: string
  status: EnrollmentStatus
  enrolledAtUtc: string
}

export interface DashboardCourseItem {
  courseId: string
  courseTitle: string
  status: EnrollmentStatus
  totalDocuments: number
  completedDocuments: number
}

export interface RecentDocumentItem {
  documentId: string
  documentTitle: string
  courseId: string
  courseTitle: string
  uploadedAtUtc: string
}

export interface DashboardResponse {
  courses: DashboardCourseItem[]
  recentlyAdded: RecentDocumentItem[]
}

export interface DocumentSearchResult {
  documentId: string
  documentTitle: string
  fileType: number
  courseId: string
  courseTitle: string
  moduleId: string
  moduleTitle: string
  uploadedAtUtc: string
  canDownload: boolean
}

export interface CertificateSummary {
  id: string
  courseId: string
  courseTitle: string
  certificateNumber: string
  issuedAtUtc: string
}

export interface CertificateDetails {
  id: string
  courseId: string
  courseTitle: string
  recipientFullName: string
  certificateNumber: string
  issuedAtUtc: string
}

// --- Quizzes ---
// Single-choice only for now (REQ-QUIZ-01) — no question-type field on the frontend yet,
// mirroring the backend's decision not to expose QuestionType until multi-choice actually ships.

export interface ChoiceInput {
  text: string
  isCorrect: boolean
  order: number
}

export interface QuestionInput {
  text: string
  order: number
  choices: ChoiceInput[]
}

export interface ChoiceManagementDetails {
  id: string
  text: string
  isCorrect: boolean
  order: number
}

export interface QuestionManagementDetails {
  id: string
  text: string
  order: number
  choices: ChoiceManagementDetails[]
}

export interface QuizManagementDetails {
  id: string
  moduleId: string
  title: string
  passingScorePercent: number
  isRequiredForCompletion: boolean
  questions: QuestionManagementDetails[]
}

export interface ChoiceAttemptView {
  id: string
  text: string
  order: number
}

export interface QuestionAttemptView {
  id: string
  text: string
  order: number
  choices: ChoiceAttemptView[]
}

export interface QuizAttemptView {
  id: string
  moduleId: string
  title: string
  passingScorePercent: number
  questions: QuestionAttemptView[]
  hasPassed: boolean
  bestScorePercent: number | null
}

export interface AnswerInput {
  questionId: string
  selectedChoiceId: string
}

export interface QuizAttemptResult {
  scorePercent: number
  passed: boolean
  courseCompleted: boolean
  certificateIssued: boolean
  certificateId: string | null
}

export interface OrgSummaryReport {
  totalCourses: number
  publishedCourses: number
  totalEnrollments: number
  completedEnrollments: number
  totalCertificatesIssued: number
  activeTraineesLast30Days: number
}

export interface CourseCompletionReportItem {
  courseId: string
  courseTitle: string
  isPublished: boolean
  enrolledCount: number
  completedCount: number
  completionPercent: number
  avgCompletionDays: number | null
}

export interface TraineeProgressReportItem {
  userId: string
  userEmail: string
  userFullName: string
  status: number
  enrolledAtUtc: string
  completedDocuments: number
  totalDocuments: number
  requiredQuizzesPassed: number
  requiredQuizzesTotal: number
  certificateIssued: boolean
}
