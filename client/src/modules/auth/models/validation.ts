import { z } from 'zod'

export const loginSchema = z.object({
  email: z.string().trim().min(1, 'validation.emailRequired').email('validation.emailInvalid'),
  password: z.string().min(6, 'validation.passwordMin'),
})

export type LoginInput = z.infer<typeof loginSchema>

export const registerSchema = z.object({
  username: z.string().trim().min(3, 'validation.usernameMin').max(50, 'validation.usernameMax'),
  email: z.string().trim().min(1, 'validation.emailRequired').email('validation.emailInvalid'),
  password: z.string().min(6, 'validation.passwordMin').max(100, 'validation.passwordMax'),
  confirmPassword: z.string().min(1, 'validation.confirmPasswordRequired'),
}).refine(data => data.password === data.confirmPassword, {
  message: 'validation.passwordsMismatch',
  path: ['confirmPassword'],
})

export type RegisterInput = z.infer<typeof registerSchema>

export const profileSchema = z.object({
  username: z.string().trim().min(3, 'validation.usernameMin').max(50, 'validation.usernameMax'),
  displayName: z.string().trim().max(100, 'validation.displayNameMax'),
  bio: z.string().trim().max(500, 'validation.bioMax'),
})

export type ProfileInput = z.infer<typeof profileSchema>
