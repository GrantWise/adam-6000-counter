"use client"

import { useState } from "react"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { DeviceManagement } from "@/components/device-management"
import { RealTimeMonitoring } from "@/components/real-time-monitoring"
import { GlobalConfiguration } from "@/components/global-configuration"
import { TestingValidation } from "@/components/testing-validation"
import { Diagnostics } from "@/components/diagnostics"
import { PerformanceMonitoring } from "@/components/performance-monitoring"
import { Activity, Settings, TestTube, Stethoscope, BarChart3, Monitor } from "lucide-react"

export default function HomePage() {
  const [activeTab, setActiveTab] = useState("devices")
  const [selectedDiagnosticDevice, setSelectedDiagnosticDevice] = useState<string>("")

  const handleNavigateToTab = (tab: string, deviceId?: string) => {
    setActiveTab(tab)
    if (deviceId && tab === "diagnostics") {
      setSelectedDiagnosticDevice(deviceId)
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">ADAM Counter Logger</h1>
            <p className="text-sm text-gray-600">Industrial Counter Management System</p>
          </div>
          <div className="flex items-center space-x-4">
            <div className="flex items-center space-x-2">
              <div className="w-3 h-3 bg-green-500 rounded-full"></div>
              <span className="text-sm text-gray-600">System Healthy</span>
            </div>
          </div>
        </div>
      </header>

      <main className="p-6">
        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
          <TabsList className="grid w-full grid-cols-6 mb-6">
            <TabsTrigger value="devices" className="flex items-center gap-2">
              <Settings className="w-4 h-4" />
              Device Management
            </TabsTrigger>
            <TabsTrigger value="monitoring" className="flex items-center gap-2">
              <Monitor className="w-4 h-4" />
              Real-Time Monitoring
            </TabsTrigger>
            <TabsTrigger value="config" className="flex items-center gap-2">
              <Activity className="w-4 h-4" />
              Global Config
            </TabsTrigger>
            <TabsTrigger value="testing" className="flex items-center gap-2">
              <TestTube className="w-4 h-4" />
              Testing
            </TabsTrigger>
            <TabsTrigger value="diagnostics" className="flex items-center gap-2">
              <Stethoscope className="w-4 h-4" />
              Diagnostics
            </TabsTrigger>
            <TabsTrigger value="performance" className="flex items-center gap-2">
              <BarChart3 className="w-4 h-4" />
              Performance
            </TabsTrigger>
          </TabsList>

          <TabsContent value="devices">
            <DeviceManagement />
          </TabsContent>

          <TabsContent value="monitoring">
            <RealTimeMonitoring onNavigateToTab={handleNavigateToTab} />
          </TabsContent>

          <TabsContent value="config">
            <GlobalConfiguration />
          </TabsContent>

          <TabsContent value="testing">
            <TestingValidation />
          </TabsContent>

          <TabsContent value="diagnostics">
            <Diagnostics selectedDevice={selectedDiagnosticDevice} />
          </TabsContent>

          <TabsContent value="performance">
            <PerformanceMonitoring />
          </TabsContent>
        </Tabs>
      </main>
    </div>
  )
}
