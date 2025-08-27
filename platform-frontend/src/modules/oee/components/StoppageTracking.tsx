/**
 * Stoppage Tracking Component
 * Real-time stoppage monitoring and acknowledgment interface
 */

import React from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { AlertTriangle, Clock, CheckCircle } from 'lucide-react'

const StoppageTracking: React.FC = () => {
  return (
    <div className="stoppage-tracking">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-neutral-900 mb-2">Stoppage Tracking</h1>
        <p className="text-neutral-600">Monitor, acknowledge, and analyze equipment stoppages</p>
      </div>

      <Card className="p-8 text-center">
        <AlertTriangle className="w-16 h-16 text-yellow-500 mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-neutral-900 mb-4">
          Stoppage Tracking & Analysis
        </h2>
        <p className="text-neutral-600 mb-6">
          Real-time stoppage alerts, acknowledgment workflows, and root cause analysis tools.
        </p>
        <div className="flex justify-center space-x-4">
          <Button variant="outline">
            <Clock className="w-4 h-4 mr-2" />
            Active Stoppages
          </Button>
          <Button variant="outline">
            <CheckCircle className="w-4 h-4 mr-2" />
            Acknowledge All
          </Button>
        </div>
      </Card>
    </div>
  )
}

export default StoppageTracking