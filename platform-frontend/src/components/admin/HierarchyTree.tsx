import React, { useState, useCallback } from 'react'
import {
  ChevronRight,
  ChevronDown,
  Building,
  Factory,
  MapPin,
  Zap,
  Settings,
  Plus,
  Edit,
  Trash2,
  MoreHorizontal,
  Users,
  Move
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import type { HierarchyNode, HierarchyLevel } from '@/types'
import { cn } from '@/lib/utils'

interface HierarchyTreeProps {
  nodes: HierarchyNode[]
  selectedNode?: string
  expandedNodes?: Set<string>
  onNodeSelect?: (node: HierarchyNode) => void
  onNodeExpand?: (nodeId: string, expanded: boolean) => void
  onNodeAdd?: (parentNode: HierarchyNode) => void
  onNodeEdit?: (node: HierarchyNode) => void
  onNodeDelete?: (node: HierarchyNode) => void
  onNodeMove?: (nodeId: string, newParentId: string) => void
  className?: string
  enableDragDrop?: boolean
  showActions?: boolean
}

interface DragState {
  draggedNode?: string
  dropTarget?: string
  dropPosition?: 'before' | 'after' | 'inside'
}

/**
 * Hierarchy Tree Component
 * Displays ISA-95 hierarchy with interactive tree view
 * Supports drag-and-drop, selection, and CRUD operations
 */
export const HierarchyTree: React.FC<HierarchyTreeProps> = ({
  nodes,
  selectedNode,
  expandedNodes = new Set(),
  onNodeSelect,
  onNodeExpand,
  onNodeAdd,
  onNodeEdit,
  onNodeDelete,
  onNodeMove,
  className,
  enableDragDrop = false,
  showActions = true
}) => {
  const [dragState, setDragState] = useState<DragState>({})

  // Get icon for hierarchy level
  const getNodeIcon = (type: HierarchyLevel) => {
    switch (type) {
      case 'Enterprise':
        return Building
      case 'Site':
        return Factory
      case 'Area':
        return MapPin
      case 'Line':
        return Zap
      case 'Equipment':
        return Settings
      default:
        return Building
    }
  }

  // Get color for hierarchy level
  const getNodeColor = (type: HierarchyLevel) => {
    switch (type) {
      case 'Enterprise':
        return 'text-blue-600'
      case 'Site':
        return 'text-green-600'
      case 'Area':
        return 'text-yellow-600'
      case 'Line':
        return 'text-orange-600'
      case 'Equipment':
        return 'text-purple-600'
      default:
        return 'text-gray-600'
    }
  }

  // Handle node expansion
  const handleNodeExpand = useCallback((nodeId: string, currentlyExpanded: boolean) => {
    onNodeExpand?.(nodeId, !currentlyExpanded)
  }, [onNodeExpand])

  // Handle drag start
  const handleDragStart = (e: React.DragEvent, node: HierarchyNode) => {
    if (!enableDragDrop) return
    
    e.dataTransfer.setData('text/plain', node.id)
    e.dataTransfer.effectAllowed = 'move'
    
    setDragState({ draggedNode: node.id })
  }

  // Handle drag over
  const handleDragOver = (e: React.DragEvent, targetNode: HierarchyNode) => {
    if (!enableDragDrop) return
    
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    
    setDragState(prev => ({
      ...prev,
      dropTarget: targetNode.id,
      dropPosition: 'inside'
    }))
  }

  // Handle drag leave
  const handleDragLeave = () => {
    setDragState(prev => ({
      ...prev,
      dropTarget: undefined,
      dropPosition: undefined
    }))
  }

  // Handle drop
  const handleDrop = (e: React.DragEvent, targetNode: HierarchyNode) => {
    if (!enableDragDrop) return
    
    e.preventDefault()
    const draggedNodeId = e.dataTransfer.getData('text/plain')
    
    if (draggedNodeId && draggedNodeId !== targetNode.id && onNodeMove) {
      onNodeMove(draggedNodeId, targetNode.id)
    }
    
    setDragState({})
  }

  // Render individual tree node
  const renderNode = (node: HierarchyNode, level: number = 0) => {
    const isExpanded = expandedNodes.has(node.id)
    const isSelected = selectedNode === node.id
    const hasChildren = node.children && node.children.length > 0
    const Icon = getNodeIcon(node.type)
    const isDragTarget = dragState.dropTarget === node.id
    const isDragged = dragState.draggedNode === node.id

    return (
      <div key={node.id} className="select-none">
        <div
          className={cn(
            'flex items-center gap-2 p-2 rounded-md cursor-pointer transition-all',
            'hover:bg-muted/50',
            isSelected && 'bg-primary/10 border border-primary/20',
            isDragTarget && 'bg-blue-100 border-2 border-blue-300',
            isDragged && 'opacity-50',
            level > 0 && `ml-${level * 4}`
          )}
          onClick={() => onNodeSelect?.(node)}
          draggable={enableDragDrop}
          onDragStart={(e) => handleDragStart(e, node)}
          onDragOver={(e) => handleDragOver(e, node)}
          onDragLeave={handleDragLeave}
          onDrop={(e) => handleDrop(e, node)}
        >
          {/* Expand/Collapse Button */}
          <div className="w-4 h-4 flex items-center justify-center">
            {hasChildren ? (
              <Button
                variant="ghost"
                size="sm"
                className="h-4 w-4 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  handleNodeExpand(node.id, isExpanded)
                }}
              >
                {isExpanded ? (
                  <ChevronDown className="h-3 w-3" />
                ) : (
                  <ChevronRight className="h-3 w-3" />
                )}
              </Button>
            ) : (
              <div className="w-4 h-4" />
            )}
          </div>

          {/* Node Icon */}
          <Icon className={cn('h-4 w-4', getNodeColor(node.type))} />

          {/* Node Name and Details */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <span className="font-medium text-sm truncate">{node.name}</span>
              <Badge variant="outline" className="text-xs">
                {node.type}
              </Badge>
            </div>
            
            {/* Additional Info */}
            <div className="flex items-center gap-3 mt-1 text-xs text-muted-foreground">
              {node.assignedUsers && node.assignedUsers.length > 0 && (
                <div className="flex items-center gap-1">
                  <Users className="h-3 w-3" />
                  <span>{node.assignedUsers.length}</span>
                </div>
              )}
              
              {node.deviceConfigs && node.deviceConfigs.length > 0 && (
                <div className="flex items-center gap-1">
                  <Settings className="h-3 w-3" />
                  <span>{node.deviceConfigs.length} devices</span>
                </div>
              )}
            </div>
          </div>

          {/* Actions */}
          {showActions && (
            <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  onNodeAdd?.(node)
                }}
                title="Add child node"
              >
                <Plus className="h-3 w-3" />
              </Button>
              
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  onNodeEdit?.(node)
                }}
                title="Edit node"
              >
                <Edit className="h-3 w-3" />
              </Button>
              
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0 text-destructive hover:text-destructive"
                onClick={(e) => {
                  e.stopPropagation()
                  onNodeDelete?.(node)
                }}
                title="Delete node"
              >
                <Trash2 className="h-3 w-3" />
              </Button>
              
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                title="More actions"
              >
                <MoreHorizontal className="h-3 w-3" />
              </Button>
            </div>
          )}
        </div>

        {/* Child Nodes */}
        {hasChildren && isExpanded && (
          <div className="ml-6 border-l border-muted pl-2 mt-1">
            {node.children.map((child) => renderNode(child, level + 1))}
          </div>
        )}
      </div>
    )
  }

  if (!nodes || nodes.length === 0) {
    return (
      <Card className={cn('p-8 text-center', className)}>
        <Building className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
        <h3 className="text-lg font-semibold mb-2">No Hierarchy Defined</h3>
        <p className="text-muted-foreground mb-4">
          Create your first hierarchy node to get started with the ISA-95 structure.
        </p>
        {onNodeAdd && (
          <Button onClick={() => onNodeAdd({} as HierarchyNode)}>
            <Plus className="h-4 w-4 mr-2" />
            Create Root Node
          </Button>
        )}
      </Card>
    )
  }

  return (
    <Card className={cn('p-4', className)}>
      <div className="space-y-1 group">
        {nodes.map((node) => renderNode(node))}
      </div>
      
      {/* Drag Drop Indicator */}
      {enableDragDrop && dragState.draggedNode && (
        <div className="fixed top-4 right-4 bg-background border rounded-md p-2 shadow-lg z-50">
          <div className="flex items-center gap-2 text-sm">
            <Move className="h-4 w-4" />
            <span>Moving node...</span>
          </div>
        </div>
      )}
    </Card>
  )
}

export default HierarchyTree