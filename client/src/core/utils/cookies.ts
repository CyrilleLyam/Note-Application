import Cookies from 'js-cookie'
import { z } from 'zod'

const tokenSchema = z.string().min(1)

export const authTokensSchema = z.object({
  accessToken: tokenSchema,
  refreshToken: tokenSchema,
})

export type AuthTokens = z.infer<typeof authTokensSchema>

const ACCESS_TOKEN_KEY = 'accessToken'
const REFRESH_TOKEN_KEY = 'refreshToken'

export function setAuthTokens(tokens: {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt?: string | Date
  refreshTokenExpiresAt?: string | Date
}) {
  const validated = authTokensSchema.parse({
    accessToken: tokens.accessToken,
    refreshToken: tokens.refreshToken,
  })

  const accessExpires = tokens.accessTokenExpiresAt ? new Date(tokens.accessTokenExpiresAt) : undefined
  const refreshExpires = tokens.refreshTokenExpiresAt ? new Date(tokens.refreshTokenExpiresAt) : undefined

  Cookies.set(ACCESS_TOKEN_KEY, validated.accessToken, {
    expires: accessExpires,
    sameSite: 'lax',
    secure: window.location.protocol === 'https:',
  })

  Cookies.set(REFRESH_TOKEN_KEY, validated.refreshToken, {
    expires: refreshExpires,
    sameSite: 'lax',
    secure: window.location.protocol === 'https:',
  })
}

export function getAccessToken(): string | undefined {
  return Cookies.get(ACCESS_TOKEN_KEY)
}

export function getRefreshToken(): string | undefined {
  return Cookies.get(REFRESH_TOKEN_KEY)
}

export function clearAuthTokens() {
  Cookies.remove(ACCESS_TOKEN_KEY)
  Cookies.remove(REFRESH_TOKEN_KEY)
}

export function isAuthenticated(): boolean {
  return tokenSchema.safeParse(getAccessToken()).success
    || tokenSchema.safeParse(getRefreshToken()).success
}
