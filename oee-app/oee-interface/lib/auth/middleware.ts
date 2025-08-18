import { NextRequest, NextResponse } from 'next/server'
import { verifyToken, extractTokenFromHeader, JwtPayload } from './jwt'
import { UserService } from './userService'

export interface AuthenticatedRequest extends NextRequest {
  user?: JwtPayload & { role: string }
}

/**
 * Middleware to authenticate JWT tokens
 */
export async function authenticateToken(request: NextRequest): Promise<{
  isAuthenticated: boolean
  user?: JwtPayload
  response?: NextResponse
}> {
  // Get token from Authorization header or cookie
  let token = extractTokenFromHeader(request.headers.get('Authorization'))
  
  if (!token) {
    // Try to get token from httpOnly cookie
    const tokenCookie = request.cookies.get('auth-token')
    token = tokenCookie?.value || null
  }

  if (!token) {
    return {
      isAuthenticated: false,
      response: NextResponse.json(
        { error: 'Authentication required' },
        { status: 401 }
      )
    }
  }

  const payload = verifyToken(token)
  
  if (!payload) {
    return {
      isAuthenticated: false,
      response: NextResponse.json(
        { error: 'Invalid or expired token' },
        { status: 401 }
      )
    }
  }

  return {
    isAuthenticated: true,
    user: payload
  }
}

/**
 * Middleware to check if user has required role
 */
export async function requireRole(
  request: NextRequest,
  requiredRole: string
): Promise<{
  hasPermission: boolean
  user?: JwtPayload
  response?: NextResponse
}> {
  const authResult = await authenticateToken(request)
  
  if (!authResult.isAuthenticated || !authResult.user) {
    return {
      hasPermission: false,
      response: authResult.response
    }
  }

  const user = await UserService.getUserById(authResult.user.userId)
  
  if (!user || !UserService.hasRole(user, requiredRole)) {
    return {
      hasPermission: false,
      response: NextResponse.json(
        { error: 'Insufficient permissions' },
        { status: 403 }
      )
    }
  }

  return {
    hasPermission: true,
    user: { ...authResult.user, role: user.role }
  }
}

/**
 * Middleware to check if user can perform specific action
 */
export async function requirePermission(
  request: NextRequest,
  action: string
): Promise<{
  hasPermission: boolean
  user?: JwtPayload
  response?: NextResponse
}> {
  const authResult = await authenticateToken(request)
  
  if (!authResult.isAuthenticated || !authResult.user) {
    return {
      hasPermission: false,
      response: authResult.response
    }
  }

  const user = await UserService.getUserById(authResult.user.userId)
  
  if (!user || !UserService.canPerformAction(user, action)) {
    return {
      hasPermission: false,
      response: NextResponse.json(
        { error: `Permission denied: ${action}` },
        { status: 403 }
      )
    }
  }

  return {
    hasPermission: true,
    user: { ...authResult.user, role: user.role }
  }
}

/**
 * Helper to create authenticated API handler
 */
export function withAuth(
  handler: (request: NextRequest, user: JwtPayload) => Promise<NextResponse>,
  requiredRole?: string
) {
  return async (request: NextRequest) => {
    const authResult = requiredRole 
      ? await requireRole(request, requiredRole)
      : await authenticateToken(request)
    
    if (!authResult.hasPermission && !authResult.isAuthenticated) {
      return authResult.response!
    }
    
    return handler(request, authResult.user!)
  }
}

/**
 * Helper to create permission-based API handler
 */
export function withPermission(
  handler: (request: NextRequest, user: JwtPayload) => Promise<NextResponse>,
  requiredAction: string
) {
  return async (request: NextRequest) => {
    const authResult = await requirePermission(request, requiredAction)
    
    if (!authResult.hasPermission) {
      return authResult.response!
    }
    
    return handler(request, authResult.user!)
  }
}