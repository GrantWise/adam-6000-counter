import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Loader2, Shield } from 'lucide-react'
import { authService } from '@/lib/services/authService'
import { usePlatformStore } from '@/store/platformStore'
import type { LoginCredentials } from '@/types'

/**
 * Industrial-optimized login form component
 * Features large touch targets and clear validation feedback
 */
export const LoginForm: React.FC = () => {
  const navigate = useNavigate()
  const addNotification = usePlatformStore((state) => state.addNotification)
  
  const [formData, setFormData] = useState<LoginCredentials>({
    username: '',
    password: '',
    rememberMe: false
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }))
    
    // Clear error when user starts typing
    if (error) setError(null)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!formData.username.trim() || !formData.password.trim()) {
      setError('Please enter both username and password')
      return
    }

    setLoading(true)
    setError(null)

    try {
      const result = await authService.login(formData)
      
      if (result.success && result.user) {
        addNotification({
          type: 'success',
          title: 'Login Successful',
          message: `Welcome back, ${result.user.fullName}!`
        })
        
        // Navigate to appropriate dashboard
        navigate(result.redirectTo || '/dashboard')
      } else {
        setError(result.error || 'Login failed. Please try again.')
      }
    } catch (error) {
      console.error('Login error:', error)
      setError('An unexpected error occurred. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary-50 to-primary-100 p-4">
      <Card className="w-full max-w-md shadow-2xl">
        <CardHeader className="space-y-4 text-center">
          <div className="mx-auto w-16 h-16 bg-primary/10 rounded-full flex items-center justify-center">
            <Shield className="w-8 h-8 text-primary" />
          </div>
          <div>
            <CardTitle className="text-2xl font-bold text-foreground">
              Industrial ADAM Platform
            </CardTitle>
            <CardDescription className="text-muted-foreground mt-2">
              Sign in to access your industrial monitoring system
            </CardDescription>
          </div>
        </CardHeader>
        
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
            
            <div className="space-y-2">
              <Label htmlFor="username" className="text-sm font-medium">
                Username
              </Label>
              <Input
                id="username"
                name="username"
                type="text"
                variant="industrial"
                placeholder="Enter your username"
                value={formData.username}
                onChange={handleInputChange}
                disabled={loading}
                autoComplete="username"
                autoFocus
                required
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="password" className="text-sm font-medium">
                Password
              </Label>
              <Input
                id="password"
                name="password"
                type="password"
                variant="industrial"
                placeholder="Enter your password"
                value={formData.password}
                onChange={handleInputChange}
                disabled={loading}
                autoComplete="current-password"
                required
              />
            </div>
            
            <div className="flex items-center space-x-2">
              <input
                id="rememberMe"
                name="rememberMe"
                type="checkbox"
                checked={formData.rememberMe}
                onChange={handleInputChange}
                disabled={loading}
                className="h-4 w-4 text-primary focus:ring-primary border-input rounded"
              />
              <Label
                htmlFor="rememberMe"
                className="text-sm text-muted-foreground cursor-pointer"
              >
                Remember me for 30 days
              </Label>
            </div>
            
            <Button
              type="submit"
              size="touch"
              disabled={loading}
              className="w-full"
            >
              {loading ? (
                <>
                  <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                  Signing in...
                </>
              ) : (
                'Sign In'
              )}
            </Button>
            
            <div className="text-center text-sm text-muted-foreground">
              <p>Having trouble signing in?</p>
              <p>Contact your system administrator for assistance.</p>
            </div>
          </form>
        </CardContent>
      </Card>
      
      {/* System info footer */}
      <div className="absolute bottom-4 right-4 text-xs text-muted-foreground">
        <p>Industrial ADAM Platform v{import.meta.env.VITE_APP_VERSION || '1.0.0'}</p>
        <p>© 2025 Industrial Automation Solutions</p>
      </div>
    </div>
  )
}