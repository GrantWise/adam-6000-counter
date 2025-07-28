"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Progress } from "@/components/ui/progress"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Play, Square, Download, CheckCircle, AlertTriangle, XCircle, SkipForward } from "lucide-react"

interface TestResult {
  id: string
  name: string
  category: string
  status: "pass" | "warning" | "fail" | "skip" | "running"
  duration: number
  message: string
  details?: string
}

interface TestCategory {
  id: string
  name: string
  description: string
  estimatedDuration: string
  requirements: string[]
}

const testCategories: TestCategory[] = [
  {
    id: "configuration",
    name: "Configuration Tests",
    description: "Validates all device and channel configurations",
    estimatedDuration: "~30 seconds",
    requirements: ["At least one device configured"],
  },
  {
    id: "connection",
    name: "Connection Tests",
    description: "Tests connectivity to all configured devices",
    estimatedDuration: "~60 seconds",
    requirements: ["Valid device configurations", "Network connectivity"],
  },
  {
    id: "data_quality",
    name: "Data Quality Tests",
    description: "Verifies data integrity and validity",
    estimatedDuration: "~45 seconds",
    requirements: ["Connected devices", "Readable channels"],
  },
  {
    id: "performance",
    name: "Performance Benchmarks",
    description: "Measures system capacity and response times",
    estimatedDuration: "~90 seconds",
    requirements: ["System resources available"],
  },
  {
    id: "health_check",
    name: "Health Check Tests",
    description: "Validates monitoring and alert systems",
    estimatedDuration: "~20 seconds",
    requirements: ["Monitoring enabled"],
  },
  {
    id: "device_discovery",
    name: "Device Discovery",
    description: "Scans network for ADAM devices",
    estimatedDuration: "~120 seconds",
    requirements: ["Network access"],
  },
]

const mockTestResults: TestResult[] = [
  {
    id: "config_validation",
    name: "Configuration Validation",
    category: "configuration",
    status: "pass",
    duration: 1.2,
    message: "All device configurations are valid",
  },
  {
    id: "register_mapping",
    name: "Register Mapping Check",
    category: "configuration",
    status: "warning",
    duration: 0.8,
    message: "Overlapping registers detected on Device PROD_LINE_1",
    details: "Channels 2 and 3 both use register 4-5. This may cause data conflicts.",
  },
  {
    id: "device_connectivity",
    name: "Device Connectivity",
    category: "connection",
    status: "fail",
    duration: 5.0,
    message: "Failed to connect to BOTTLING_LINE_3",
    details: "Connection timeout after 5000ms. Check device power and network connectivity.",
  },
  {
    id: "modbus_protocol",
    name: "Modbus Protocol Test",
    category: "connection",
    status: "pass",
    duration: 2.3,
    message: "All devices respond to Modbus requests",
  },
]

