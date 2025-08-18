import { type NextRequest, NextResponse } from "next/server"
import { createJobService } from "@/lib/services/jobService"
import { endJobUseCase } from "@/lib/usecases/EndJobUseCase"
import { withPermission } from "@/lib/auth/middleware"

// PUT /api/jobs/{jobId}/end - End current job using use case (requires operate permission)
export const PUT = withPermission(async (request: NextRequest, user) => {
  try {
    // Extract jobId from URL path
    const url = new URL(request.url)
    const pathParts = url.pathname.split('/')
    const jobIdIndex = pathParts.indexOf('jobs') + 1
    const jobId = pathParts[jobIdIndex]

    if (!jobId || isNaN(Number(jobId))) {
      return NextResponse.json({ error: "Valid Job ID is required" }, { status: 400 })
    }

    // Get device ID from request body or query params
    const body = await request.json().catch(() => ({}))
    const { deviceId } = body

    if (!deviceId) {
      return NextResponse.json({ error: "Device ID is required" }, { status: 400 })
    }

    // Use EndJobUseCase for business logic
    const result = await endJobUseCase.execute({
      deviceId,
      jobId,
      operatorId: user.username || 'system'
    })

    if (!result.isSuccess) {
      return NextResponse.json({ error: result.errorMessage }, { status: 400 })
    }

    const response = {
      jobId: result.value.jobId,
      endTime: new Date().toISOString(),
      status: result.value.status,
    }

    return NextResponse.json(response)
  } catch (error) {
    console.error("Error ending job:", error)
    return NextResponse.json({ error: "Internal server error" }, { status: 500 })
  }
}, 'operate')