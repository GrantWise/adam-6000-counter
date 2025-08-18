import { hashPassword, verifyPassword } from './jwt'
import { queryDatabaseSingle } from '../database/connection'

export interface User {
  id: string
  username: string
  passwordHash: string
  role: 'admin' | 'operator' | 'viewer'
  isActive: boolean
  createdAt: Date
  lastLogin?: Date
}

export interface LoginCredentials {
  username: string
  password: string
}

export interface AuthResult {
  success: boolean
  user?: User
  error?: string
}

/**
 * Simple user service for OEE application
 * Uses in-memory storage for simplicity (can be upgraded to database later)
 */
export class UserService {
  // In-memory user store (replace with database in production)
  private static users: User[] = []
  private static initialized = false

  /**
   * Initialize with default users
   */
  private static async initialize() {
    if (this.initialized) return

    // Create default users for development
    const defaultUsers = [
      {
        id: '1',
        username: 'admin',
        password: 'admin123', // Change in production!
        role: 'admin' as const,
        isActive: true,
      },
      {
        id: '2',
        username: 'operator',
        password: 'operator123', // Change in production!
        role: 'operator' as const,
        isActive: true,
      },
      {
        id: '3',
        username: 'viewer',
        password: 'viewer123', // Change in production!
        role: 'viewer' as const,
        isActive: true,
      }
    ]

    for (const userData of defaultUsers) {
      const passwordHash = await hashPassword(userData.password)
      this.users.push({
        id: userData.id,
        username: userData.username,
        passwordHash,
        role: userData.role,
        isActive: userData.isActive,
        createdAt: new Date(),
      })
    }

    this.initialized = true
    
    // Log default credentials in development
    if (process.env.NODE_ENV === 'development') {
      console.log('🔐 Default login credentials:')
      defaultUsers.forEach(user => {
        console.log(`  ${user.role}: ${user.username}/${user.password}`)
      })
    }
  }

  /**
   * Authenticate a user with username and password
   */
  static async authenticate(credentials: LoginCredentials): Promise<AuthResult> {
    await this.initialize()

    try {
      // Find user by username
      const user = this.users.find(u => 
        u.username.toLowerCase() === credentials.username.toLowerCase() && u.isActive
      )

      if (!user) {
        return {
          success: false,
          error: 'Invalid username or password'
        }
      }

      // Verify password
      const isValidPassword = await verifyPassword(credentials.password, user.passwordHash)
      
      if (!isValidPassword) {
        return {
          success: false,
          error: 'Invalid username or password'
        }
      }

      // Update last login
      user.lastLogin = new Date()

      return {
        success: true,
        user: {
          ...user,
          passwordHash: '' // Don't return password hash
        }
      }
    } catch (error) {
      console.error('Authentication error:', error)
      return {
        success: false,
        error: 'Authentication failed'
      }
    }
  }

  /**
   * Get user by ID
   */
  static async getUserById(id: string): Promise<User | null> {
    await this.initialize()
    
    const user = this.users.find(u => u.id === id && u.isActive)
    if (!user) return null
    
    return {
      ...user,
      passwordHash: '' // Don't return password hash
    }
  }

  /**
   * Get user by username
   */
  static async getUserByUsername(username: string): Promise<User | null> {
    await this.initialize()
    
    const user = this.users.find(u => 
      u.username.toLowerCase() === username.toLowerCase() && u.isActive
    )
    
    if (!user) return null
    
    return {
      ...user,
      passwordHash: '' // Don't return password hash
    }
  }

  /**
   * Check if user has required role
   */
  static hasRole(user: User, requiredRole: string): boolean {
    if (user.role === 'admin') return true // Admin has all permissions
    
    const roleHierarchy = {
      'admin': 3,
      'operator': 2,
      'viewer': 1
    }
    
    const userLevel = roleHierarchy[user.role] || 0
    const requiredLevel = roleHierarchy[requiredRole as keyof typeof roleHierarchy] || 0
    
    return userLevel >= requiredLevel
  }

  /**
   * Validate user permissions for specific actions
   */
  static canPerformAction(user: User, action: string): boolean {
    const permissions = {
      'admin': ['view', 'operate', 'manage', 'configure'],
      'operator': ['view', 'operate'],
      'viewer': ['view']
    }
    
    const userPermissions = permissions[user.role] || []
    return userPermissions.includes(action)
  }
}

// Initialize default users on import
UserService.authenticate({ username: '', password: '' }).catch(() => {
  // This will trigger initialization
})