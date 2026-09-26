export interface User {
  id: number
  username: string
  email: string
  avatarUrl?: string | null
  displayName?: string | null
  bio?: string | null
  createdAt?: string
  updatedAt?: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
  user: User
}
