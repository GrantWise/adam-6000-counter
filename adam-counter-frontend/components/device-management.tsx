"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Plus, Edit, Trash2, TestTube, Power, PowerOff, Download, Upload, Loader2 } from "lucide-react"
import { DeviceConfigurationForm } from "@/components/device-configuration-form"
import { ChannelConfiguration } from "@/components/channel-configuration"
import { useDevices, useTestDevice, useEnableDevice, useDisableDevice, useDeleteDevice } from "@/hooks/useDevices"
import { DeviceStatus } from "@/lib/types/api"
import { format } from "date-fns"

export function DeviceManagement() {
  const { data: devices, isLoading, error } = useDevices()
  const testDevice = useTestDevice()
  const enableDevice = useEnableDevice()
  const disableDevice = useDisableDevice()
  const deleteDevice = useDeleteDevice()
  
  const [selectedDevice, setSelectedDevice] = useState<string | null>(null)
  const [showAddDevice, setShowAddDevice] = useState(false)
  const [showChannelConfig, setShowChannelConfig] = useState(false)

  const getStatusColor = (status: DeviceStatus) => {
    switch (status) {
      case DeviceStatus.Online:
        return "bg-green-500"
      case DeviceStatus.Warning:
        return "bg-yellow-500"
      case DeviceStatus.Error:
        return "bg-red-600"
      case DeviceStatus.Offline:
        return "bg-red-500"
      default:
        return "bg-gray-500"
    }
  }

  const getStatusText = (status: DeviceStatus) => {
    switch (status) {
      case DeviceStatus.Online:
        return "Connected"
      case DeviceStatus.Warning:
        return "Warning"
      case DeviceStatus.Error:
        return "Error"
      case DeviceStatus.Offline:
        return "Disconnected"
      default:
        return "Unknown"
    }
  }

  const handleTestConnection = (deviceId: string) => {
    testDevice.mutate(deviceId)
  }

  const handleToggleDevice = (deviceId: string, currentEnabled: boolean) => {
    if (currentEnabled) {
      disableDevice.mutate(deviceId)
    } else {
      enableDevice.mutate(deviceId)
    }
  }

  const handleDeleteDevice = (deviceId: string) => {
    if (confirm('Are you sure you want to delete this device?')) {
      deleteDevice.mutate(deviceId)
    }
  }

  const formatLastContact = (timestamp: string | undefined) => {
    if (!timestamp) return 'Never'
    const date = new Date(timestamp)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffSecs = Math.floor(diffMs / 1000)
    
    if (diffSecs < 60) return `${diffSecs} seconds ago`
    if (diffSecs < 3600) return `${Math.floor(diffSecs / 60)} minutes ago`
    if (diffSecs < 86400) return `${Math.floor(diffSecs / 3600)} hours ago`
    return format(date, 'MMM dd, HH:mm')
  }

  if (isLoading) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center py-8">
          <Loader2 className="h-8 w-8 animate-spin" />
          <span className="ml-2">Loading devices...</span>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card>
        <CardContent className="py-8">
          <p className="text-center text-red-500">Error loading devices. Please try again.</p>
        </CardContent>
      </Card>
    )
  }


  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Device Management</CardTitle>
            <div className="flex items-center space-x-2">
              <Button variant="outline" size="sm">
                <Upload className="w-4 h-4 mr-2" />
                Import Config
              </Button>
              <Button variant="outline" size="sm">
                <Download className="w-4 h-4 mr-2" />
                Export Config
              </Button>
              <Dialog open={showAddDevice} onOpenChange={setShowAddDevice}>
                <DialogTrigger asChild>
                  <Button>
                    <Plus className="w-4 h-4 mr-2" />
                    Add Device
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
                  <DialogHeader>
                    <DialogTitle>Add New Device</DialogTitle>
                  </DialogHeader>
                  <DeviceConfigurationForm
                    onSave={() => setShowAddDevice(false)}
                    onCancel={() => setShowAddDevice(false)}
                  />
                </DialogContent>
              </Dialog>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Device ID</TableHead>
                <TableHead>Device Name</TableHead>
                <TableHead>IP:Port</TableHead>
                <TableHead>Unit ID</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Last Contact</TableHead>
                <TableHead>Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {devices?.map((device) => (
                <TableRow key={device.config.deviceId}>
                  <TableCell className="font-mono">{device.config.deviceId}</TableCell>
                  <TableCell>{device.config.description || device.config.deviceId}</TableCell>
                  <TableCell className="font-mono">
                    {device.config.ipAddress}:{device.config.port}
                  </TableCell>
                  <TableCell>{device.config.unitId}</TableCell>
                  <TableCell>
                    <div className="flex items-center space-x-2">
                      <div className={`w-3 h-3 rounded-full ${getStatusColor(device.health?.status || DeviceStatus.Unknown)}`}></div>
                      <Badge variant={device.health?.status === DeviceStatus.Online ? "default" : "destructive"}>
                        {getStatusText(device.health?.status || DeviceStatus.Unknown)}
                      </Badge>
                    </div>
                  </TableCell>
                  <TableCell>{formatLastContact(device.health?.timestamp)}</TableCell>
                  <TableCell>
                    <div className="flex items-center space-x-1">
                      <Dialog>
                        <DialogTrigger asChild>
                          <Button variant="outline" size="sm" onClick={() => setSelectedDevice(device.config.deviceId)}>
                            <Edit className="w-4 h-4" />
                          </Button>
                        </DialogTrigger>
                        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
                          <DialogHeader>
                            <DialogTitle>Edit Device: {device.config.description || device.config.deviceId}</DialogTitle>
                          </DialogHeader>
                          <DeviceConfigurationForm
                            device={device.config}
                            onSave={() => setSelectedDevice(null)}
                            onCancel={() => setSelectedDevice(null)}
                          />
                        </DialogContent>
                      </Dialog>

                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          setSelectedDevice(device.config.deviceId)
                          setShowChannelConfig(true)
                        }}
                      >
                        Channels
                      </Button>

                      <Button 
                        variant="outline" 
                        size="sm" 
                        onClick={() => handleTestConnection(device.config.deviceId)}
                        disabled={testDevice.isPending}
                      >
                        <TestTube className="w-4 h-4" />
                      </Button>

                      <Button 
                        variant="outline" 
                        size="sm" 
                        onClick={() => handleToggleDevice(device.config.deviceId, device.config.enabled)}
                        disabled={enableDevice.isPending || disableDevice.isPending}
                      >
                        {device.config.enabled ? <PowerOff className="w-4 h-4" /> : <Power className="w-4 h-4" />}
                      </Button>

                      <Button 
                        variant="destructive" 
                        size="sm" 
                        onClick={() => handleDeleteDevice(device.config.deviceId)}
                        disabled={deleteDevice.isPending}
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Dialog open={showChannelConfig} onOpenChange={setShowChannelConfig}>
        <DialogContent className="max-w-6xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Channel Configuration: {selectedDevice}</DialogTitle>
          </DialogHeader>
          {selectedDevice && (
            <ChannelConfiguration deviceId={selectedDevice} onClose={() => setShowChannelConfig(false)} />
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
