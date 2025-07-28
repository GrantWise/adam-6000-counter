"use client"

import { useState, useEffect } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Progress } from "@/components/ui/progress"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { TestTube, Download, RefreshCw, Copy, CheckCircle, XCircle } from "lucide-react"

interface ConnectionTestResult {
  stage: string
  status: "success" | "failed" | "running"
  duration: number
  message: string
  details?: string
}

interface LogEntry {
  timestamp: string
  level: "error" | "warning" | "info" | "debug"
  device: string
  message: string
  details?: string
}

interface ErrorAnalytics {
  errorType: string
  count: number
  lastOccurrence: string
  devices: string[]
  recoveryRate: number
}

interface DiagnosticsProps {
  selectedDevice?: string
}

const mockConnectionTest: ConnectionTestResult[] = [
  {
    stage: "TCP Connection Test",
    status: "success",
    duration: 12,
    message: "Connected to 192.168.1.100:502",
  },
  {
    stage: "Modbus Protocol Test",
    status: "success",
    duration: 23,
    message: "Device responds to Modbus requests",
  },
  {
    stage: "Register Read Test",
    status: "failed",
    duration: 5000,
    message: "Register 40 returned error code 02",
    details: "Illegal data address - register may not exist on this device",
  },
  {
    stage: "Performance Analysis",
    status: "success",
    duration: 145,
    message: "Average response time: 145ms (acceptable)",
  },
]

const mockLogs: LogEntry[] = [
  {
    timestamp: "2024-01-15 14:32:15",
    level: "error",
    device: "BOTTLING_LINE_3",
    message: "Connection timeout after 5000ms",
    details: "Failed to establish TCP connection to 192.168.1.102:502",
  },
  {
    timestamp: "2024-01-15 14:31:45",
    level: "warning",
    device: "QUALITY_GATE_1",
    message: "High response time detected",
    details: "Response time 245ms exceeds recommended threshold of 100ms",
  },
  {
    timestamp: "2024-01-15 14:30:12",
    level: "info",
    device: "PROD_LINE_1",
    message: "Counter overflow handled",
    details: "Channel 0 counter wrapped from 65535 to 0",
  },
  {
    timestamp: "2024-01-15 14:29:33",
    level: "error",
    device: "BOTTLING_LINE_3",
    message: "Modbus exception: Illegal data address",
    details: "Register 45 does not exist on device",
  },
]

const mockErrorAnalytics: ErrorAnalytics[] = [
  {
    errorType: "Connection timeout",
    count: 45,
    lastOccurrence: "2 minutes ago",
    devices: ["BOTTLING_LINE_3", "REMOTE_COUNTER_1"],
    recoveryRate: 15,
  },
  {
    errorType: "High response time",
    count: 23,
    lastOccurrence: "5 minutes ago",
    devices: ["QUALITY_GATE_1"],
    recoveryRate: 85,
  },
  {
    errorType: "Invalid register address",
    count: 12,
    lastOccurrence: "1 hour ago",
    devices: ["BOTTLING_LINE_3"],
    recoveryRate: 0,
  },
  {
    errorType: "CRC error",
    count: 8,
    lastOccurrence: "3 hours ago",
    devices: ["PROD_LINE_1", "QUALITY_GATE_1"],
    recoveryRate: 95,
  },
]

