import { createContext, useContext, useState } from 'react'
import type { ReactNode } from 'react'
import { authApi } from '../api/endpoints'
import { STORAGE_KEY } from '../api/client'
import type { AuthUser, RegisterBody } from '../api/types'

interface AuthContextValue {
  user: AuthUser | null
  login: (email: string, password: string) => Promise<AuthUser>
  register: (body: RegisterBody) => Promise<AuthUser>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

// بيقرأ المستخدم المحفوظ، ويتجاهله لو الـ Token خلص
function loadUser(): AuthUser | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const u = JSON.parse(raw) as AuthUser
    if (new Date(u.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(STORAGE_KEY)
      return null
    }
    return u
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(loadUser)

  const save = (u: AuthUser) => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(u))
    setUser(u)
    return u
  }

  const value: AuthContextValue = {
    user,
    login: async (email, password) => save(await authApi.login({ email, password })),
    register: async (body) => save(await authApi.register(body)),
    logout: () => {
      localStorage.removeItem(STORAGE_KEY)
      setUser(null)
    },
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}