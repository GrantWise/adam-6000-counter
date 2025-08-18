"use client"

import { createContext, useContext, useState, useEffect, ReactNode } from 'react'

export interface User {
  id: string
  username: string
  role: 'admin' | 'operator' | 'viewer'
  lastLogin?: string
}

interface AuthContextType {
  user: User | null
  isLoading: boolean
  error: string | null
  login: (credentials: { username: string; password: string }) => Promise<void>
  logout: () => Promise<void>
  checkAuth: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

interface AuthProviderProps {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Check if user is already authenticated on app start
  useEffect(() => {
    checkAuth()
  }, [])

  const checkAuth = async () => {
    try {
      setIsLoading(true)
      setError(null)
      
      const response = await fetch('/api/auth/me', {
        credentials: 'include' // Include httpOnly cookies
      })
      
      if (response.ok) {
        const data = await response.json()
        setUser(data.user)
      } else {
        setUser(null)
      }
    } catch (err) {
      console.error('Auth check failed:', err)
      setUser(null)
    } finally {
      setIsLoading(false)
    }
  }

  const login = async (credentials: { username: string; password: string }) => {
    try {
      setIsLoading(true)
      setError(null)

      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include', // Include httpOnly cookies
        body: JSON.stringify(credentials),
      })

      if (response.ok) {
        const data = await response.json()
        setUser(data.user)
        
        // Store token in localStorage as backup (optional)
        if (data.token) {
          localStorage.setItem('auth-token', data.token)
        }
      } else {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Login failed')
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Login failed'
      setError(errorMessage)
      throw err
    } finally {
      setIsLoading(false)
    }
  }

  const logout = async () => {
    try {
      setIsLoading(true)
      
      await fetch('/api/auth/logout', {
        method: 'POST',
        credentials: 'include'
      })
      
      // Clear local storage
      localStorage.removeItem('auth-token')
      
      setUser(null)
      setError(null)
    } catch (err) {
      console.error('Logout failed:', err)
      // Still clear user state even if request fails
      setUser(null)
    } finally {
      setIsLoading(false)
    }
  }

  const value: AuthContextType = {
    user,
    isLoading,
    error,
    login,
    logout,
    checkAuth
  }

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  )
}