export function TestingValidation() {
  const [selectedCategory, setSelectedCategory] = useState<string>("")
  const [testResults, setTestResults] = useState<TestResult[]>(mockTestResults)
  const [isRunning, setIsRunning] = useState(false)
  const [progress, setProgress] = useState(0)
  const [currentTest, setCurrentTest] = useState("")
  const [productionReadinessScore, setProductionReadinessScore] = useState(87)

  const handleRunAllTests = async () => {
    setIsRunning(true)
    setProgress(0)
    setCurrentTest("Starting test suite...")

    // Simulate running all tests
    const totalTests = testCategories.length
    for (let i = 0; i < totalTests; i++) {
      setCurrentTest(`Running ${testCategories[i].name}...`)
      setProgress(((i + 1) / totalTests) * 100)
      await new Promise((resolve) => setTimeout(resolve, 2000))
    }

    setCurrentTest("Test suite completed")
    setIsRunning(false)
  }

  const handleRunCategory = async (categoryId: string) => {
    setIsRunning(true)
    setProgress(0)
    setCurrentTest(`Running ${testCategories.find((c) => c.id === categoryId)?.name}...`)

    // Simulate category test
    await new Promise((resolve) => setTimeout(resolve, 3000))
    setProgress(100)
    setCurrentTest("Category tests completed")
    setIsRunning(false)
  }

  const handleStopTests = () => {
    setIsRunning(false)
    setCurrentTest("Tests stopped by user")
  }

  const getStatusIcon = (status: TestResult["status"]) => {
    switch (status) {
      case "pass":
        return <CheckCircle className="w-4 h-4 text-green-500" />
      case "warning":
        return <AlertTriangle className="w-4 h-4 text-yellow-500" />
      case "fail":
        return <XCircle className="w-4 h-4 text-red-500" />
      case "skip":
        return <SkipForward className="w-4 h-4 text-gray-500" />
      case "running":
        return <div className="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
      default:
        return null
    }
  }

  const getStatusBadge = (status: TestResult["status"]) => {
    switch (status) {
      case "pass":
        return <Badge className="bg-green-500">PASS</Badge>
      case "warning":
        return <Badge className="bg-yellow-500">WARNING</Badge>
      case "fail":
        return <Badge className="bg-red-500">FAIL</Badge>
      case "skip":
        return <Badge variant="secondary">SKIP</Badge>
      case "running":
        return <Badge className="bg-blue-500">RUNNING</Badge>
      default:
        return <Badge variant="secondary">UNKNOWN</Badge>
    }
  }

  const getReadinessColor = (score: number) => {
    if (score >= 90) return "text-green-600"
    if (score >= 70) return "text-yellow-600"
    return "text-red-600"
  }

  const getReadinessStatus = (score: number) => {
    if (score >= 90) return "Ready for Production"
    if (score >= 70) return "Functional - Needs Attention"
    return "Not Ready - Critical Issues"
  }

  return (
    <div className="space-y-6">
      {/* Production Readiness Overview */}
      <Card>
        <CardHeader>
          <CardTitle>Production Readiness Assessment</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center">
              <div className={`text-4xl font-bold ${getReadinessColor(productionReadinessScore)}`}>
                {productionReadinessScore}%
              </div>
              <div className="text-sm text-gray-600">Overall Score</div>
              <div className={`text-sm font-medium ${getReadinessColor(productionReadinessScore)}`}>
                {getReadinessStatus(productionReadinessScore)}
              </div>
            </div>
            <div className="space-y-2">
              <div className="text-sm font-medium">Critical Issues</div>
              <div className="text-2xl font-bold text-red-600">2</div>
              <div className="text-xs text-gray-600">
                • Device connection failed
                <br />• InfluxDB write errors
              </div>
            </div>
            <div className="space-y-2">
              <div className="text-sm font-medium">Recommendations</div>
              <div className="text-xs text-gray-600">
                • Fix BOTTLING_LINE_3 connectivity
                <br />• Verify InfluxDB configuration
                <br />• Consider reducing poll intervals
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Test Execution Controls */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Test Execution</CardTitle>
            <div className="flex items-center space-x-2">
              <Button onClick={handleRunAllTests} disabled={isRunning} className="bg-blue-600 hover:bg-blue-700">
                <Play className="w-4 h-4 mr-2" />
                Run All Tests
              </Button>
              {isRunning && (
                <Button variant="destructive" onClick={handleStopTests}>
                  <Square className="w-4 h-4 mr-2" />
                  Stop Tests
                </Button>
              )}
              <Button variant="outline">
                <Download className="w-4 h-4 mr-2" />
                Export Report
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {isRunning && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Test Progress</span>
                <span className="text-sm text-gray-600">{Math.round(progress)}%</span>
              </div>
              <Progress value={progress} className="h-2" />
              <div className="text-sm text-gray-600">{currentTest}</div>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {testCategories.map((category) => (
              <Card key={category.id} className="border-2">
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div className="font-medium">{category.name}</div>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleRunCategory(category.id)}
                      disabled={isRunning}
                    >
                      <Play className="w-3 h-3 mr-1" />
                      Run
                    </Button>
                  </div>
                </CardHeader>
                <CardContent className="space-y-2">
                  <div className="text-sm text-gray-600">{category.description}</div>
                  <div className="text-xs text-gray-500">
                    <div>Duration: {category.estimatedDuration}</div>
                    <div>Requirements:</div>
                    <ul className="list-disc list-inside ml-2">
                      {category.requirements.map((req, index) => (
                        <li key={index}>{req}</li>
                      ))}
                    </ul>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Test Results */}
      <Card>
        <CardHeader>
          <CardTitle>Test Results</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {testResults.map((result) => (
              <div key={result.id} className="border rounded-lg p-4">
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center space-x-2">
                    {getStatusIcon(result.status)}
                    <span className="font-medium">{result.name}</span>
                    {getStatusBadge(result.status)}
                  </div>
                  <div className="text-sm text-gray-600">{result.duration}s</div>
                </div>
                <div className="text-sm text-gray-700 mb-2">{result.message}</div>
                {result.details && <div className="text-xs text-gray-600 bg-gray-50 p-2 rounded">{result.details}</div>}
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Individual Test Selection */}
      <Card>
        <CardHeader>
          <CardTitle>Individual Test Execution</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-sm font-medium">Select Test Category</label>
              <Select value={selectedCategory} onValueChange={setSelectedCategory}>
                <SelectTrigger>
                  <SelectValue placeholder="Choose a test category" />
                </SelectTrigger>
                <SelectContent>
                  {testCategories.map((category) => (
                    <SelectItem key={category.id} value={category.id}>
                      {category.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-end">
              <Button
                disabled={!selectedCategory || isRunning}
                onClick={() => selectedCategory && handleRunCategory(selectedCategory)}
              >
                <Play className="w-4 h-4 mr-2" />
                Run Selected Tests
              </Button>
            </div>
          </div>

          {selectedCategory && (
            <div className="bg-blue-50 p-4 rounded-lg">
              <div className="font-medium mb-2">{testCategories.find((c) => c.id === selectedCategory)?.name}</div>
              <div className="text-sm text-gray-700 mb-2">
                {testCategories.find((c) => c.id === selectedCategory)?.description}
              </div>
              <div className="text-xs text-gray-600">
                <div>
                  Estimated Duration: {testCategories.find((c) => c.id === selectedCategory)?.estimatedDuration}
                </div>
                <div>Requirements:</div>
                <ul className="list-disc list-inside ml-2">
                  {testCategories
                    .find((c) => c.id === selectedCategory)
                    ?.requirements.map((req, index) => (
                      <li key={index}>{req}</li>
                    ))}
                </ul>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
