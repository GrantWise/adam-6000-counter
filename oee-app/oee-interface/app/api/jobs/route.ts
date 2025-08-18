import { type NextRequest, NextResponse } from "next/server"
import { createJobService } from "@/lib/services/jobService"
import { NewJobRequest } from "@/lib/types/oee"
import { validateBody, validateQuery, startJobSchema, getJobSchema } from "@/lib/validation/schemas"
import { withPermission, withAuth } from "@/lib/auth/middleware"
import { startJobUseCase } from "@/lib/usecases/StartJobUseCase"

// POST /api/jobs - Start new job using use case (requires operate permission)
export const POST = withPermission(async (request: NextRequest, user) => {
  try {
    const body = await request.json()
    
    // Validate request body
    const validatedData = validateBody(startJobSchema)(body)
    const { jobNumber, partNumber, targetRate, deviceId, operatorId } = validatedData

    // Use StartJobUseCase for business logic
    const result = await startJobUseCase.execute({
      jobNumber,
      partNumber,
      deviceId,
      targetRate,
      operatorId: operatorId || user.username || 'system'
    })

    if (!result.isSuccess) {
      return NextResponse.json({ error: result.errorMessage }, { status: 400 })
    }

    // Transform to API response format
    const responseJob = {
      jobId: result.value.jobId,
      jobNumber: result.value.jobNumber,
      partNumber: result.value.partNumber,
      deviceId: deviceId,
      targetRate: result.value.targetRate,
      startTime: result.value.startTime.toISOString(),
      operatorId: user.username || 'system',
      status: result.value.status,
    }

    return NextResponse.json(responseJob, { status: 201 })
  } catch (error) {
    console.error("Error starting job:", error)
    
    // Handle validation errors specifically
    if (error instanceof Error && error.message.includes('Validation error')) {
      return NextResponse.json({ error: error.message }, { status: 400 })
    }
    
    return NextResponse.json({ error: "Internal server error" }, { status: 500 })
  }
}, 'operate')

// GET /api/jobs - Get current job using real job service (requires view permission)
export const GET = withAuth(async (request: NextRequest, user) => {
  try {
    const { searchParams } = new URL(request.url)
    
    // Validate query parameters
    const queryParams = Object.fromEntries(searchParams.entries())
    const { deviceId } = validateQuery(getJobSchema)(queryParams)

    // Create job service instance for this device
    const jobService = createJobService(deviceId)
    
    // Check if client wants domain model format
    const format = searchParams.get('format')
    
    if (format === 'domain') {
      // Return Work_Order domain model
      const result = await jobService.getCurrentWorkOrder()
      
      if (!result.isSuccess) {
        return NextResponse.json({ error: result.errorMessage }, { status: 500 })
      }
      
      if (!result.value) {
        return NextResponse.json(null)
      }
      
      return NextResponse.json(result.value.toSummary())
    }
    
    // Legacy format for backward compatibility
    const result = await jobService.getCurrentJob()

    if (!result.isSuccess) {
      return NextResponse.json({ error: result.errorMessage }, { status: 500 })
    }

    // Return null if no active job
    if (!result.value) {
      return NextResponse.json(null)
    }

    // Transform database response to match frontend expectations
    const responseJob = {
      jobId: result.value.job_id,
      jobNumber: result.value.job_number,
      partNumber: result.value.part_number,
      deviceId: result.value.device_id,
      targetRate: result.value.target_rate,
      startTime: new Date(result.value.start_time).toISOString(),
      operatorId: result.value.operator_id,
      status: result.value.status,
    }

    return NextResponse.json(responseJob)
  } catch (error) {
    console.error("Error getting current job:", error)
    
    // Handle validation errors specifically
    if (error instanceof Error && error.message.includes('validation error')) {
      return NextResponse.json({ error: error.message }, { status: 400 })
    }
    
    return NextResponse.json({ error: "Internal server error" }, { status: 500 })
  }
})