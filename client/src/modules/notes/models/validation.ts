import { z } from 'zod'

export const NOTE_TITLE_MAX_LENGTH = 200
export const NOTE_MAX_TAGS = 10
export const NOTE_TAG_MAX_LENGTH = 30

export const noteSchema = z.object({
  title: z.string().trim().min(1, 'validation.titleRequired').max(NOTE_TITLE_MAX_LENGTH, 'validation.titleMax'),
  content: z.string().optional(),
  tags: z.array(z.string().max(NOTE_TAG_MAX_LENGTH, 'validation.tagMax')).max(NOTE_MAX_TAGS, 'validation.tagsMax'),
})

export type NoteInput = z.infer<typeof noteSchema>

export function normalizeTag(value: string): string {
  return value.trim().split(/\s+/).filter(Boolean).join(' ').toLowerCase().slice(0, NOTE_TAG_MAX_LENGTH)
}
