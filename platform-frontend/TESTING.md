# Industrial ADAM Platform - Testing Guide

This document provides comprehensive testing guidance for the Industrial ADAM Platform frontend, with special emphasis on CFR Part 11 compliance requirements.

## Table of Contents

1. [Testing Philosophy](#testing-philosophy)
2. [Test Architecture](#test-architecture)
3. [Running Tests](#running-tests)
4. [CFR Part 11 Compliance Testing](#cfr-part-11-compliance-testing)
5. [Writing Tests](#writing-tests)
6. [CI/CD Pipeline](#cicd-pipeline)
7. [Troubleshooting](#troubleshooting)

## Testing Philosophy

Following CLAUDE.md principles, our testing strategy prioritizes:

- **Pragmatic Excellence**: Focus on critical business logic and compliance requirements
- **CFR Part 11 Compliance**: Zero tolerance for synthetic data generation
- **Data Integrity**: Comprehensive validation of data quality indicators
- **Real-world Scenarios**: Testing against actual backend API contracts

### Coverage Goals

- **Critical Services**: 90%+ coverage on authentication, validation, and compliance
- **Business Logic**: 80%+ coverage on core functionality
- **UI Components**: 70%+ coverage on shared components
- **Integration**: 100% coverage on critical user workflows

## Test Architecture

```
src/
├── __tests__/
│   └── integration/          # Integration tests
│       ├── auth-flow.test.tsx
│       └── cfr-compliance.test.tsx
├── components/
│   └── ui/
│       └── __tests__/        # Component tests
│           └── data-quality-indicator.test.tsx
├── lib/
│   ├── services/
│   │   └── __tests__/        # Service unit tests
│   │       └── authService.test.ts
│   └── utils/
│       └── __tests__/        # Utility tests
│           └── apiValidation.test.ts
└── test/                     # Test configuration
    ├── setup.ts
    ├── msw-setup.ts
    ├── handlers.ts
    └── test-utils.tsx
```

### Test Types

1. **Unit Tests**: Individual functions and classes
2. **Component Tests**: React component behavior
3. **Integration Tests**: Cross-system workflows
4. **Compliance Tests**: CFR Part 11 specific scenarios

## Running Tests

### Prerequisites

```bash
npm install
```

### Basic Test Commands

```bash
# Run all tests
npm test

# Run specific test suites
npm run test:unit           # Unit tests only
npm run test:integration    # Integration tests only
npm run test:compliance     # CFR Part 11 compliance tests

# Run with coverage
npm run test:coverage

# Run in watch mode
npm run test:watch

# Run UI test runner
npm run test:ui

# Complete validation (lint + test + build)
npm run validate
```

### Test Filtering

```bash
# Run specific test files
npm test authService

# Run tests matching pattern
npm test -- --grep "authentication"

# Run tests in specific directory
npm test src/lib/services

# Run only changed files (in watch mode)
npm run test:watch
```

## CFR Part 11 Compliance Testing

### Critical Compliance Requirements

1. **No Synthetic Data Generation**
   - Tests verify system never generates fake measurements
   - All unavailable data must be explicitly marked
   - Simulated data requires clear warnings

2. **Data Quality Indicators**
   - Every data point must have quality indication
   - Users must be warned about uncertain data
   - Audit trails must be maintained

3. **Electronic Records Integrity**
   - Data precision must be preserved
   - Timestamps must be accurate
   - User attribution must be maintained

### Compliance Test Examples

```typescript
// ❌ BAD - Generates synthetic data
it('should provide fallback values when device offline', () => {
  const result = getDeviceReading('offline-device')
  expect(result.value).toBe(42) // This is synthetic!
})

// ✅ GOOD - Properly indicates unavailable data
it('should indicate unavailable data when device offline', () => {
  const result = getDeviceReading('offline-device')
  expect(result.quality).toBe('unavailable')
  expect(result.warning).toContain('Device offline')
  expect(result.value).toBeNull()
})
```

### Running Compliance Tests

```bash
# Run all compliance tests
npm run test:compliance

# Run with detailed output
npm run test:compliance -- --reporter=verbose

# Generate compliance report
npm run test:compliance -- --coverage --reporter=html
```

## Writing Tests

### Test Structure

Follow the **Arrange, Act, Assert** pattern:

```typescript
describe('AuthService', () => {
  it('should handle login failure without generating fake tokens', async () => {
    // Arrange
    const invalidCredentials = { username: 'invalid', password: 'wrong' }
    
    // Act
    const result = await authService.login(invalidCredentials)
    
    // Assert
    expect(result.success).toBe(false)
    expect(result.user).toBeUndefined()
    expect(result.error).toBeDefined()
  })
})
```

### Using Test Utilities

```typescript
import { 
  render, 
  screen, 
  createMockUser, 
  setupAuthenticatedUser,
  assertNoSyntheticData 
} from '@/test/test-utils'

it('should display user information correctly', () => {
  // Setup authenticated state
  const mockUser = createMockUser({ role: 'Admin' })
  setupAuthenticatedUser(mockUser)
  
  // Render component
  render(<UserProfile />)
  
  // Assert
  expect(screen.getByText(mockUser.fullName)).toBeInTheDocument()
  assertNoSyntheticData(mockUser)
})
```

### Component Testing

```typescript
import { render, screen, fireEvent } from '@/test/test-utils'
import { DataQualityIndicator } from '../data-quality-indicator'

describe('DataQualityIndicator', () => {
  it('should show prominent warning for simulated data', () => {
    render(
      <DataQualityIndicator 
        quality="simulated" 
        showAlert 
      />
    )
    
    expect(screen.getByText('SIMULATED')).toBeInTheDocument()
    expect(screen.getByText(/SYNTHETIC DATA/)).toBeInTheDocument()
  })
})
```

### Service Testing

```typescript
import { authService } from '../authService'
import { server } from '@/test/msw-setup'

describe('AuthService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })
  
  it('should refresh tokens automatically', async () => {
    // Test implementation
  })
})
```

### Integration Testing

```typescript
import { render, screen, fireEvent, waitFor } from '@/test/test-utils'
import { LoginForm } from '@/components/auth/LoginForm'

describe('Authentication Flow', () => {
  it('should complete full login workflow', async () => {
    const user = userEvent.setup()
    
    render(<LoginForm />)
    
    await user.type(screen.getByLabelText(/username/i), 'test.admin')
    await user.type(screen.getByLabelText(/password/i), 'test123')
    await user.click(screen.getByRole('button', { name: /sign in/i }))
    
    await waitFor(() => {
      expect(screen.getByText(/dashboard/i)).toBeInTheDocument()
    })
  })
})
```

## CI/CD Pipeline

### GitHub Actions Workflow

The CI/CD pipeline runs automatically on:
- Push to `main`, `develop`, or feature branches
- Pull requests
- Daily scheduled runs

### Pipeline Stages

1. **Security Audit**: `npm audit` for vulnerabilities
2. **Code Quality**: ESLint, TypeScript, Prettier
3. **Unit Tests**: Core business logic tests
4. **Integration Tests**: End-to-end workflows
5. **CFR Compliance**: Regulatory compliance validation
6. **Build Validation**: Production build testing
7. **Performance Analysis**: Bundle size and performance

### Pipeline Commands

```bash
# Run full CI pipeline locally
npm run validate

# Individual pipeline stages
npm run lint                    # Code quality
npm run test:unit              # Unit tests
npm run test:integration       # Integration tests
npm run test:compliance        # CFR compliance
npm run build                  # Build validation
```

## Troubleshooting

### Common Issues

#### Tests Fail with "MSW Handler Not Found"

```bash
# Check if MSW handlers are properly configured
grep -r "http.get\|http.post" src/test/handlers.ts
```

#### Authentication Tests Failing

```bash
# Verify store state is properly reset
# Check that usePlatformStore.getState().clearAuth() is called in beforeEach
```

#### Component Tests Can't Find Elements

```bash
# Use screen.debug() to see rendered output
render(<Component />)
screen.debug()
```

#### Coverage Thresholds Not Met

```bash
# Run coverage report to identify gaps
npm run test:coverage
# Open coverage/index.html in browser
```

### Performance Issues

```bash
# Run tests with performance timing
npm test -- --reporter=verbose

# Check for memory leaks in watch mode
npm run test:watch
# Monitor memory usage over time
```

### Debugging Tests

```typescript
// Add debug output
it('should work correctly', () => {
  const result = someFunction()
  console.log('Debug result:', result) // Will show in test output
  expect(result).toBe(expected)
})

// Use vi.fn() to inspect calls
const mockFunction = vi.fn()
someFunction(mockFunction)
console.log('Mock calls:', mockFunction.mock.calls)
```

## Best Practices

### DO

- ✅ Test critical business logic thoroughly
- ✅ Use descriptive test names that explain the scenario
- ✅ Follow AAA pattern (Arrange, Act, Assert)
- ✅ Mock external dependencies
- ✅ Test error conditions and edge cases
- ✅ Verify CFR Part 11 compliance in data handling
- ✅ Clean up after tests (clear stores, timers, mocks)

### DON'T

- ❌ Generate synthetic data in tests without clear marking
- ❌ Test implementation details instead of behavior
- ❌ Write tests that depend on specific timing
- ❌ Mock everything - test real integrations where possible
- ❌ Ignore test failures or lower coverage thresholds
- ❌ Create tests that pass/fail randomly

### CFR Part 11 Specific Guidelines

- **NEVER** generate fake measurement data
- **ALWAYS** mark simulated/test data clearly
- **VERIFY** data quality indicators are present
- **ENSURE** audit trails are maintained
- **TEST** unauthorized access scenarios
- **VALIDATE** electronic signature workflows

## Additional Resources

- [Vitest Documentation](https://vitest.dev/)
- [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/)
- [MSW (Mock Service Worker)](https://mswjs.io/)
- [CFR Part 11 Guidelines](https://www.fda.gov/regulatory-information/search-fda-guidance-documents/part-11-electronic-records-electronic-signatures-scope-and-application)

For questions or issues with testing, please refer to the project's CLAUDE.md guidelines and ensure all contributions maintain our pragmatic excellence standards.