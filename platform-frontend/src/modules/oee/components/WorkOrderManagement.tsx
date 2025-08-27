/**
 * Work Order Management Component
 * Interface for creating, managing, and tracking production work orders
 */

import React from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { ClipboardList, Plus, Filter } from 'lucide-react'

const WorkOrderManagement: React.FC = () => {
  return (
    <div className="work-order-management">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-neutral-900 mb-2">Work Order Management</h1>
        <p className="text-neutral-600">Create, track, and manage production work orders</p>
      </div>

      <Card className="p-8 text-center">
        <ClipboardList className="w-16 h-16 text-blue-500 mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-neutral-900 mb-4">
          Work Order Management System
        </h2>
        <p className="text-neutral-600 mb-6">
          Complete work order lifecycle management with OEE tracking and resource allocation.
        </p>
        <div className="flex justify-center space-x-4">
          <Button variant="outline">
            <Plus className="w-4 h-4 mr-2" />
            Create Work Order
          </Button>
          <Button variant="outline">
            <Filter className="w-4 h-4 mr-2" />
            Filter Orders
          </Button>
        </div>
      </Card>
    </div>
  )
}

export default WorkOrderManagement