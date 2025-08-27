import { apiClient } from '@/lib/api/client'
import type {
  HierarchyNode,
  HierarchyLevel,
  ApiResponse,
  DeviceConfig
} from '@/types'

/**
 * Hierarchy Management Service
 * Handles ISA-95 hierarchy operations including tree management,
 * equipment assignments, and organizational structure
 */
class HierarchyService {
  private readonly basePath = '/api/scheduling/hierarchy'

  /**
   * Get complete hierarchy tree
   */
  async getHierarchyTree(): Promise<ApiResponse<HierarchyNode[]>> {
    return apiClient.get<HierarchyNode[]>(`${this.basePath}/tree`, {}, 'scheduling')
  }

  /**
   * Get hierarchy node by ID with full details
   */
  async getNode(id: string): Promise<ApiResponse<HierarchyNode>> {
    return apiClient.get<HierarchyNode>(`${this.basePath}/nodes/${id}`, {}, 'scheduling')
  }

  /**
   * Get nodes by level (Enterprise, Site, Area, Line, Equipment)
   */
  async getNodesByLevel(level: HierarchyLevel): Promise<ApiResponse<HierarchyNode[]>> {
    return apiClient.get<HierarchyNode[]>(`${this.basePath}/nodes?level=${level}`, {}, 'scheduling')
  }

  /**
   * Get child nodes for a specific parent
   */
  async getChildNodes(parentId: string): Promise<ApiResponse<HierarchyNode[]>> {
    return apiClient.get<HierarchyNode[]>(`${this.basePath}/nodes/${parentId}/children`, {}, 'scheduling')
  }

  /**
   * Create new hierarchy node
   */
  async createNode(nodeData: {
    name: string
    type: HierarchyLevel
    parentId?: string
    properties?: Record<string, any>
    description?: string
  }): Promise<ApiResponse<HierarchyNode>> {
    return apiClient.post<HierarchyNode>(`${this.basePath}/nodes`, nodeData, {}, 'scheduling')
  }

  /**
   * Update existing hierarchy node
   */
  async updateNode(
    id: string,
    nodeData: Partial<{
      name: string
      properties: Record<string, any>
      description: string
    }>
  ): Promise<ApiResponse<HierarchyNode>> {
    return apiClient.put<HierarchyNode>(`${this.basePath}/nodes/${id}`, nodeData, {}, 'scheduling')
  }

  /**
   * Move node to different parent (drag-and-drop support)
   */
  async moveNode(
    nodeId: string,
    newParentId: string,
    position?: number
  ): Promise<ApiResponse<HierarchyNode>> {
    return apiClient.patch<HierarchyNode>(`${this.basePath}/nodes/${nodeId}/move`, {
      newParentId,
      position
    }, {}, 'scheduling')
  }

  /**
   * Delete hierarchy node (with cascade options)
   */
  async deleteNode(
    id: string,
    options: {
      cascade?: boolean // Delete all children
      reassignChildren?: string // Move children to this parent
    } = {}
  ): Promise<ApiResponse<void>> {
    const params = new URLSearchParams()
    if (options.cascade) params.append('cascade', 'true')
    if (options.reassignChildren) params.append('reassignTo', options.reassignChildren)

    return apiClient.delete<void>(`${this.basePath}/nodes/${id}?${params.toString()}`, {}, 'scheduling')
  }

  /**
   * Validate hierarchy structure integrity
   */
  async validateHierarchy(): Promise<ApiResponse<{
    valid: boolean
    issues: Array<{
      nodeId: string
      nodeName: string
      issue: string
      severity: 'error' | 'warning'
    }>
  }>> {
    return apiClient.get<any>(`${this.basePath}/validate`, {}, 'scheduling')
  }

  /**
   * Get hierarchy path for a specific node
   */
  async getNodePath(nodeId: string): Promise<ApiResponse<HierarchyNode[]>> {
    return apiClient.get<HierarchyNode[]>(`${this.basePath}/nodes/${nodeId}/path`, {}, 'scheduling')
  }

