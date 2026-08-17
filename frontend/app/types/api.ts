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

export interface ModuleDetails {
  id: string
  courseId: string
  title: string
  order: number
  documents: DocumentSummary[]
}

export interface CourseDetails {
  id: string
  title: string
  description: string
  trainerId: string
  isPublished: boolean
  createdAtUtc: string
  modules: ModuleDetails[]
}

export interface UploadTicket {
  documentId: string
  uploadUrl: string
  expiresAtUtc: string
}
