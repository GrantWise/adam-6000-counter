import { NextResponse } from "next/server"
import { getAlertingService, AlertSeverity } from "@/lib/services/alertingService"

/**
 * Alerts API Endpoint for Phase 4
 * Provides access to system alerts and alerting functionality
 */

// GET /api/alerts - Get active alerts and alert statistics
export async function GET(request: Request) {
  try {
    const { searchParams } = new URL(request.url)
    const severity = searchParams.get('severity') as AlertSeverity | null
    const includeHistory = searchParams.get('includeHistory') === 'true'
    const historyLimit = Number(searchParams.get('historyLimit')) || 50
    
    const alerting = getAlertingService()
    
    // Get active alerts (filtered by severity if specified)
    const activeAlerts = severity 
      ? alerting.getAlertsBySeverity(severity)
      : alerting.getActiveAlerts()
    
    // Get alert statistics
    const stats = alerting.getAlertStats()
    
    const response: any = {
      timestamp: new Date().toISOString(),
      active: activeAlerts,
      statistics: stats,
      hasCritical: alerting.hasCriticalAlerts()
    }
    
    // Include history if requested
    if (includeHistory) {
      response.history = alerting.getAlertHistory(historyLimit)
    }
    
    return NextResponse.json(response)
  } catch (error) {
    console.error("Alerts API error:", error)
    
    const errorResponse = {
      timestamp: new Date().toISOString(),
      error: "Failed to retrieve alerts",
      details: error instanceof Error ? error.message : 'Unknown error'
    }
    
    return NextResponse.json(errorResponse, { status: 500 })
  }
}

// POST /api/alerts - Fire a custom alert or resolve an alert
export async function POST(request: Request) {
  try {
    const body = await request.json()
    const alerting = getAlertingService()
    
    if (body.action === 'fire') {
      // Fire a custom alert
      const { id, severity, title, message, metadata } = body
      
      if (!id || !severity || !title || !message) {
        return NextResponse.json(
          { error: "Missing required fields: id, severity, title, message" },
          { status: 400 }
        )
      }
      
      if (!Object.values(AlertSeverity).includes(severity)) {
        return NextResponse.json(
          { error: "Invalid severity. Must be one of: info, warning, error, critical" },
          { status: 400 }
        )
      }
      
      alerting.fireCustomAlert(id, severity, title, message, 'api', metadata)
      
      return NextResponse.json({
        message: "Alert fired successfully",
        alert: { id, severity, title, message },
        timestamp: new Date().toISOString()
      })
      
    } else if (body.action === 'resolve') {
      // Resolve an alert
      const { alertId } = body
      
      if (!alertId) {
        return NextResponse.json(
          { error: "Missing required field: alertId" },
          { status: 400 }
        )
      }
      
      const resolved = alerting.manuallyResolveAlert(alertId)
      
      if (resolved) {
        return NextResponse.json({
          message: "Alert resolved successfully",
          alertId,
          timestamp: new Date().toISOString()
        })
      } else {
        return NextResponse.json(
          { error: "Alert not found or already resolved" },
          { status: 404 }
        )
      }
      
    } else {
      return NextResponse.json(
        { error: "Invalid action. Must be 'fire' or 'resolve'" },
        { status: 400 }
      )
    }
  } catch (error) {
    console.error("Alerts POST API error:", error)
    return NextResponse.json(
      { error: "Failed to process alert request" },
      { status: 500 }
    )
  }
}

// DELETE /api/alerts - Clear resolved alerts from history
export async function DELETE() {
  try {
    const alerting = getAlertingService()
    alerting.clearResolvedAlerts()
    
    return NextResponse.json({
      message: "Resolved alerts cleared from history",
      timestamp: new Date().toISOString()
    })
  } catch (error) {
    console.error("Alerts DELETE API error:", error)
    return NextResponse.json(
      { error: "Failed to clear resolved alerts" },
      { status: 500 }
    )
  }
}