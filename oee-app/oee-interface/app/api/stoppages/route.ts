import { type NextRequest, NextResponse } from "next/server"
import { createStoppageService } from "@/lib/services/stoppageService"
import { validateQuery, getStoppagesSchema } from "@/lib/validation/schemas"

// GET /api/stoppages - Get unclassified stoppages using real stoppage service
export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url)
    
    // Validate query parameters
    const queryParams = Object.fromEntries(searchParams.entries())
    const { deviceId: validatedDeviceId } = validateQuery(getStoppagesSchema)(queryParams)
    const deviceId = validatedDeviceId || process.env.DEFAULT_DEVICE_ID || 'Device001'

    // Create stoppage service instance for this device
    const stoppageService = createStoppageService(deviceId)
    
    // Get unclassified stoppages - use legacy method for backward compatibility
    const unclassifiedStoppages = await stoppageService.getUnclassifiedStoppagesLegacy()

    // Transform database response to match frontend expectations
    const responseStoppages = unclassifiedStoppages.map(stoppage => ({
      eventId: stoppage.event_id,
      deviceId: stoppage.device_id,
      jobId: stoppage.job_id,
      startTime: new Date(stoppage.start_time).toISOString(),
      endTime: stoppage.end_time ? new Date(stoppage.end_time).toISOString() : null,
      durationMinutes: stoppage.duration_minutes || null,
      category: stoppage.category,
      subCategory: stoppage.sub_category,
      comments: stoppage.comments,
      status: stoppage.status,
      classifiedAt: stoppage.classified_at ? new Date(stoppage.classified_at).toISOString() : null,
      operatorId: stoppage.operator_id
    }))

    return NextResponse.json(responseStoppages)
  } catch (error) {
    console.error("Error getting unclassified stoppages:", error)
    
    // Handle validation errors specifically
    if (error instanceof Error && error.message.includes('validation error')) {
      return NextResponse.json({ error: error.message }, { status: 400 })
    }
    
    return NextResponse.json({ error: "Internal server error" }, { status: 500 })
  }
}