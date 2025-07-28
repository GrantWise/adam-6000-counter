"use client"

import { useState, useEffect } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Activity, Cpu, HardDrive, Network, Database, Clock, AlertTriangle } from "lucide-react"

interface SystemMetrics {
  cpuUsage: number
  memoryUsage: number
  memoryTotal: number
  networkBandwidth: number
  influxdbStatus: "connected" | "disconnected"
  influxdbWriteRate: number
  influxdbQueueSize: number
}

interface CounterMetrics {
  channel: string
  device: string
  currentCount: number
  todayTotal: number
  averageRate: number
  peakRate: number
  dataQuality: number
  lastUpdate: string
  status: "active" | "idle" | "error"
  overflowEvents: number
}

interface PerformanceStats {
  metric: string
  current: number
  average: number
  peak: number
  unit: string
  status: "good" | "warning" | "critical"
}

const mockSystemMetrics: SystemMetrics = {
  cpuUsage: 45,
  memoryUsage: 2.8,
  memoryTotal: 8.0,
  networkBandwidth: 12.5,
  influxdbStatus: "connected",
  influxdbWriteRate: 150,
  influxdbQueueSize: 23,
}

const mockCounterMetrics: CounterMetrics[] = [
  {
    channel: "Good_Products",
    device: "PROD_LINE_1",
    currentCount: 15847,
    todayTotal: 45230,
    averageRate: 45,
    peakRate: 67,
    dataQuality: 99.8,
    lastUpdate: "2s ago",
    status: "active",
    overflowEvents: 0,
  },
  {
    channel: "Rejects",
    device: "PROD_LINE_1",
    currentCount: 234,
    todayTotal: 892,
    averageRate: 2,
    peakRate: 8,
    dataQuality: 99.5,
    lastUpdate: "2s ago",
    status: "active",
    overflowEvents: 0,
  },
  {
    channel: "Passed",
    device: "QUALITY_GATE_1",
    currentCount: 8923,
    todayTotal: 23456,
    averageRate: 38,
    peakRate: 52,
    dataQuality: 95.2,
    lastUpdate: "5s ago",
    status: "active",
    overflowEvents: 1,
  },
  {
    channel: "Bottles_Filled",
    device: "BOTTLING_LINE_3",
    currentCount: 0,
    todayTotal: 0,
    averageRate: 0,
    peakRate: 0,
    dataQuality: 0,
    lastUpdate: "2m ago",
    status: "error",
    overflowEvents: 0,
  },
]

const mockPerformanceStats: PerformanceStats[] = [
  {
    metric: "Counter Processing Rate",
    current: 1250,
    average: 1180,
    peak: 1450,
    unit: "readings/sec",
    status: "good",
  },
  {
    metric: "Batch Processing Time",
    current: 45,
    average: 38,
    peak: 89,
    unit: "ms",
    status: "good",
  },
  {
    metric: "Device Response Time",
    current: 67,
    average: 52,
    peak: 245,
    unit: "ms",
    status: "warning",
  },
  {
    metric: "Data Quality Score",
    current: 97.8,
    average: 98.2,
    peak: 99.9,
    unit: "%",
    status: "good",
  },
  {
    metric: "Memory Usage",
    current: 35,
    average: 32,
    peak: 78,
    unit: "%",
    status: "good",
  },
]

