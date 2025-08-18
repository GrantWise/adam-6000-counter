import { NextRequest, NextResponse } from 'next/server'
import { UserService } from '@/lib/auth/userService'
import { generateToken } from '@/lib/auth/jwt'
import { validateBody, loginSchema } from '@/lib/validation/schemas'

export async function POST(request: NextRequest) {
  try {
    const body = await request.json()
    
    // Validate request body
    const { username, password } = validateBody(loginSchema)(body)
    
    // Authenticate user
    const authResult = await UserService.authenticate({ username, password })
    
    if (!authResult.success || !authResult.user) {
      return NextResponse.json(
        { error: authResult.error || 'Authentication failed' },
        { status: 401 }
      )
    }

    // Generate JWT token
    const token = generateToken({
      userId: authResult.user.id,
      username: authResult.user.username,
      role: authResult.user.role,
    })

    // Create response with user data
    const response = NextResponse.json(
      {
        success: true,
        user: {
          id: authResult.user.id,
          username: authResult.user.username,
          role: authResult.user.role,
          lastLogin: authResult.user.lastLogin,
        },
        token, // Include token in response for client storage
      },
      { status: 200 }
    )

    // Set httpOnly cookie for browser security
    response.cookies.set('auth-token', token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'lax',
      maxAge: 24 * 60 * 60, // 24 hours in seconds
      path: '/',
    })

    return response
  } catch (error) {
    console.error('Login error:', error)
    
    // Handle validation errors specifically
    if (error instanceof Error && error.message.includes('Validation error')) {
      return NextResponse.json({ error: error.message }, { status: 400 })
    }
    
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}

// Handle preflight requests
export async function OPTIONS() {
  return new NextResponse(null, {
    status: 200,
    headers: {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization',
    },
  })
}