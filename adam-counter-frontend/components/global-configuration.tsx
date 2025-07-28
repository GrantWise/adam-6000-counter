"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Separator } from "@/components/ui/separator"
import { Save, TestTube } from "lucide-react"

export function GlobalConfiguration() {
  const [config, setConfig] = useState({
    // Performance Settings
    maxConcurrentDevices: 10,
    dataBufferSize: 10000,
    batchSize: 100,
    batchTimeout: 5000,
    healthCheckInterval: 30000,

    // Error Handling
    enableAutoRecovery: true,
    maxConsecutiveFailures: 10,
    deviceTimeoutMinutes: 5,

    // Monitoring
    enablePerformanceCounters: true,
    enableDetailedLogging: false,
    enableDemoMode: false,

    // InfluxDB Settings
    influxUrl: "http://localhost:8086",
    influxToken: "",
    influxOrganization: "manufacturing_corp",
    influxBucket: "production_counters",
    influxMeasurement: "counter_data",
    writeBatchSize: 50,
    flushInterval: 5000,
  })

  const [testResult, setTestResult] = useState<string | null>(null)
  const [testing, setTesting] = useState(false)

  const handleConfigChange = (field: string, value: any) => {
    setConfig((prev) => ({ ...prev, [field]: value }))
  }

  const handleTestInfluxDB = async () => {
    setTesting(true)
    setTestResult(null)

    // Simulate InfluxDB connection test
    setTimeout(() => {
      setTestResult("✅ InfluxDB connection successful")
      setTesting(false)
    }, 2000)
  }

  const handleSave = () => {
    console.log("Saving global configuration:", config)
    // Show success toast
  }

  return (
    <div className="space-y-6">
      {/* Performance Settings */}
      <Card>
        <CardHeader>
          <CardTitle>Performance Settings</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="maxConcurrentDevices">Max Concurrent Devices (1-50)</Label>
              <Input
                id="maxConcurrentDevices"
                type="number"
                min="1"
                max="50"
                value={config.maxConcurrentDevices}
                onChange={(e) => handleConfigChange("maxConcurrentDevices", Number.parseInt(e.target.value))}
              />
              <p className="text-xs text-gray-600 mt-1">
                How many devices to poll simultaneously. Higher = faster polling but more resource usage.
              </p>
            </div>
            <div>
              <Label htmlFor="dataBufferSize">Data Buffer Size (100-100,000)</Label>
              <Input
                id="dataBufferSize"
                type="number"
                min="100"
                max="100000"
                value={config.dataBufferSize}
                onChange={(e) => handleConfigChange("dataBufferSize", Number.parseInt(e.target.value))}
              />
              <p className="text-xs text-gray-600 mt-1">Number of readings to hold in memory before processing.</p>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="batchSize">Batch Size (1-1,000)</Label>
              <Input
                id="batchSize"
                type="number"
                min="1"
                max="1000"
                value={config.batchSize}
                onChange={(e) => handleConfigChange("batchSize", Number.parseInt(e.target.value))}
              />
              <p className="text-xs text-gray-600 mt-1">Number of readings to process together.</p>
            </div>
            <div>
              <Label htmlFor="batchTimeout">Batch Timeout (ms)</Label>
              <Input
                id="batchTimeout"
                type="number"
                value={config.batchTimeout}
                onChange={(e) => handleConfigChange("batchTimeout", Number.parseInt(e.target.value))}
              />
              <p className="text-xs text-gray-600 mt-1">Maximum time to wait before processing partial batch.</p>
            </div>
          </div>

          <div>
            <Label htmlFor="healthCheckInterval">Health Check Interval (ms)</Label>
            <Input
              id="healthCheckInterval"
              type="number"
              value={config.healthCheckInterval}
              onChange={(e) => handleConfigChange("healthCheckInterval", Number.parseInt(e.target.value))}
              className="w-64"
            />
            <p className="text-xs text-gray-600 mt-1">How often to verify device connectivity.</p>
          </div>
        </CardContent>
      </Card>

      {/* Error Handling */}
      <Card>
        <CardHeader>
          <CardTitle>Error Handling</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center space-x-2">
            <Switch
              id="enableAutoRecovery"
              checked={config.enableAutoRecovery}
              onCheckedChange={(checked) => handleConfigChange("enableAutoRecovery", checked)}
            />
            <Label htmlFor="enableAutoRecovery">Enable Automatic Recovery</Label>
          </div>
          <p className="text-xs text-gray-600">System automatically attempts to reconnect failed devices.</p>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="maxConsecutiveFailures">Max Consecutive Failures</Label>
              <Input
                id="maxConsecutiveFailures"
                type="number"
                value={config.maxConsecutiveFailures}
                onChange={(e) => handleConfigChange("maxConsecutiveFailures", Number.parseInt(e.target.value))}
              />
              <p className="text-xs text-gray-600 mt-1">Failed reads before marking device offline.</p>
            </div>
            <div>
              <Label htmlFor="deviceTimeoutMinutes">Device Timeout (minutes)</Label>
              <Input
                id="deviceTimeoutMinutes"
                type="number"
                value={config.deviceTimeoutMinutes}
                onChange={(e) => handleConfigChange("deviceTimeoutMinutes", Number.parseInt(e.target.value))}
              />
              <p className="text-xs text-gray-600 mt-1">Time before considering device unresponsive.</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Monitoring */}
      <Card>
        <CardHeader>
          <CardTitle>Monitoring</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-3">
            <div className="flex items-center space-x-2">
              <Switch
                id="enablePerformanceCounters"
                checked={config.enablePerformanceCounters}
                onCheckedChange={(checked) => handleConfigChange("enablePerformanceCounters", checked)}
              />
              <Label htmlFor="enablePerformanceCounters">Enable Performance Counters</Label>
            </div>
            <p className="text-xs text-gray-600">Collect detailed performance metrics for troubleshooting.</p>

            <div className="flex items-center space-x-2">
              <Switch
                id="enableDetailedLogging"
                checked={config.enableDetailedLogging}
                onCheckedChange={(checked) => handleConfigChange("enableDetailedLogging", checked)}
              />
              <Label htmlFor="enableDetailedLogging">Enable Detailed Logging</Label>
            </div>
            <p className="text-xs text-gray-600">Verbose logs for debugging. Warning: Can impact performance.</p>

            <div className="flex items-center space-x-2">
              <Switch
                id="enableDemoMode"
                checked={config.enableDemoMode}
                onCheckedChange={(checked) => handleConfigChange("enableDemoMode", checked)}
              />
              <Label htmlFor="enableDemoMode">Enable Demo Mode</Label>
            </div>
            <p className="text-xs text-gray-600">Generate fake counter data for testing and training.</p>
          </div>
        </CardContent>
      </Card>

      {/* InfluxDB Settings */}
      <Card>
        <CardHeader>
          <CardTitle>InfluxDB Settings</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="influxUrl">URL</Label>
              <Input
                id="influxUrl"
                value={config.influxUrl}
                onChange={(e) => handleConfigChange("influxUrl", e.target.value)}
                placeholder="http://localhost:8086"
              />
            </div>
            <div>
              <Label htmlFor="influxToken">Token</Label>
              <Input
                id="influxToken"
                type="password"
                value={config.influxToken}
                onChange={(e) => handleConfigChange("influxToken", e.target.value)}
                placeholder="Authentication token"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="influxOrganization">Organization</Label>
              <Input
                id="influxOrganization"
                value={config.influxOrganization}
                onChange={(e) => handleConfigChange("influxOrganization", e.target.value)}
              />
            </div>
            <div>
              <Label htmlFor="influxBucket">Bucket</Label>
              <Input
                id="influxBucket"
                value={config.influxBucket}
                onChange={(e) => handleConfigChange("influxBucket", e.target.value)}
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div>
              <Label htmlFor="influxMeasurement">Measurement</Label>
              <Input
                id="influxMeasurement"
                value={config.influxMeasurement}
                onChange={(e) => handleConfigChange("influxMeasurement", e.target.value)}
              />
            </div>
            <div>
              <Label htmlFor="writeBatchSize">Write Batch Size</Label>
              <Input
                id="writeBatchSize"
                type="number"
                value={config.writeBatchSize}
                onChange={(e) => handleConfigChange("writeBatchSize", Number.parseInt(e.target.value))}
              />
            </div>
            <div>
              <Label htmlFor="flushInterval">Flush Interval (ms)</Label>
              <Input
                id="flushInterval"
                type="number"
                value={config.flushInterval}
                onChange={(e) => handleConfigChange("flushInterval", Number.parseInt(e.target.value))}
              />
            </div>
          </div>

          <div className="flex items-center space-x-4">
            <Button variant="outline" onClick={handleTestInfluxDB} disabled={testing}>
              <TestTube className="w-4 h-4 mr-2" />
              {testing ? "Testing..." : "Test Connection"}
            </Button>
            {testResult && <div className="text-sm text-green-600">{testResult}</div>}
          </div>
        </CardContent>
      </Card>

      <Separator />

      {/* Save Button */}
      <div className="flex justify-end">
        <Button onClick={handleSave} size="lg">
          <Save className="w-4 h-4 mr-2" />
          Save Global Configuration
        </Button>
      </div>
    </div>
  )
}
