import axios, { AxiosInstance, AxiosResponse, AxiosError, InternalAxiosRequestConfig } from 'axios'
import type { ApiResponse, ApiError } from '@/types'

/**
 * Enhanced API client with automatic authentication, error handling, and retry logic
 * Designed for industrial environments with network reliability considerations
 * Supports multiple service endpoints (Logger API, OEE API, etc.)
 */
class ApiClient {
  private instances: Record<string, AxiosInstance>
  private tokenStorage: TokenStorage

  constructor() {
    this.tokenStorage = new TokenStorage()
    this.instances = {}
    
    // Initialize default instance
    this.instances.default = this.createInstance('http://localhost:5139')
    
    // Initialize service-specific instances
    this.instances.logger = this.createInstance('http://localhost:5139')
    this.instances.oee = this.createInstance('http://localhost:5001')
    this.instances.scheduling = this.createInstance('http://localhost:5141')
  }

  private createInstance(baseURL: string): AxiosInstance {
    const instance = axios.create({
      baseURL,
      timeout: 10000, // 10 second timeout for industrial environments
      headers: {
        'Content-Type': 'application/json',
      },
    })

    this.setupInterceptors(instance)
    return instance
  }

  /**
   * Get the appropriate axios instance for a service
   */
  private getInstance(service: 'logger' | 'oee' | 'scheduling' | 'default' = 'default'): AxiosInstance {
    return this.instances[service] || this.instances.default
  }

