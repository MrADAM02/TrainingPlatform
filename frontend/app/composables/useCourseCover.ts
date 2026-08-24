// Fixed, deterministic cover treatments for course cards. There is no thumbnailUrl field on
// Course today (and none is planned for this pass) — every course gets a gradient+icon cover
// instead of a real photo, picked by hashing the course's stable id so the same course always
// renders the same cover. This is the seam to swap in real thumbnails later: only this file and
// CourseCoverPlaceholder.vue need to change.
//
// Gradient classes are written out in full (not built from interpolated strings) so Tailwind's
// static scanner can actually find and generate them.
const GRADIENTS = [
  'from-brand-600 to-brand-800',
  'from-brand-500 to-brand-700',
  'from-accent-500 to-accent-700',
  'from-accent-400 to-brand-600',
  'from-brand-700 to-accent-600',
  'from-brand-400 to-brand-700',
  'from-accent-600 to-brand-800',
  'from-brand-500 to-accent-500',
]

const ICONS = [
  'i-lucide-book-open',
  'i-lucide-graduation-cap',
  'i-lucide-code',
  'i-lucide-laptop',
  'i-lucide-target',
  'i-lucide-lightbulb',
  'i-lucide-compass',
  'i-lucide-puzzle',
]

function hashString(value: string): number {
  let hash = 0
  for (let i = 0; i < value.length; i++) {
    hash = (hash * 31 + value.charCodeAt(i)) >>> 0
  }
  return hash
}

function initialsOf(title: string): string {
  return title
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map(word => word.charAt(0).toUpperCase())
    .join('')
}

export interface CourseCover {
  gradient: string
  icon: string
  initials: string
}

export function useCourseCover(id: string, title: string): CourseCover {
  const hash = hashString(id)
  return {
    gradient: GRADIENTS[hash % GRADIENTS.length]!,
    icon: ICONS[Math.floor(hash / GRADIENTS.length) % ICONS.length]!,
    initials: initialsOf(title),
  }
}
