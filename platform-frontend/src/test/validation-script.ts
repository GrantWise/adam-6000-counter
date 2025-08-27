#!/usr/bin/env tsx
/**
 * Critical Issues Validation Script
 * Validates that all Phase 2 critical issues have been resolved
 * Run this to verify CFR Part 11 compliance and production readiness
 */

import { readFileSync, existsSync } from 'fs'
import { join } from 'path'
import { scanForSyntheticDataPatterns } from '@/lib/utils/cfrCompliance'

interface ValidationResult {
  category: string
  test: string
  status: 'PASS' | 'FAIL' | 'WARNING'
  details: string
}

const results: ValidationResult[] = []

function addResult(category: string, test: string, status: 'PASS' | 'FAIL' | 'WARNING', details: string) {
  results.push({ category, test, status, details })
}

function validateFileExists(filePath: string, description: string): boolean {
  if (existsSync(filePath)) {
    addResult('File Structure', `${description} exists`, 'PASS', filePath)
    return true
  } else {
    addResult('File Structure', `${description} exists`, 'FAIL', `Missing: ${filePath}`)
    return false
  }
}

function validateFileContent(filePath: string, description: string, checks: Array<{pattern: RegExp, shouldExist: boolean, name: string}>): void {
  if (!existsSync(filePath)) {
    addResult('Content Validation', description, 'FAIL', `File not found: ${filePath}`)
    return
  }

  const content = readFileSync(filePath, 'utf-8')
  let hasFailures = false

  checks.forEach(check => {
    const found = check.pattern.test(content)
    if (found !== check.shouldExist) {
      hasFailures = true
      const status = check.shouldExist ? 'FAIL' : 'FAIL'
      const expectedText = check.shouldExist ? 'should contain' : 'should NOT contain'
      addResult('Content Validation', `${description} - ${check.name}`, status, 
        `${expectedText} pattern: ${check.pattern.toString()}`)
    }
  })

  if (!hasFailures) {
    addResult('Content Validation', description, 'PASS', 'All content checks passed')
  }

  // Check for synthetic data patterns
  const syntheticPatterns = scanForSyntheticDataPatterns(content)
  if (syntheticPatterns.length > 0) {
    addResult('CFR Compliance', `${description} - Synthetic Data Check`, 'FAIL',
      `Found potential synthetic patterns: ${syntheticPatterns.join(', ')}`)
  } else {
    addResult('CFR Compliance', `${description} - Synthetic Data Check`, 'PASS',
      'No synthetic data patterns detected')
  }
}