export function Diagnostics({ selectedDeviceProp }: DiagnosticsProps) {
  const [selectedDevice, setSelectedDevice] = useState(selectedDeviceProp || "PROD_LINE_1")
  const [connectionTest, setConnectionTest] = useState<ConnectionTestResult[]>([])
  const [isTestingConnection, setIsTestingConnection] = useState(false)
  const [testProgress, setTestProgress] = useState(0)
  const [logFilter, setLogFilter] = useState("")
  const [logLevel, setLogLevel] = useState("all")
  const [logDevice, setLogDevice] = useState(selectedDeviceProp || "all")

  useEffect(() => {
    if (selectedDeviceProp) {
      setSelectedDevice(selectedDeviceProp)
      setLogDevice(selectedDeviceProp)
    }
  }, [selectedDeviceProp])

  const handleConnectionTest = async () => {
    setIsTestingConnection(true)
    setTestProgress(0)
    setConnectionTest([])

    // Simulate connection test stages
    for (let i = 0; i < mockConnectionTest.length; i++) {
      setTestProgress((i / mockConnectionTest.length) * 100)
      await new Promise((resolve) => setTimeout(resolve, 1000))
      setConnectionTest((prev) => [...prev, mockConnectionTest[i]])
    }

    setTestProgress(100)
    setIsTestingConnection(false)
  }

  const getStatusIcon = (status: ConnectionTestResult["status"]) => {
    switch (status) {
      case "success":
        return <CheckCircle className="w-4 h-4 text-green-500" />
      case "failed":
        return <XCircle className="w-4 h-4 text-red-500" />
      case "running":
        return <div className="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
      default:
        return null
    }
  }

  const getLogLevelBadge = (level: LogEntry["level"]) => {
    switch (level) {
      case "error":
        return <Badge className="bg-red-500">ERROR</Badge>
      case "warning":
        return <Badge className="bg-yellow-500">WARNING</Badge>
      case "info":
        return <Badge className="bg-blue-500">INFO</Badge>
      case "debug":
        return <Badge variant="secondary">DEBUG</Badge>
      default:
        return <Badge variant="secondary">UNKNOWN</Badge>
    }
  }

  const filteredLogs = mockLogs.filter((log) => {
    const matchesFilter =
      log.message.toLowerCase().includes(logFilter.toLowerCase()) ||
      log.device.toLowerCase().includes(logFilter.toLowerCase())
    const matchesLevel = logLevel === "all" || log.level === logLevel
    const matchesDevice = logDevice === "all" || log.device === logDevice
    return matchesFilter && matchesLevel && matchesDevice
  })

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text)
  }

  return (
    <div className="space-y-6">
      <Tabs defaultValue="connection-test" className="w-full">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="connection-test">Connection Test</TabsTrigger>
          <TabsTrigger value="device-logs">Device Logs</TabsTrigger>
          <TabsTrigger value="error-analysis">Error Analysis</TabsTrigger>
        </TabsList>

        <TabsContent value="connection-test" className="space-y-4">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Connection Diagnostics</CardTitle>
                <div className="flex items-center space-x-2">
                  <Select value={selectedDevice} onValueChange={setSelectedDevice}>
                    <SelectTrigger className="w-48">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="PROD_LINE_1">Main Production Line</SelectItem>
                      <SelectItem value="QUALITY_GATE_1">Quality Inspection</SelectItem>
                      <SelectItem value="BOTTLING_LINE_3">Bottling Line 3</SelectItem>
                    </SelectContent>
                  </Select>
                  <Button onClick={handleConnectionTest} disabled={isTestingConnection}>
                    <TestTube className="w-4 h-4 mr-2" />
                    {isTestingConnection ? "Testing..." : "Test Connection"}
                  </Button>
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {isTestingConnection && (
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">Test Progress</span>
                    <span className="text-sm text-gray-600">{Math.round(testProgress)}%</span>
                  </div>
                  <Progress value={testProgress} className="h-2" />
                </div>
              )}

              {connectionTest.length > 0 && (
                <div className="space-y-3">
                  {connectionTest.map((test, index) => (
                    <div key={index} className="border rounded-lg p-4">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center space-x-2">
                          {getStatusIcon(test.status)}
                          <span className="font-medium">{test.stage}</span>
                        </div>
                        <div className="text-sm text-gray-600">{test.duration}ms</div>
                      </div>
                      <div className="text-sm text-gray-700 mb-2">{test.message}</div>
                      {test.details && (
                        <div className="text-xs text-gray-600 bg-gray-50 p-2 rounded">{test.details}</div>
                      )}
                    </div>
                  ))}

                  <div className="flex justify-end space-x-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => copyToClipboard(JSON.stringify(connectionTest, null, 2))}
                    >
                      <Copy className="w-4 h-4 mr-2" />
                      Copy Results
                    </Button>
                    <Button variant="outline" size="sm">
                      <Download className="w-4 h-4 mr-2" />
                      Export Report
                    </Button>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="device-logs" className="space-y-4">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Device Logs</CardTitle>
                <div className="flex items-center space-x-2">
                  <Button variant="outline" size="sm">
                    <RefreshCw className="w-4 h-4 mr-2" />
                    Refresh
                  </Button>
                  <Button variant="outline" size="sm">
                    <Download className="w-4 h-4 mr-2" />
                    Export Logs
                  </Button>
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center space-x-2">
                <Input
                  placeholder="Search logs..."
                  value={logFilter}
                  onChange={(e) => setLogFilter(e.target.value)}
                  className="flex-1"
                />
                <Select value={logLevel} onValueChange={setLogLevel}>
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Levels</SelectItem>
                    <SelectItem value="error">Error</SelectItem>
                    <SelectItem value="warning">Warning</SelectItem>
                    <SelectItem value="info">Info</SelectItem>
                    <SelectItem value="debug">Debug</SelectItem>
                  </SelectContent>
                </Select>
                <Select value={logDevice} onValueChange={setLogDevice}>
                  <SelectTrigger className="w-48">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Devices</SelectItem>
                    <SelectItem value="PROD_LINE_1">Main Production Line</SelectItem>
                    <SelectItem value="QUALITY_GATE_1">Quality Inspection</SelectItem>
                    <SelectItem value="BOTTLING_LINE_3">Bottling Line 3</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2 max-h-96 overflow-y-auto">
                {filteredLogs.map((log, index) => (
                  <div key={index} className="border rounded-lg p-3">
                    <div className="flex items-center justify-between mb-1">
                      <div className="flex items-center space-x-2">
                        {getLogLevelBadge(log.level)}
                        <span className="text-sm font-medium">{log.device}</span>
                      </div>
                      <span className="text-xs text-gray-500">{log.timestamp}</span>
                    </div>
                    <div className="text-sm text-gray-700 mb-1">{log.message}</div>
                    {log.details && <div className="text-xs text-gray-600 bg-gray-50 p-2 rounded">{log.details}</div>}
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="error-analysis" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Error Analysis Dashboard</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Error Type</TableHead>
                    <TableHead>Count</TableHead>
                    <TableHead>Last Occurrence</TableHead>
                    <TableHead>Affected Devices</TableHead>
                    <TableHead>Recovery Rate</TableHead>
                    <TableHead>Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {mockErrorAnalytics.map((error, index) => (
                    <TableRow key={index}>
                      <TableCell className="font-medium">{error.errorType}</TableCell>
                      <TableCell>
                        <Badge variant={error.count > 20 ? "destructive" : "secondary"}>{error.count}</Badge>
                      </TableCell>
                      <TableCell>{error.lastOccurrence}</TableCell>
                      <TableCell>
                        <div className="space-y-1">
                          {error.devices.map((device, i) => (
                            <Badge key={i} variant="outline" className="text-xs">
                              {device}
                            </Badge>
                          ))}
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center space-x-2">
                          <Progress value={error.recoveryRate} className="w-16 h-2" />
                          <span className="text-sm">{error.recoveryRate}%</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Button variant="outline" size="sm">
                          View Details
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Card>
              <CardHeader>
                <CardTitle>Recovery Statistics</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <div className="flex justify-between text-sm">
                      <span>Automatic Recovery</span>
                      <span>85%</span>
                    </div>
                    <Progress value={85} className="h-2 mt-1" />
                  </div>
                  <div>
                    <div className="flex justify-between text-sm">
                      <span>Manual Intervention</span>
                      <span>10%</span>
                    </div>
                    <Progress value={10} className="h-2 mt-1" />
                  </div>
                  <div>
                    <div className="flex justify-between text-sm">
                      <span>Still Failing</span>
                      <span>5%</span>
                    </div>
                    <Progress value={5} className="h-2 mt-1" />
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Mean Time to Recovery</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-sm">Network errors:</span>
                    <span className="text-sm font-medium">~30 seconds</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm">Device errors:</span>
                    <span className="text-sm font-medium">~5 minutes</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm">Configuration errors:</span>
                    <span className="text-sm font-medium">~20 minutes</span>
                  </div>
                  <div className="flex justify-between border-t pt-2">
                    <span className="text-sm font-medium">Overall MTTR:</span>
                    <span className="text-sm font-bold">~8 minutes</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  )
}
