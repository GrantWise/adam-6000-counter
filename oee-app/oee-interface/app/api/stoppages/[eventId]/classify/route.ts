import { type NextRequest, NextResponse } from "next/server"
import { createStoppageService } from "@/lib/services/stoppageService"
import { StoppageClassificationRequest } from "@/lib/types/oee"
import { classifyStoppageUseCase } from "@/lib/usecases/ClassifyStoppageUseCase"
import { withPermission } from "@/lib/auth/middleware"

// PUT /api/stoppages/{eventId}/classify - Classify stoppage using use case (requires operate permission)
export const PUT = withPermission(async (request: NextRequest, user) => {
  try {
    // Extract eventId from URL path
    const url = new URL(request.url)
    const pathParts = url.pathname.split('/')
    const eventIdIndex = pathParts.indexOf('stoppages') + 1
    const eventId = pathParts[eventIdIndex]
    const body = await request.json()
    const { category, subCategory, comments, deviceId } = body

    if (!eventId || isNaN(Number(eventId))) {
      return NextResponse.json({ error: "Valid Event ID is required" }, { status: 400 })
    }

    if (!category || !subCategory) {
      return NextResponse.json({ error: "Category and sub-category are required" }, { status: 400 })
    }

    if (!deviceId) {
      return NextResponse.json({ error: "Device ID is required" }, { status: 400 })
    }

    const eventIdNum = Number(eventId)

    // Use ClassifyStoppageUseCase for business logic
    const result = await classifyStoppageUseCase.execute({
      deviceId,
      eventId: eventIdNum,
      classification: {
        category: category.trim(),
        subCategory: subCategory.trim(),
        comments: comments?.trim()
      },
      operatorId: user.username || 'system'
    })

    if (!result.isSuccess) {
      return NextResponse.json({ error: result.errorMessage }, { status: 400 })
    }

    const response = {
      eventId: result.value.event_id,
      category: result.value.category,
      subCategory: result.value.sub_category,
      comments: result.value.comments,
      classifiedAt: result.value.classified_at,
      operatorId: result.value.classified_by,
      status: "classified",
    }

    return NextResponse.json(response)
  } catch (error) {
    console.error("Error classifying stoppage:", error)
    return NextResponse.json({ error: "Internal server error" }, { status: 500 })
  }
}, 'operate')