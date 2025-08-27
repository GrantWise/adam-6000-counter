import React, { useState, useEffect } from 'react'
import { Save, X, Eye, EyeOff, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert } from '@/components/ui/alert'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'
import { userService } from '@/lib/services/userService'
import type { User, UserRole, FormState, HierarchyNode } from '@/types'

interface UserFormProps {
  user?: User | null
  mode: 'create' | 'edit'
  onSave: (user: User) => void
  onCancel: () => void
  hierarchyNodes?: HierarchyNode[]
}

interface UserFormData {
  username: string
  email: string
  fullName: string
  password: string
  confirmPassword: string
  role: UserRole
  status: 'active' | 'inactive'
  hierarchyNodeIds: string[]
}

/**
 * User Form Component
 * Handles creation and editing of user accounts with full validation
 * Designed for industrial admin interfaces with accessibility in mind
 */
export const UserForm: React.FC<UserFormProps> = ({
  user,
  mode,
  onSave,
  onCancel,
  hierarchyNodes = []
}) => {
  const [formData, setFormData] = useState<UserFormData>({
    username: '',
    email: '',
    fullName: '',
    password: '',
    confirmPassword: '',
    role: 'Operator',
    status: 'active',
    hierarchyNodeIds: []
  })

  const [formState, setFormState] = useState<FormState>({
    data: formData,
    errors: {},
    touched: {},
    dirty: false,
    valid: false
  })

  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [availableRoles, setAvailableRoles] = useState<Array<{
    value: UserRole
    label: string
    description: string
  }>>([])

  // Initialize form data
  useEffect(() => {
    if (user && mode === 'edit') {
      const initialData = {
        username: user.username,
        email: user.email,
        fullName: user.fullName,
        password: '',
        confirmPassword: '',
        role: user.role,
        status: user.status,
        hierarchyNodeIds: user.hierarchyAssignments.map(a => a.nodeId)
      }
      setFormData(initialData)
      setFormState(prev => ({ ...prev, data: initialData }))
    }
  }, [user, mode])

  // Load available roles
  useEffect(() => {
    const loadRoles = async () => {
      const response = await userService.getRoles()
      if (response.success && response.data) {
        setAvailableRoles(response.data)
      } else {
        // Fallback to default roles
        setAvailableRoles([
          { value: 'Operator', label: 'Operator', description: 'Basic equipment operation access' },
          { value: 'Supervisor', label: 'Supervisor', description: 'Supervisory access with OEE management' },
          { value: 'Admin', label: 'Administrator', description: 'Full administrative access' },
          { value: 'SystemAdmin', label: 'System Administrator', description: 'Complete system control' }
        ])
      }
    }

    loadRoles()
  }, [])

  // Form validation
  const validateForm = (): boolean => {
    const errors: Record<string, string> = {}

    // Username validation
    if (!formData.username.trim()) {
      errors.username = 'Username is required'
    } else if (formData.username.length < 3) {
      errors.username = 'Username must be at least 3 characters'
    } else if (!/^[a-zA-Z0-9_-]+$/.test(formData.username)) {
      errors.username = 'Username can only contain letters, numbers, underscore, and dash'
    }

    // Email validation
    if (!formData.email.trim()) {
      errors.email = 'Email is required'
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = 'Please enter a valid email address'
    }

    // Full name validation
    if (!formData.fullName.trim()) {
      errors.fullName = 'Full name is required'
    }

    // Password validation (only for create mode or when password is entered)
    if (mode === 'create' || formData.password) {
      if (!formData.password) {
        errors.password = 'Password is required'
      } else if (formData.password.length < 8) {
        errors.password = 'Password must be at least 8 characters'
      } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(formData.password)) {
        errors.password = 'Password must contain at least one uppercase letter, one lowercase letter, and one number'
      }

      if (formData.password !== formData.confirmPassword) {
        errors.confirmPassword = 'Passwords do not match'
      }
    }

    setFormState(prev => ({
      ...prev,
      errors,
      valid: Object.keys(errors).length === 0
    }))

    return Object.keys(errors).length === 0
  }

  // Handle input changes
  const handleInputChange = (field: keyof UserFormData, value: any) => {
    const newFormData = { ...formData, [field]: value }
    setFormData(newFormData)
    setFormState(prev => ({
      ...prev,
      data: newFormData,
      dirty: true,
      touched: { ...prev.touched, [field]: true }
    }))
  }

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!validateForm()) {
      return
    }

    setLoading(true)
    try {
      let response
      
      if (mode === 'create') {
        response = await userService.createUser({
          username: formData.username,
          email: formData.email,
          fullName: formData.fullName,
          password: formData.password,
          role: formData.role,
          hierarchyNodeIds: formData.hierarchyNodeIds,
          status: formData.status
        })
      } else {
        response = await userService.updateUser(user!.id, {
          username: formData.username,
          email: formData.email,
          fullName: formData.fullName,
          role: formData.role,
          status: formData.status,
          hierarchyNodeIds: formData.hierarchyNodeIds
        })

        // Update password separately if provided
        if (formData.password) {
          await userService.resetPassword(user!.id, formData.password)
        }
      }

      if (response.success && response.data) {
        onSave(response.data)
      } else {
        setFormState(prev => ({
          ...prev,
          errors: { submit: response.error?.message || 'Failed to save user' }
        }))
      }
    } catch (error) {
      setFormState(prev => ({
        ...prev,
        errors: { submit: 'An unexpected error occurred' }
      }))
    } finally {
      setLoading(false)
    }
  }

  // Handle hierarchy node selection
  const handleHierarchyChange = (nodeId: string, selected: boolean) => {
    let newHierarchyNodeIds = [...formData.hierarchyNodeIds]
    
    if (selected) {
      if (!newHierarchyNodeIds.includes(nodeId)) {
        newHierarchyNodeIds.push(nodeId)
      }
    } else {
      newHierarchyNodeIds = newHierarchyNodeIds.filter(id => id !== nodeId)
    }
    
    handleInputChange('hierarchyNodeIds', newHierarchyNodeIds)
  }

  return (
    <Card className="w-full max-w-2xl">
      <CardHeader>
        <CardTitle>
          {mode === 'create' ? 'Create New User' : `Edit User: ${user?.fullName}`}
        </CardTitle>
        <CardDescription>
          {mode === 'create'
            ? 'Add a new user to the system with appropriate role and permissions'
            : 'Update user information and settings'
          }
        </CardDescription>
      </CardHeader>

      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Error Alert */}
          {formState.errors.submit && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <div>{formState.errors.submit}</div>
            </Alert>
          )}

          {/* Basic Information */}
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="username">Username*</Label>
              <Input
                id="username"
                type="text"
                value={formData.username}
                onChange={(e) => handleInputChange('username', e.target.value)}
                placeholder="Enter username"
                disabled={mode === 'edit'} // Don't allow username changes
                className={formState.errors.username ? 'border-destructive' : ''}
              />
              {formState.errors.username && (
                <div className="text-sm text-destructive">{formState.errors.username}</div>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email Address*</Label>
              <Input
                id="email"
                type="email"
                value={formData.email}
                onChange={(e) => handleInputChange('email', e.target.value)}
                placeholder="Enter email address"
                className={formState.errors.email ? 'border-destructive' : ''}
              />
              {formState.errors.email && (
                <div className="text-sm text-destructive">{formState.errors.email}</div>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="fullName">Full Name*</Label>
              <Input
                id="fullName"
                type="text"
                value={formData.fullName}
                onChange={(e) => handleInputChange('fullName', e.target.value)}
                placeholder="Enter full name"
                className={formState.errors.fullName ? 'border-destructive' : ''}
              />
              {formState.errors.fullName && (
                <div className="text-sm text-destructive">{formState.errors.fullName}</div>
              )}
            </div>
          </div>

          {/* Password Section */}
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="password">
                Password{mode === 'create' ? '*' : ''} 
                {mode === 'edit' && (
                  <span className="text-sm text-muted-foreground ml-2">
                    (leave blank to keep current password)
                  </span>
                )}
              </Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  value={formData.password}
                  onChange={(e) => handleInputChange('password', e.target.value)}
                  placeholder="Enter password"
                  className={formState.errors.password ? 'border-destructive pr-10' : 'pr-10'}
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="absolute right-0 top-0 h-full px-3"
                  onClick={() => setShowPassword(!showPassword)}
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </Button>
              </div>
              {formState.errors.password && (
                <div className="text-sm text-destructive">{formState.errors.password}</div>
              )}
            </div>

            {(mode === 'create' || formData.password) && (
              <div className="space-y-2">
                <Label htmlFor="confirmPassword">Confirm Password*</Label>
                <Input
                  id="confirmPassword"
                  type={showPassword ? 'text' : 'password'}
                  value={formData.confirmPassword}
                  onChange={(e) => handleInputChange('confirmPassword', e.target.value)}
                  placeholder="Confirm password"
                  className={formState.errors.confirmPassword ? 'border-destructive' : ''}
                />
                {formState.errors.confirmPassword && (
                  <div className="text-sm text-destructive">{formState.errors.confirmPassword}</div>
                )}
              </div>
            )}
          </div>

          {/* Role and Status */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="role">Role*</Label>
              <select
                id="role"
                value={formData.role}
                onChange={(e) => handleInputChange('role', e.target.value as UserRole)}
                className="w-full px-3 py-2 border border-input rounded-md bg-background"
              >
                {availableRoles.map((role) => (
                  <option key={role.value} value={role.value}>
                    {role.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="status">Status</Label>
              <select
                id="status"
                value={formData.status}
                onChange={(e) => handleInputChange('status', e.target.value as 'active' | 'inactive')}
                className="w-full px-3 py-2 border border-input rounded-md bg-background"
              >
                <option value="active">Active</option>
                <option value="inactive">Inactive</option>
              </select>
            </div>
          </div>

          {/* Hierarchy Assignments */}
          {hierarchyNodes.length > 0 && (
            <div className="space-y-4">
              <Label>Hierarchy Assignments</Label>
              <div className="max-h-48 overflow-y-auto border border-input rounded-md p-3 space-y-2">
                {hierarchyNodes.map((node) => (
                  <div key={node.id} className="flex items-center space-x-2">
                    <input
                      type="checkbox"
                      id={`hierarchy-${node.id}`}
                      checked={formData.hierarchyNodeIds.includes(node.id)}
                      onChange={(e) => handleHierarchyChange(node.id, e.target.checked)}
                      className="h-4 w-4 rounded border-border"
                    />
                    <Label htmlFor={`hierarchy-${node.id}`} className="text-sm">
                      <div className="flex items-center gap-2">
                        <span>{node.name}</span>
                        <Badge variant="outline" className="text-xs">
                          {node.type}
                        </Badge>
                      </div>
                    </Label>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Form Actions */}
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={onCancel}
              disabled={loading}
            >
              <X className="h-4 w-4 mr-2" />
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={loading || !formState.valid}
              className="min-w-[120px]"
            >
              {loading ? (
                <LoadingSpinner size="sm" className="mr-2" />
              ) : (
                <Save className="h-4 w-4 mr-2" />
              )}
              {mode === 'create' ? 'Create User' : 'Save Changes'}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}

export default UserForm