  private setupInterceptors(instance: AxiosInstance) {
    // Request interceptor - Add authentication headers
    instance.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        const token = this.tokenStorage.getAccessToken()
        if (token && config.headers) {
          config.headers.Authorization = `Bearer ${token}`
        }
        
        // Add request timestamp for debugging
        config.metadata = { startTime: Date.now() }
        
        return config
      },
      (error) => {
        console.error('Request interceptor error:', error)
        return Promise.reject(error)
      }
    )

    // Response interceptor - Handle authentication and errors
    instance.interceptors.response.use(
      (response: AxiosResponse) => {
        // Log response time for monitoring
        const duration = Date.now() - (response.config.metadata?.startTime || 0)
        if (duration > 5000) {
          console.warn(`Slow API response (${duration}ms): ${response.config.method?.toUpperCase()} ${response.config.url}`)
        }
        
        return response
      },
      async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

        // Handle token refresh for 401 errors
        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true

          try {
            const refreshToken = this.tokenStorage.getRefreshToken()
            if (refreshToken) {
              const response = await this.refreshToken(refreshToken)
              this.tokenStorage.setTokens(response.data.accessToken, response.data.refreshToken)
              
              // Retry the original request with the correct instance
              if (originalRequest.headers) {
                originalRequest.headers.Authorization = `Bearer ${response.data.accessToken}`
              }
              return instance.request(originalRequest)
            }
          } catch (refreshError) {
            // Refresh failed, redirect to login
            this.tokenStorage.clearTokens()
            window.location.href = '/login'
            return Promise.reject(refreshError)
          }
        }

        return Promise.reject(this.transformError(error))
      }
    )
  }

  private transformError(error: AxiosError): ApiError {
    // Handle network errors
    if (error.code === 'ERR_NETWORK') {
      return {
        code: 'NETWORK_ERROR',
        message: 'Network connection failed. Please check your internet connection and try again.',
        details: { networkError: true, originalError: error.message },
        timestamp: new Date(),
        retryable: true
      }
    }
    
    // Handle timeout errors
    if (error.code === 'ECONNABORTED' || error.message.includes('timeout')) {
      return {
        code: 'TIMEOUT_ERROR',
        message: 'Request timed out. Please try again.',
        details: { timeout: true, originalError: error.message },
        timestamp: new Date(),
        retryable: true
      }
    }
    
    const apiError: ApiError = {
      code: error.response?.data?.code || this.getErrorCodeFromStatus(error.response?.status),
      message: this.getUserFriendlyMessage(error),
      details: error.response?.data || { originalError: error.message },
      timestamp: new Date(),
      retryable: this.isRetryableError(error.response?.status)
    }

    // Log error for debugging (in development)
    if (import.meta.env.DEV) {
      console.error('API Error:', {
        url: error.config?.url,
        method: error.config?.method,
        status: error.response?.status,
        error: apiError
      })
    }

    return apiError
  }

  private getUserFriendlyMessage(error: AxiosError): string {
    // Handle network and timeout errors
    if (error.code === 'ECONNABORTED') {
      return 'Request timeout. Please check your network connection and try again.'
    }
    
    if (error.code === 'ERR_NETWORK') {
      return 'Network connection failed. Please check your internet connection and try again.'
    }

    // Handle server responses
    const status = error.response?.status
    const serverMessage = error.response?.data?.message
    
    switch (status) {
      case 400:
        return serverMessage || 'Invalid request. Please check your input and try again.'
      case 401:
        return 'Your session has expired. Please log in again.'
      case 403:
        return 'You do not have permission to perform this action. Contact your administrator if needed.'
      case 404:
        return 'The requested resource was not found. It may have been moved or deleted.'
      case 409:
        return serverMessage || 'Conflict occurred. The resource may have been modified by another user.'
      case 422:
        return serverMessage || 'Validation error. Please check your input and try again.'
      case 429:
        return 'Too many requests. Please wait a moment before trying again.'
      case 500:
        return 'A server error occurred. Our team has been notified. Please try again later.'
      case 502:
      case 503:
        return 'The service is temporarily unavailable. Please try again in a few minutes.'
      case 504:
        return 'The server is taking too long to respond. Please try again later.'
      default:
        return serverMessage || 'An unexpected error occurred. Please try again or contact support if the problem persists.'
    }
  }

  // Authentication methods (always use logger API for auth)
  async login(username: string, password: string): Promise<AxiosResponse> {
    return this.getInstance('logger').post('/auth/login', { username, password })
  }

  async refreshToken(refreshToken: string): Promise<AxiosResponse> {
    return this.getInstance('logger').post('/auth/refresh', { refreshToken })
  }

  async logout(): Promise<AxiosResponse> {
    const result = this.getInstance('logger').post('/auth/logout')
    this.tokenStorage.clearTokens()
    return result
  }

  // Helper methods for error handling
  private getErrorCodeFromStatus(status?: number): string {
    switch (status) {
      case 400: return 'BAD_REQUEST'
      case 401: return 'UNAUTHORIZED'
      case 403: return 'FORBIDDEN'
      case 404: return 'NOT_FOUND'
      case 409: return 'CONFLICT'
      case 422: return 'VALIDATION_ERROR'
      case 429: return 'RATE_LIMITED'
      case 500: return 'INTERNAL_SERVER_ERROR'
      case 502: return 'BAD_GATEWAY'
      case 503: return 'SERVICE_UNAVAILABLE'
      case 504: return 'GATEWAY_TIMEOUT'
      default: return 'UNKNOWN_ERROR'
    }
  }
  
  private isRetryableError(status?: number): boolean {
    return status ? [408, 429, 500, 502, 503, 504].includes(status) : true
  }
  
  // Generic HTTP methods with proper typing and service selection
  async get<T = any>(url: string, config?: any, service: 'logger' | 'oee' | 'scheduling' | 'default' = 'default'): Promise<ApiResponse<T>> {
    try {
      const response = await this.getInstance(service).get<T>(url, config)
      return {
        success: true,
        data: response.data
      }
    } catch (error) {
      return {
        success: false,
        error: error as ApiError
      }
    }
  }

  async post<T = any>(url: string, data?: any, config?: any, service: 'logger' | 'oee' | 'scheduling' | 'default' = 'default'): Promise<ApiResponse<T>> {
    try {
      const response = await this.getInstance(service).post<T>(url, data, config)
      return {
        success: true,
        data: response.data
      }
    } catch (error) {
      return {
        success: false,
        error: error as ApiError
      }
    }
  }

  async put<T = any>(url: string, data?: any, config?: any, service: 'logger' | 'oee' | 'scheduling' | 'default' = 'default'): Promise<ApiResponse<T>> {
    try {
      const response = await this.getInstance(service).put<T>(url, data, config)
      return {
        success: true,
        data: response.data
      }
    } catch (error) {
      return {
        success: false,
        error: error as ApiError
      }
    }
  }

  async patch<T = any>(url: string, data?: any, config?: any, service: 'logger' | 'oee' | 'scheduling' | 'default' = 'default'): Promise<ApiResponse<T>> {
    try {
      const response = await this.getInstance(service).patch<T>(url, data, config)
      return {
        success: true,
        data: response.data
      }
    } catch (error) {
      return {
        success: false,
        error: error as ApiError
      }
    }
  }

  async delete<T = any>(url: string, config?: any, service: 'logger' | 'oee' | 'scheduling' | 'default' = 'default'): Promise<ApiResponse<T>> {
    try {
      const response = await this.getInstance(service).delete<T>(url, config)
      return {
        success: true,
        data: response.data
      }
    } catch (error) {
      return {
        success: false,
        error: error as ApiError
      }
    }
  }

  // Raw axios instance access for advanced use cases
  getPublicInstance(service: 'logger' | 'oee' | 'scheduling' | 'default' = 'default'): AxiosInstance {
    return this.getInstance(service)
  }
}

/**
 * Token storage utility for secure token management
 */
class TokenStorage {
  private readonly ACCESS_TOKEN_KEY = 'adam_access_token'
  private readonly REFRESH_TOKEN_KEY = 'adam_refresh_token'

  setTokens(accessToken: string, refreshToken: string): void {
    // Use sessionStorage for access token (more secure)
    sessionStorage.setItem(this.ACCESS_TOKEN_KEY, accessToken)
    // Use localStorage for refresh token (persists across sessions)
    localStorage.setItem(this.REFRESH_TOKEN_KEY, refreshToken)
  }

  getAccessToken(): string | null {
    return sessionStorage.getItem(this.ACCESS_TOKEN_KEY)
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY)
  }

  clearTokens(): void {
    sessionStorage.removeItem(this.ACCESS_TOKEN_KEY)
    localStorage.removeItem(this.REFRESH_TOKEN_KEY)
  }

  hasValidTokens(): boolean {
    const accessToken = this.getAccessToken()
    const refreshToken = this.getRefreshToken()
    return !!(accessToken && refreshToken)
  }
}

// Export singleton instance
export const apiClient = new ApiClient()

// Export the class for testing
export { ApiClient, TokenStorage }

// Extend axios config type to include metadata
declare module 'axios' {
  interface InternalAxiosRequestConfig {
    metadata?: {
      startTime: number
    }
  }
}