"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { CurrentMetricsResponse, Job } from "@/lib/types/oee"

interface StoppageManagerProps {
  metrics: CurrentMetricsResponse | null
  currentJob: Job | null
  onClassifyStoppage: (classification: {
    category: string
    subCategory: string
    comments: string
  }) => void
  deviceId: string
}

const stoppageCategories = {
  Mechanical: ["Jam", "Wear", "Breakage", "Alignment", "Lubrication", "Hydraulic", "Pneumatic", "Belt/Chain", "Other"],
  Electrical: ["Power Loss", "Motor Failure", "Sensor Issue", "Wiring", "Control Panel", "Overload", "Short Circuit", "Voltage", "Other"],
  Material: ["Out of Stock", "Wrong Material", "Quality Issue", "Supplier Delay", "Damaged", "Contaminated", "Wrong Size", "Expired", "Other"],
  Quality: ["Defect Rate", "Inspection Fail", "Rework Required", "Calibration", "Tolerance", "Surface Finish", "Dimension", "Assembly", "Other"],
  Operator: ["Break", "Training", "Absent", "Shift Change", "Meeting", "Safety Issue", "Procedure", "Documentation", "Other"],
  Planned: ["Maintenance", "Cleaning", "Setup", "Changeover", "Inspection", "Calibration", "Testing", "Documentation", "Other"],
  External: ["Power Outage", "Utility Issue", "Weather", "Supplier", "Customer", "Regulatory", "Emergency", "Transport", "Other"],
  Setup: ["Tool Change", "Program Load", "Fixture", "Calibration", "First Article", "Adjustment", "Validation", "Documentation", "Other"],
  Other: ["Unknown", "Investigation", "Multiple Causes", "System Error", "Communication", "Software", "Network", "Database", "Other"],
}