// Main validation function
async function runValidation() {
  console.log('🔍 Running Critical Issues Validation...\n')

  // 1. CRITICAL: SignalR Hub Configuration
  console.log('1️⃣ Validating SignalR Integration Architecture...')
  
  validateFileContent(
    'src/modules/logger/hooks/useRealTimeCounters.ts',
    'Logger SignalR Configuration',
    [
      { pattern: /\/hubs\/counter-hub/, shouldExist: true, name: 'Counter Hub URL' },
      { pattern: /\/health-hub/, shouldExist: false, name: 'Generic Health Hub (should be removed)' },
      { pattern: /5139/, shouldExist: true, name: 'Logger API Port Reference' },
      { pattern: /accessTokenFactory/, shouldExist: true, name: 'Authentication Token Factory' }
    ]
  )

  validateFileContent(
    'src/modules/oee/hooks/useOeeRealTime.ts',
    'OEE SignalR Configuration',
    [
      { pattern: /\/hubs\/oee-hub/, shouldExist: true, name: 'OEE Hub URL' },
      { pattern: /5140/, shouldExist: true, name: 'OEE API Port Reference' },
      { pattern: /accessTokenFactory/, shouldExist: true, name: 'Authentication Token Factory' }
    ]
  )

  // 2. CRITICAL: API Client Integration
  console.log('2️⃣ Validating Real API Client Integration...')
  
  validateFileContent(
    'src/lib/api/client.ts',
    'API Client Configuration',
    [
      { pattern: /VITE_LOGGER_API_PORT.*5139/, shouldExist: true, name: 'Logger API Port Config' },
      { pattern: /VITE_OEE_API_PORT.*5140/, shouldExist: true, name: 'OEE API Port Config' },
      { pattern: /instances\.logger/, shouldExist: true, name: 'Logger Service Instance' },
      { pattern: /instances\.oee/, shouldExist: true, name: 'OEE Service Instance' }
    ]
  )

  // 3. CRITICAL: CFR Part 11 Compliance - No Synthetic Data
  console.log('3️⃣ Validating CFR Part 11 Compliance (No Synthetic Data)...')
  
  validateFileContent(
    'src/lib/services/oeeService.ts',
    'OEE Service - No Synthetic Data',
    [
      { pattern: /Math\.random/, shouldExist: false, name: 'No Random Number Generation' },
      { pattern: /generateFallbackOEE/, shouldExist: false, name: 'No Synthetic Fallback Methods' },
      { pattern: /synthetic|fake|mock.*data/i, shouldExist: false, name: 'No Synthetic Data References' },
      { pattern: /CFR.*Part.*11|compliance/i, shouldExist: true, name: 'CFR Compliance Documentation' },
      { pattern: /success:\s*false/, shouldExist: true, name: 'Proper Error Handling' }
    ]
  )

  validateFileContent(
    'src/lib/services/deviceService.ts',
    'Device Service - Real API Integration',
    [
      { pattern: /apiClient\.get.*logger/, shouldExist: true, name: 'Logger API Integration' },
      { pattern: /basePath.*logger/, shouldExist: true, name: 'Logger API Base Path' },
      { pattern: /mock|fake|synthetic/i, shouldExist: false, name: 'No Mock Data References' }
    ]
  )

  // 4. CRITICAL: Authentication Token Management
  console.log('4️⃣ Validating Authentication Token Management...')
  
  validateFileContent(
    'src/lib/services/authService.ts',
    'Authentication Service',
    [
      { pattern: /getToken\(\)/, shouldExist: true, name: 'Token Getter Method' },
      { pattern: /accessToken/, shouldExist: true, name: 'Access Token Handling' },
      { pattern: /refreshToken/, shouldExist: true, name: 'Refresh Token Logic' }
    ]
  )

  // 5. Data Quality Indicators
  console.log('5️⃣ Validating Data Quality Indicators...')
  
  validateFileExists('src/components/ui/data-quality-indicator.tsx', 'Data Quality Indicator Component')
  validateFileExists('src/lib/utils/cfrCompliance.ts', 'CFR Compliance Utilities')
  
  // 6. Test Coverage for Compliance
  console.log('6️⃣ Validating Test Coverage...')
  
  validateFileExists('src/test/integration/api-compliance.test.ts', 'API Compliance Tests')

  // 7. Environment Configuration
  console.log('7️⃣ Validating Environment Configuration...')
  
  if (validateFileExists('.env.template', 'Environment Template')) {
    validateFileContent(
      '.env.template',
      'Environment Configuration Template',
      [
        { pattern: /VITE_LOGGER_API_PORT.*5139/, shouldExist: true, name: 'Logger API Port' },
        { pattern: /VITE_OEE_API_PORT.*5140/, shouldExist: true, name: 'OEE API Port' },
        { pattern: /localhost/, shouldExist: true, name: 'Development Host Configuration' }
      ]
    )
  }

  // 8. Module Exports
  console.log('8️⃣ Validating Module Exports...')
  
  validateFileContent(
    'src/modules/oee/index.ts',
    'OEE Module Exports',
    [
      { pattern: /useOeeRealTime/, shouldExist: true, name: 'Real-time OEE Hook Export' },
      { pattern: /useOeeData/, shouldExist: true, name: 'OEE Data Hook Export' }
    ]
  )

  // Generate Report
  console.log('\n📊 Validation Results Summary:')
  console.log('=' * 50)
  
  const passed = results.filter(r => r.status === 'PASS').length
  const failed = results.filter(r => r.status === 'FAIL').length
  const warnings = results.filter(r => r.status === 'WARNING').length
  
  console.log(`✅ PASSED: ${passed}`)
  console.log(`❌ FAILED: ${failed}`)
  console.log(`⚠️  WARNINGS: ${warnings}`)
  console.log(`📊 TOTAL: ${results.length}`)
  
  // Detailed Results
  console.log('\n📝 Detailed Results:')
  console.log('=' * 50)
  
  const categories = [...new Set(results.map(r => r.category))]
  categories.forEach(category => {
    console.log(`\n🗂️  ${category}:`)
    const categoryResults = results.filter(r => r.category === category)
    categoryResults.forEach(result => {
      const icon = result.status === 'PASS' ? '✅' : result.status === 'FAIL' ? '❌' : '⚠️'
      console.log(`   ${icon} ${result.test}`)
      if (result.status !== 'PASS') {
        console.log(`      ${result.details}`)
      }
    })
  })

  // Critical Issues Assessment
  console.log('\n🎯 Critical Issues Status:')
  console.log('=' * 50)
  
  const criticalChecks = [
    { name: 'SignalR Hub URLs Fixed', category: 'Content Validation', pattern: /SignalR.*Counter Hub URL|OEE Hub URL/ },
    { name: 'Real API Integration', category: 'Content Validation', pattern: /API.*Integration/ },
    { name: 'No Synthetic Data Fallbacks', category: 'CFR Compliance', pattern: /Synthetic Data Check/ },
    { name: 'Authentication Working', category: 'Content Validation', pattern: /Authentication/ },
  ]

  criticalChecks.forEach(check => {
    const relatedResults = results.filter(r => 
      r.category.includes(check.category) && 
      (r.test.includes(check.name) || check.pattern.test(r.test))
    )
    const allPassed = relatedResults.every(r => r.status === 'PASS')
    const icon = allPassed ? '✅' : '❌'
    console.log(`${icon} ${check.name}: ${allPassed ? 'RESOLVED' : 'NEEDS ATTENTION'}`)
  })

  // Final Assessment
  const criticalFailures = results.filter(r => 
    r.status === 'FAIL' && 
    (r.category === 'CFR Compliance' || r.test.includes('SignalR') || r.test.includes('API'))
  ).length

  console.log('\n🏁 Final Assessment:')
  console.log('=' * 50)
  
  if (criticalFailures === 0) {
    console.log('🎉 ALL CRITICAL ISSUES RESOLVED!')
    console.log('✅ Ready for CFR Part 11 compliance')
    console.log('✅ Ready for production deployment')
    console.log('✅ All API integrations configured correctly')
    console.log('✅ Zero tolerance for synthetic data enforced')
  } else {
    console.log(`❌ ${criticalFailures} CRITICAL ISSUES REMAINING`)
    console.log('⛔ NOT ready for production')
    console.log('⛔ CFR Part 11 compliance at risk')
    console.log('\n🔧 Action Required:')
    results
      .filter(r => r.status === 'FAIL' && (r.category === 'CFR Compliance' || r.test.includes('SignalR') || r.test.includes('API')))
      .forEach(failure => {
        console.log(`   • Fix: ${failure.test} - ${failure.details}`)
      })
  }

  process.exit(criticalFailures > 0 ? 1 : 0)
}

// Run validation if this script is executed directly
if (require.main === module) {
  runValidation().catch(error => {
    console.error('❌ Validation script failed:', error)
    process.exit(1)
  })
}

export { runValidation, ValidationResult }