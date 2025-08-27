import React from 'react'
import { CfrComplianceDemo } from '@/components/dashboard/cfr-compliance-demo'

/**
 * Test page for CFR Part 11 Compliance Demo
 * Bypasses authentication for testing purposes
 */
export const TestCfrDemo: React.FC = () => {
  return (
    <div className="min-h-screen bg-background p-6">
      <div className="max-w-7xl mx-auto">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-foreground">CFR Part 11 Compliance Test Page</h1>
          <p className="text-muted-foreground mt-2">
            Testing CFR Part 11 compliance implementation - bypassing authentication for testing
          </p>
        </div>
        
        <CfrComplianceDemo />
      </div>
    </div>
  )
}

export default TestCfrDemo