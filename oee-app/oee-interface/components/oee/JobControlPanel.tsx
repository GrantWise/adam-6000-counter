"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Job } from "@/lib/types/oee"

interface JobControlPanelProps {
  currentJob: Job | null
  jobLoading: boolean
  onStartJob: (jobData: { 
    jobNumber: string
    partNumber: string
    targetRate: number
    quantity: number
  }) => void
  onEndJob: () => void
  deviceId: string
}

export function JobControlPanel({
  currentJob,
  jobLoading,
  onStartJob,
  onEndJob,
  deviceId
}: JobControlPanelProps) {
  const [showJobModal, setShowJobModal] = useState(false)
  const [newJobNumber, setNewJobNumber] = useState("")
  const [newPartNumber, setNewPartNumber] = useState("")
  const [newTargetRate, setNewTargetRate] = useState("")
  const [newJobQuantity, setNewJobQuantity] = useState("")

  const formatJobDuration = (startTime: Date) => {
    const now = new Date()
    const diffMs = now.getTime() - startTime.getTime()
    const hours = Math.floor(diffMs / (1000 * 60 * 60))
    const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60))
    return `${hours}h ${minutes}m ago`
  }

  const handleStartJob = () => {
    if (newJobNumber && newPartNumber && newTargetRate && newJobQuantity) {
      onStartJob({
        jobNumber: newJobNumber.trim(),
        partNumber: newPartNumber.trim(),
        targetRate: Number(newTargetRate),
        quantity: Number(newJobQuantity)
      })
      
      // Reset form and close modal
      setNewJobNumber("")
      setNewPartNumber("")
      setNewTargetRate("")
      setNewJobQuantity("")
      setShowJobModal(false)
    }
  }

  return (
    <>
      <Card className="p-4 space-y-3">
        <CardHeader className="pb-4">
          <CardTitle className="text-xl font-bold text-gray-900">Job Control</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            {currentJob ? (
              <>
                <div className="text-lg font-semibold text-gray-900">
                  Current Job: {currentJob.jobNumber} - {currentJob.partNumber}
                </div>
                <div className="text-gray-600">
                  Started: {currentJob.startTime.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })} (
                  {formatJobDuration(currentJob.startTime)})
                </div>
              </>
            ) : jobLoading ? (
              <div className="text-lg font-semibold text-gray-500">Loading job...</div>
            ) : (
              <div className="text-lg font-semibold text-gray-500">No active job</div>
            )}
          </div>

          <div className="flex-1 flex items-end">
            {!currentJob ? (
              <Button
                size="lg"
                className="w-full h-10 text-base font-semibold bg-green-600 hover:bg-green-700"
                onClick={() => setShowJobModal(true)}
                disabled={jobLoading}
              >
                {jobLoading ? 'LOADING...' : 'START NEW JOB'}
              </Button>
            ) : (
              <Button
                size="lg"
                className="w-full h-10 text-base font-semibold bg-red-600 hover:bg-red-700"
                onClick={onEndJob}
                disabled={jobLoading}
              >
                {jobLoading ? 'ENDING...' : 'END JOB'}
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {/* New Job Modal */}
      <Dialog open={showJobModal} onOpenChange={setShowJobModal}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-xl font-bold">Start New Job</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="jobNumber" className="text-base font-medium">
                Job Number
              </Label>
              <Input
                id="jobNumber"
                value={newJobNumber}
                onChange={(e) => setNewJobNumber(e.target.value)}
                placeholder="Enter job number"
                className="h-12 text-base"
                disabled={jobLoading}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="partNumber" className="text-base font-medium">
                Part Number
              </Label>
              <Input
                id="partNumber"
                value={newPartNumber}
                onChange={(e) => setNewPartNumber(e.target.value)}
                placeholder="Enter part number"
                className="h-12 text-base"
                disabled={jobLoading}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="targetRate" className="text-base font-medium">
                Target Rate (units/min)
              </Label>
              <Input
                id="targetRate"
                type="number"
                value={newTargetRate}
                onChange={(e) => setNewTargetRate(e.target.value)}
                placeholder="Enter target rate"
                className="h-12 text-base"
                min="1"
                max="10000"
                disabled={jobLoading}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="quantity" className="text-base font-medium">
                Job Quantity
              </Label>
              <Input
                id="quantity"
                type="number"
                value={newJobQuantity}
                onChange={(e) => setNewJobQuantity(e.target.value)}
                placeholder="Enter total quantity"
                className="h-12 text-base"
                min="1"
                max="100000"
                disabled={jobLoading}
              />
            </div>
          </div>
          <div className="flex gap-2">
            <Button 
              variant="outline" 
              onClick={() => setShowJobModal(false)} 
              className="h-10"
              disabled={jobLoading}
            >
              Cancel
            </Button>
            <Button
              onClick={handleStartJob}
              disabled={!newJobNumber || !newPartNumber || !newTargetRate || !newJobQuantity || jobLoading}
              className="h-10"
            >
              {jobLoading ? 'Starting...' : 'Start Job'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}