  /**
   * Search nodes by name or properties
   */
  async searchNodes(query: string, level?: HierarchyLevel): Promise<ApiResponse<HierarchyNode[]>> {
    const params = new URLSearchParams()
    params.append('q', query)
    if (level) params.append('level', level)

    return apiClient.get<HierarchyNode[]>(`${this.basePath}/search?${params.toString()}`, {}, 'scheduling')
  }

  /**
   * Get equipment assignments for a hierarchy node
   */
  async getEquipmentForNode(nodeId: string): Promise<ApiResponse<DeviceConfig[]>> {
    return apiClient.get<DeviceConfig[]>(`${this.basePath}/nodes/${nodeId}/equipment`, {}, 'scheduling')
  }

  /**
   * Assign equipment to hierarchy node
   */
  async assignEquipment(nodeId: string, deviceIds: string[]): Promise<ApiResponse<void>> {
    return apiClient.post<void>(`${this.basePath}/nodes/${nodeId}/equipment`, { deviceIds }, {}, 'scheduling')
  }

  /**
   * Remove equipment from hierarchy node
   */
  async removeEquipment(nodeId: string, deviceId: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`${this.basePath}/nodes/${nodeId}/equipment/${deviceId}`, {}, 'scheduling')
  }

  /**
   * Get hierarchy statistics
   */
  async getHierarchyStats(): Promise<ApiResponse<{
    totalNodes: number
    nodesByLevel: Record<HierarchyLevel, number>
    equipmentAssignments: number
    userAssignments: number
    orphanedNodes: number
  }>> {
    return apiClient.get<any>(`${this.basePath}/stats`, {}, 'scheduling')
  }

  /**
   * Export hierarchy structure
   */
  async exportHierarchy(format: 'json' | 'csv' | 'xlsx'): Promise<ApiResponse<{ downloadUrl: string }>> {
    return apiClient.post<{ downloadUrl: string }>(`${this.basePath}/export`, { format }, {}, 'scheduling')
  }

  /**
   * Import hierarchy structure from file
   */
  async importHierarchy(
    file: File,
    options: {
      mergeMode: 'replace' | 'merge' | 'update'
      validateOnly?: boolean
    }
  ): Promise<ApiResponse<{
    imported: number
    updated: number
    skipped: number
    errors: Array<{ line: number; message: string }>
  }>> {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('options', JSON.stringify(options))

    return apiClient.post<any>(`${this.basePath}/import`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    }, 'scheduling')
  }

  /**
   * Get hierarchy templates for quick setup
   */
  async getHierarchyTemplates(): Promise<ApiResponse<Array<{
    id: string
    name: string
    description: string
    structure: HierarchyNode[]
  }>>> {
    return apiClient.get<any>(`${this.basePath}/templates`, {}, 'scheduling')
  }

  /**
   * Apply hierarchy template
   */
  async applyTemplate(
    templateId: string,
    options: {
      parentNodeId?: string
      namePrefix?: string
    } = {}
  ): Promise<ApiResponse<HierarchyNode[]>> {
    return apiClient.post<HierarchyNode[]>(`${this.basePath}/templates/${templateId}/apply`, options, {}, 'scheduling')
  }

  /**
   * Bulk update nodes
   */
  async bulkUpdateNodes(
    nodeIds: string[],
    updates: {
      properties?: Record<string, any>
      parentId?: string
    }
  ): Promise<ApiResponse<{
    updated: number
    failed: Array<{ nodeId: string; error: string }>
  }>> {
    return apiClient.patch<any>(`${this.basePath}/nodes/bulk-update`, {
      nodeIds,
      updates
    }, {}, 'scheduling')
  }

  /**
   * Get node permissions for current user
   */
  async getNodePermissions(nodeId: string): Promise<ApiResponse<{
    canView: boolean
    canEdit: boolean
    canDelete: boolean
    canAddChildren: boolean
    canAssignEquipment: boolean
    canAssignUsers: boolean
  }>> {
    return apiClient.get<any>(`${this.basePath}/nodes/${nodeId}/permissions`, {}, 'scheduling')
  }
}

// Export singleton instance
export const hierarchyService = new HierarchyService()

// Export the class for testing
export { HierarchyService }