export function StoppageManager({ 
  metrics, 
  currentJob,
  onClassifyStoppage,
  deviceId 
}: StoppageManagerProps) {
  const [showStoppageModal, setShowStoppageModal] = useState(false)
  const [classificationLevel, setClassificationLevel] = useState<1 | 2>(1)
  const [selectedCategory, setSelectedCategory] = useState<string>("")
  const [selectedSubCategory, setSelectedSubCategory] = useState<string>("")
  const [stoppageComments, setStoppageComments] = useState<string>("")
  const [stoppageLoading, setStoppageLoading] = useState(false)

  const formatJobDuration = (startTime: Date) => {
    const now = new Date()
    const diffMs = now.getTime() - startTime.getTime()
    const hours = Math.floor(diffMs / (1000 * 60 * 60))
    const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60))
    return `${hours}h ${minutes}m ago`
  }

  const handleCategorySelect = (category: string) => {
    setSelectedCategory(category)
    setClassificationLevel(2)
  }

  const handleStoppageConfirm = async () => {
    if (selectedCategory && selectedSubCategory) {
      setStoppageLoading(true)
      
      try {
        await onClassifyStoppage({
          category: selectedCategory,
          subCategory: selectedSubCategory,
          comments: stoppageComments.trim() || ''
        })
        
        // Reset modal state
        setShowStoppageModal(false)
        setClassificationLevel(1)
        setSelectedCategory("")
        setSelectedSubCategory("")
        setStoppageComments("")
      } catch (error) {
        console.error('Error classifying stoppage:', error)
        alert('Error classifying stoppage. Please try again.')
      } finally {
        setStoppageLoading(false)
      }
    }
  }

  const handleStoppageBack = () => {
    if (classificationLevel === 2) {
      setClassificationLevel(1)
      setSelectedSubCategory("")
    }
  }

  return (
    <>
      <Card className="p-4 space-y-3">
        <CardHeader className="pb-4">
          <CardTitle className="text-xl font-bold text-gray-900">Stoppages</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center gap-2">
            <span className="font-medium">Status:</span>
            <Badge className={
              metrics?.status === 'running' 
                ? "bg-green-100 text-green-800 text-base px-3 py-1" 
                : metrics?.status === 'stopped'
                ? "bg-red-100 text-red-800 text-base px-3 py-1"
                : "bg-gray-100 text-gray-800 text-base px-3 py-1"
            }>
              {metrics?.status === 'running' ? 'RUNNING ✓' : 
               metrics?.status === 'stopped' ? 'STOPPED ⚠' : 
               'UNKNOWN'}
            </Badge>
          </div>

          <div className="space-y-2">
            <div className="flex justify-between">
              <span className="text-gray-600">Running:</span>
              <span className="font-semibold text-gray-900">
                {currentJob ? formatJobDuration(currentJob.startTime) : '--'}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Stopped:</span>
              <span className="font-semibold text-gray-900">23m total</span>
            </div>
          </div>

          <div className="pt-4 border-t space-y-2">
            <div className="flex justify-between">
              <span className="text-gray-600">Last Stop:</span>
              <span className="font-semibold text-gray-900">8 minutes</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-gray-600">Reason:</span>
              <Badge className="bg-red-100 text-red-800 text-base px-3 py-1">UNCLASSIFIED</Badge>
            </div>
          </div>

          <div className="flex-1 flex items-end">
            <Button
              size="lg"
              className="w-full h-10 text-base font-semibold bg-orange-600 hover:bg-orange-700"
              onClick={() => setShowStoppageModal(true)}
              disabled={stoppageLoading}
            >
              {stoppageLoading ? 'CLASSIFYING...' : 'CLASSIFY STOPPAGE'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Stoppage Classification Modal */}
      <Dialog open={showStoppageModal} onOpenChange={setShowStoppageModal}>
        <DialogContent className="sm:max-w-lg max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="text-xl font-bold">
              {classificationLevel === 1 ? "Classify Stoppage" : `${selectedCategory} Issue`}
            </DialogTitle>
          </DialogHeader>

          <div className="py-4">
            <div className="mb-4">
              <span className="text-base font-medium">Duration: 8 minutes</span>
            </div>

            {!selectedCategory ? (
              <div className="space-y-4">
                <div className="text-base font-medium">Select stoppage category:</div>
                <div className="grid grid-cols-3 gap-2">
                  {Object.keys(stoppageCategories).map((category) => (
                    <Button
                      key={category}
                      variant="outline"
                      className="h-12 text-sm font-medium bg-transparent"
                      onClick={() => handleCategorySelect(category)}
                      disabled={stoppageLoading}
                    >
                      {category}
                    </Button>
                  ))}
                </div>
              </div>
            ) : (
              <div className="space-y-4">
                <div className="text-base font-medium">Select specific reason:</div>
                <div className="grid grid-cols-3 gap-2">
                  {stoppageCategories[selectedCategory as keyof typeof stoppageCategories].map((subCategory) => (
                    <Button
                      key={subCategory}
                      variant={selectedSubCategory === subCategory ? "default" : "outline"}
                      className="h-12 text-sm font-medium"
                      onClick={() => setSelectedSubCategory(subCategory)}
                      disabled={stoppageLoading}
                    >
                      {subCategory}
                    </Button>
                  ))}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="comments">Comments (Optional):</Label>
                  <Textarea
                    id="comments"
                    placeholder="Additional details about the stoppage..."
                    value={stoppageComments}
                    onChange={(e) => setStoppageComments(e.target.value)}
                    className="min-h-16"
                    disabled={stoppageLoading}
                  />
                </div>
              </div>
            )}
          </div>

          <div className="flex gap-2">
            {classificationLevel === 1 ? (
              <Button 
                variant="outline" 
                onClick={() => setShowStoppageModal(false)} 
                className="h-10"
                disabled={stoppageLoading}
              >
                Cancel
              </Button>
            ) : (
              <>
                <Button 
                  variant="outline" 
                  onClick={handleStoppageBack} 
                  className="h-10 bg-transparent"
                  disabled={stoppageLoading}
                >
                  {selectedCategory ? "Back" : "Cancel"}
                </Button>
                {selectedCategory && (
                  <Button 
                    onClick={handleStoppageConfirm} 
                    disabled={!selectedSubCategory || stoppageLoading} 
                    className="h-10"
                  >
                    {stoppageLoading ? 'Classifying...' : 'Confirm'}
                  </Button>
                )}
              </>
            )}
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}