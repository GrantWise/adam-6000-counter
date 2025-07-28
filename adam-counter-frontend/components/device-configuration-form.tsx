"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { Separator } from "@/components/ui/separator"
import { TestTube, Save, X } from "lucide-react"

interface Device {
  id: string
  name: string
  ipAddress: string
  port: number
  unitId: number
  status: "connected" | "warning" | "disconnected" | "error"
  lastContact: string
  enabled: boolean
}

interface DeviceConfigurationFormProps {
  device?: Device
  onSave: () => void
  onCancel: () => void
}

export function DeviceConfigurationForm({ device, onSave, onCancel }: DeviceConfigurationFormProps) {
  const [formData, setFormData] = useState({
    deviceId: device?.id || "",
    deviceName: device?.name || "",
    description: "",
    ipAddress: device?.ipAddress || "",
    port: device?.port || 502,
    unitId: device?.unitId || 1,
    timeout: 5000,
    maxRetries: 3,
    retryDelay: 1000,
    pollInterval: 2000,
    enabled: device?.enabled ?? true,
    priority: "normal",
  })

  const [testResult, setTestResult] = useState<string | null>(null)
  const [testing, setTesting] = useState(false)

  const handleInputChange = (field: string, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
  }

  const handleTestConnection = async () => {
    setTesting(true)
    setTestResult(null)

    // Simulate connection test
    setTimeout(() => {
      setTestResult("✅ Connection successful (Response time: 23ms)")
      setTesting(false)
    }, 2000)
  }

  const handleSave = () => {
    // Validate and save configuration
    console.log("Saving device configuration:", formData)
    onSave()
  }

  return (
    <div className="space-y-6">
      {/* Device Information Section */}
      <Card>
        <CardHeader>
          <CardTitle>Device Information</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="deviceId">Device ID</Label>
              <Input
                id="deviceId"
                value={formData.deviceId}
                onChange={(e) => handleInputChange("deviceId", e.target.value)}
                placeholder="e.g., ADAM_001"
              />
            </div>
            <div>
              <Label htmlFor="deviceName">Device Name</Label>
              <Input
                id="deviceName"
                value={formData.deviceName}
                onChange={(e) => handleInputChange("deviceName", e.target.value)}
                placeholder="e.g., Main Production Line Counter"
              />
            </div>
          </div>
          <div>
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              value={formData.description}
              onChange={(e) => handleInputChange("description", e.target.value)}
              placeholder="e.g., Counts bottles after capping station, before case packer"
              rows={3}
            />
          </div>
        </CardContent>
      </Card>

      {/* Connection Settings Section */}
      <Card>
        <CardHeader>
          <CardTitle>Connection Settings</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-3 gap-4">
            <div>
              <Label htmlFor="ipAddress">IP Address</Label>
              <Input
                id="ipAddress"
                value={formData.ipAddress}
                onChange={(e) => handleInputChange("ipAddress", e.target.value)}
                placeholder="192.168.1.100"
              />
            </div>
            <div>
              <Label htmlFor="port">Port</Label>
              <Input
                id="port"
                type="number"
                value={formData.port}
                onChange={(e) => handleInputChange("port", Number.parseInt(e.target.value))}
                placeholder="502"
              />
            </div>
            <div>
              <Label htmlFor="unitId">Unit ID</Label>
              <Input
                id="unitId"
                type="number"
                value={formData.unitId}
                onChange={(e) => handleInputChange("unitId", Number.parseInt(e.target.value))}
                placeholder="1"
                min="1"
                max="247"
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div>
              <Label htmlFor="timeout">Timeout (ms)</Label>
              <Input
                id="timeout"
                type="number"
                value={formData.timeout}
                onChange={(e) => handleInputChange("timeout", Number.parseInt(e.target.value))}
                placeholder="5000"
              />
            </div>
            <div>
              <Label htmlFor="maxRetries">Max Retries</Label>
              <Input
                id="maxRetries"
                type="number"
                value={formData.maxRetries}
                onChange={(e) => handleInputChange("maxRetries", Number.parseInt(e.target.value))}
                placeholder="3"
              />
            </div>
            <div>
              <Label htmlFor="retryDelay">Retry Delay (ms)</Label>
              <Input
                id="retryDelay"
                type="number"
                value={formData.retryDelay}
                onChange={(e) => handleInputChange("retryDelay", Number.parseInt(e.target.value))}
                placeholder="1000"
              />
            </div>
          </div>

          <div className="flex items-center space-x-4">
            <Button variant="outline" onClick={handleTestConnection} disabled={testing}>
              <TestTube className="w-4 h-4 mr-2" />
              {testing ? "Testing..." : "Test Connection"}
            </Button>
            {testResult && <div className="text-sm text-green-600">{testResult}</div>}
          </div>
        </CardContent>
      </Card>

      {/* Polling Configuration Section */}
      <Card>
        <CardHeader>
          <CardTitle>Polling Configuration</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="pollInterval">Poll Interval (ms)</Label>
              <Input
                id="pollInterval"
                type="number"
                value={formData.pollInterval}
                onChange={(e) => handleInputChange("pollInterval", Number.parseInt(e.target.value))}
                placeholder="2000"
                min="100"
                max="300000"
              />
            </div>
            <div>
              <Label htmlFor="priority">Priority Level</Label>
              <Select value={formData.priority} onValueChange={(value) => handleInputChange("priority", value)}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="high">High</SelectItem>
                  <SelectItem value="normal">Normal</SelectItem>
                  <SelectItem value="low">Low</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <Switch
              id="enabled"
              checked={formData.enabled}
              onCheckedChange={(checked) => handleInputChange("enabled", checked)}
            />
            <Label htmlFor="enabled">Enable Device</Label>
          </div>
        </CardContent>
      </Card>

      <Separator />

      {/* Action Buttons */}
      <div className="flex justify-end space-x-2">
        <Button variant="outline" onClick={onCancel}>
          <X className="w-4 h-4 mr-2" />
          Cancel
        </Button>
        <Button onClick={handleSave}>
          <Save className="w-4 h-4 mr-2" />
          Save Configuration
        </Button>
      </div>
    </div>
  )
}
