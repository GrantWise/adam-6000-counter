/**
 * Device Management Component
 * CRUD interface for ADAM device configuration and management
 */

import React from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Settings, Plus, Search } from 'lucide-react'

const DeviceManagement: React.FC = () => {
  return (
    <div className="device-management">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-neutral-900 mb-2">Device Management</h1>
        <p className="text-neutral-600">Configure and manage ADAM counter devices</p>
      </div>

      <Card className="p-8 text-center">
        <Settings className="w-16 h-16 text-neutral-300 mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-neutral-900 mb-4">
          Device Management Interface
        </h2>
        <p className="text-neutral-600 mb-6">
          CRUD interface for device configuration, discovery, and management will be implemented here.
        </p>
        <div className="flex justify-center space-x-4">
          <Button variant="outline">
            <Plus className="w-4 h-4 mr-2" />
            Add Device
          </Button>
          <Button variant="outline">
            <Search className="w-4 h-4 mr-2" />
            Discover Devices
          </Button>
        </div>
      </Card>
    </div>
  )
}

export default DeviceManagement