export function PerformanceMonitoring() {
  const [systemMetrics, setSystemMetrics] = useState<SystemMetrics>(mockSystemMetrics)
  const [counterMetrics, setCounterMetrics] = useState<CounterMetrics[]>(mockCounterMetrics)
  const [timeRange, setTimeRange] = useState("1h")

  useEffect(() => {
    // Simulate real-time metric updates
    const interval = setInterval(() => {
      setSystemMetrics((prev) => ({
        ...prev,
        cpuUsage: Math.max(20, Math.min(80, prev.cpuUsage + (Math.random() - 0.5) * 10)),
        memoryUsage: Math.max(1.5, Math.min(6.0, prev.memoryUsage + (Math.random() - 0.5) * 0.2)),
        networkBandwidth: Math.max(5, Math.min(50, prev.networkBandwidth + (Math.random() - 0.5) * 5)),
        influxdbWriteRate: Math.max(50, Math.min(300, prev.influxdbWriteRate + (Math.random() - 0.5) * 20)),
      }))
    }, 2000)

    return () => clearInterval(interval)
  }, [])

  const getStatusColor = (status: string) => {
    switch (status) {
      case "good":
        return "text-green-600"
      case "warning":
        return "text-yellow-600"
      case "critical":
        return "text-red-600"
      default:
        return "text-gray-600"
    }
  }

  const getStatusIcon = (status: CounterMetrics["status"]) => {
    switch (status) {
      case "active":
        return <Activity className="w-4 h-4 text-green-500" />
      case "idle":
        return <Clock className="w-4 h-4 text-yellow-500" />
      case "error":
        return <AlertTriangle className="w-4 h-4 text-red-500" />
      default:
        return null
    }
  }

  const getCpuStatusColor = (usage: number) => {
    if (usage < 50) return "text-green-600"
    if (usage < 80) return "text-yellow-600"
    return "text-red-600"
  }

  const getMemoryPercentage = (used: number, total: number) => {
    return (used / total) * 100
  }

  return (
    <div className="space-y-6">
      <Tabs defaultValue="system-health" className="w-full">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="system-health">System Health</TabsTrigger>
          <TabsTrigger value="counter-metrics">Counter Metrics</TabsTrigger>
          <TabsTrigger value="performance-stats">Performance Stats</TabsTrigger>
        </TabsList>

        <TabsContent value="system-health" className="space-y-4">
          {/* Resource Usage Overview */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <Card>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-sm">CPU Usage</CardTitle>
                  <Cpu className="w-4 h-4 text-gray-500" />
                </div>
              </CardHeader>
              <CardContent>
                <div className={`text-2xl font-bold ${getCpuStatusColor(systemMetrics.cpuUsage)}`}>
                  {systemMetrics.cpuUsage}%
                </div>
                <Progress value={systemMetrics.cpuUsage} className="h-2 mt-2" />
                <div className="text-xs text-gray-600 mt-1">
                  {systemMetrics.cpuUsage < 50 ? "Normal" : systemMetrics.cpuUsage < 80 ? "High" : "Critical"}
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-sm">Memory Usage</CardTitle>
                  <HardDrive className="w-4 h-4 text-gray-500" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{systemMetrics.memoryUsage.toFixed(1)}GB</div>
                <Progress
                  value={getMemoryPercentage(systemMetrics.memoryUsage, systemMetrics.memoryTotal)}
                  className="h-2 mt-2"
                />
                <div className="text-xs text-gray-600 mt-1">of {systemMetrics.memoryTotal}GB total</div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-sm">Network</CardTitle>
                  <Network className="w-4 h-4 text-gray-500" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{systemMetrics.networkBandwidth.toFixed(1)}</div>
                <div className="text-xs text-gray-600 mt-1">Mbps</div>
                <div className="text-xs text-gray-600">Device communication</div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-sm">InfluxDB</CardTitle>
                  <Database className="w-4 h-4 text-gray-500" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex items-center space-x-2 mb-2">
                  <div
                    className={`w-3 h-3 rounded-full ${systemMetrics.influxdbStatus === "connected" ? "bg-green-500" : "bg-red-500"}`}
                  ></div>
                  <Badge variant={systemMetrics.influxdbStatus === "connected" ? "default" : "destructive"}>
                    {systemMetrics.influxdbStatus}
                  </Badge>
                </div>
                <div className="text-sm">
                  <div>Write Rate: {systemMetrics.influxdbWriteRate}/sec</div>
                  <div>Queue: {systemMetrics.influxdbQueueSize} items</div>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Performance Metrics */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Performance Metrics</CardTitle>
                <Select value={timeRange} onValueChange={setTimeRange}>
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1h">Last Hour</SelectItem>
                    <SelectItem value="6h">Last 6 Hours</SelectItem>
                    <SelectItem value="24h">Last 24 Hours</SelectItem>
                    <SelectItem value="7d">Last 7 Days</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Metric</TableHead>
                    <TableHead>Current</TableHead>
                    <TableHead>Average</TableHead>
                    <TableHead>Peak</TableHead>
                    <TableHead>Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {mockPerformanceStats.map((stat, index) => (
                    <TableRow key={index}>
                      <TableCell className="font-medium">{stat.metric}</TableCell>
                      <TableCell>
                        <span className={getStatusColor(stat.status)}>
                          {stat.current} {stat.unit}
                        </span>
                      </TableCell>
                      <TableCell>
                        {stat.average} {stat.unit}
                      </TableCell>
                      <TableCell>
                        {stat.peak} {stat.unit}
                      </TableCell>
                      <TableCell>
                        <Badge
                          variant={
                            stat.status === "good" ? "default" : stat.status === "warning" ? "secondary" : "destructive"
                          }
                        >
                          {stat.status.toUpperCase()}
                        </Badge>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="counter-metrics" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Counter-Specific Metrics</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Device</TableHead>
                    <TableHead>Channel</TableHead>
                    <TableHead>Current Count</TableHead>
                    <TableHead>Today's Total</TableHead>
                    <TableHead>Avg Rate</TableHead>
                    <TableHead>Peak Rate</TableHead>
                    <TableHead>Data Quality</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Overflows</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {counterMetrics.map((metric, index) => (
                    <TableRow key={index}>
                      <TableCell className="font-medium">{metric.device}</TableCell>
                      <TableCell>{metric.channel}</TableCell>
                      <TableCell className="font-mono text-lg">{metric.currentCount.toLocaleString()}</TableCell>
                      <TableCell className="font-mono">{metric.todayTotal.toLocaleString()}</TableCell>
                      <TableCell>{metric.averageRate}/min</TableCell>
                      <TableCell>{metric.peakRate}/min</TableCell>
                      <TableCell>
                        <div className="flex items-center space-x-2">
                          <Progress value={metric.dataQuality} className="w-16 h-2" />
                          <span className="text-sm">{metric.dataQuality}%</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center space-x-2">
                          {getStatusIcon(metric.status)}
                          <Badge
                            variant={
                              metric.status === "active"
                                ? "default"
                                : metric.status === "idle"
                                  ? "secondary"
                                  : "destructive"
                            }
                          >
                            {metric.status.toUpperCase()}
                          </Badge>
                        </div>
                      </TableCell>
                      <TableCell>
                        {metric.overflowEvents > 0 ? (
                          <Badge variant="secondary">{metric.overflowEvents}</Badge>
                        ) : (
                          <span className="text-gray-400">0</span>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>

          {/* Rate Calculation Performance */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Card>
              <CardHeader>
                <CardTitle>Rate Calculation Performance</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-sm">Average calculation time:</span>
                  <span className="text-sm font-medium">8.5ms</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm">Data points used:</span>
                  <span className="text-sm font-medium">100</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm">Rate stability:</span>
                  <span className="text-sm font-medium">±2.3%</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm">Anomalies detected:</span>
                  <span className="text-sm font-medium">0 today</span>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Overflow Event Tracking</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-sm">Total overflow events:</span>
                  <span className="text-sm font-medium">1 today</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm">Last overflow:</span>
                  <span className="text-sm font-medium">2 hours ago</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm">Overflow frequency:</span>
                  <span className="text-sm font-medium">0.5/day</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm">Auto-handling success:</span>
                  <span className="text-sm font-medium">100%</span>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="performance-stats" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Device Response Time Distribution</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-5 gap-4 text-center">
                  <div>
                    <div className="text-2xl font-bold text-green-600">65%</div>
                    <div className="text-xs text-gray-600">{"<10ms"}</div>
                    <div className="text-xs text-gray-500">Excellent</div>
                  </div>
                  <div>
                    <div className="text-2xl font-bold text-green-600">25%</div>
                    <div className="text-xs text-gray-600">10-50ms</div>
                    <div className="text-xs text-gray-500">Good</div>
                  </div>
                  <div>
                    <div className="text-2xl font-bold text-yellow-600">8%</div>
                    <div className="text-xs text-gray-600">50-100ms</div>
                    <div className="text-xs text-gray-500">Acceptable</div>
                  </div>
                  <div>
                    <div className="text-2xl font-bold text-orange-600">2%</div>
                    <div className="text-xs text-gray-600">100-500ms</div>
                    <div className="text-xs text-gray-500">Slow</div>
                  </div>
                  <div>
                    <div className="text-2xl font-bold text-red-600">0%</div>
                    <div className="text-xs text-gray-600">{">500ms"}</div>
                    <div className="text-xs text-gray-500">Poor</div>
                  </div>
                </div>

                <div className="border-t pt-4">
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-center">
                    <div>
                      <div className="text-lg font-bold">52ms</div>
                      <div className="text-xs text-gray-600">Average Response</div>
                    </div>
                    <div>
                      <div className="text-lg font-bold">89ms</div>
                      <div className="text-xs text-gray-600">95th Percentile</div>
                    </div>
                    <div>
                      <div className="text-lg font-bold">±15ms</div>
                      <div className="text-xs text-gray-600">Standard Deviation</div>
                    </div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Per-Device Breakdown */}
          <Card>
            <CardHeader>
              <CardTitle>Per-Device Performance</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <Card className="border-2">
                    <CardHeader className="pb-3">
                      <CardTitle className="text-sm">PROD_LINE_1</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2">
                        <div className="flex justify-between text-sm">
                          <span>Avg Response:</span>
                          <span className="font-medium text-green-600">23ms</span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span>Success Rate:</span>
                          <span className="font-medium">99.8%</span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span>Data Quality:</span>
                          <span className="font-medium">99.8%</span>
                        </div>
                      </div>
                    </CardContent>
                  </Card>

                  <Card className="border-2">
                    <CardHeader className="pb-3">
                      <CardTitle className="text-sm">QUALITY_GATE_1</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2">
                        <div className="flex justify-between text-sm">
                          <span>Avg Response:</span>
                          <span className="font-medium text-yellow-600">145ms</span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span>Success Rate:</span>
                          <span className="font-medium">95.2%</span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span>Data Quality:</span>
                          <span className="font-medium">95.2%</span>
                        </div>
                      </div>
                    </CardContent>
                  </Card>

                  <Card className="border-2">
                    <CardHeader className="pb-3">
                      <CardTitle className="text-sm">BOTTLING_LINE_3</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2">
                        <div className="flex justify-between text-sm">
                          <span>Avg Response:</span>
                          <span className="font-medium text-red-600">Timeout</span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span>Success Rate:</span>
                          <span className="font-medium text-red-600">0%</span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span>Data Quality:</span>
                          <span className="font-medium text-red-600">0%</span>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
