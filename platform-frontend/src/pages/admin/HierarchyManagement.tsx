import React, { useState, useEffect, useCallback } from 'react'
import {
  TreePine,
  Plus,
  Building,
  Factory,
  MapPin,
  Zap,
  Settings,
  Search,
  RefreshCw,
  Download,
  Upload,
  AlertTriangle,
  CheckCircle,
  Users,
  Trash2,
  Edit
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert } from '@/components/ui/alert'
import { LoadingSpinner } from '@/components/shared/LoadingSpinner'
import { HierarchyTree } from '@/components/admin/HierarchyTree'
import { hierarchyService } from '@/lib/services/hierarchyService'
import type {
  HierarchyNode,
  HierarchyLevel,
  ComponentStatus,
  DeviceConfig
} from '@/types'

interface HierarchyStats {
  totalNodes: number
  nodesByLevel: Record<HierarchyLevel, number>
  equipmentAssignments: number
  userAssignments: number
  orphanedNodes: number
}

interface ValidationIssue {
  nodeId: string
  nodeName: string
  issue: string
  severity: 'error' | 'warning'
}

/**
 * Hierarchy Management Page - Complete ISA-95 hierarchy management interface
 * Features: Tree view, drag-and-drop, equipment assignments, validation
 */
const HierarchyManagement: React.FC = () => {
  const [hierarchyTree, setHierarchyTree] = useState<HierarchyNode[]>([])
  const [stats, setStats] = useState<HierarchyStats | null>(null)
  const [loading, setLoading] = useState<ComponentStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  
  // Tree state
  const [selectedNode, setSelectedNode] = useState<string | undefined>()
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set())
  const [searchTerm, setSearchTerm] = useState('')
  const [filteredTree, setFilteredTree] = useState<HierarchyNode[]>([])
  
  // Selected node details
  const [nodeDetails, setNodeDetails] = useState<HierarchyNode | null>(null)
  const [nodeEquipment, setNodeEquipment] = useState<DeviceConfig[]>([])
  
  // Validation state
  const [validationIssues, setValidationIssues] = useState<ValidationIssue[]>([])
  const [showValidation, setShowValidation] = useState(false)

  // Load hierarchy tree
  const loadHierarchy = useCallback(async (showLoading = true) => {
    if (showLoading) setLoading('loading')
    setError(null)

    try {
      const response = await hierarchyService.getHierarchyTree()
      if (response.success && response.data) {
        setHierarchyTree(response.data)
        setLoading('success')
      } else {
        throw new Error(response.error?.message || 'Failed to load hierarchy')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unexpected error occurred')
      setLoading('error')
      
      // Set mock data for development
      const mockHierarchy: HierarchyNode[] = [
        {
          id: '1',
          name: 'Industrial ADAM Enterprise',
          type: 'Enterprise',
          children: [
            {
              id: '2',
              name: 'Main Production Site',
              type: 'Site',
              parentId: '1',
              children: [
                {
                  id: '3',
                  name: 'Assembly Area',
                  type: 'Area',
                  parentId: '2',
                  children: [
                    {
                      id: '4',
                      name: 'Line 1 - Primary Assembly',
                      type: 'Line',
                      parentId: '3',
                      children: [
                        {
                          id: '5',
                          name: 'Station A - Counter Module',
                          type: 'Equipment',
                          parentId: '4',
                          children: [],
                          properties: { model: 'ADAM-6051', ip: '192.168.1.100' },
                          assignedUsers: [],
                          deviceConfigs: [
                            {
                              id: 'dev1',
                              name: 'Counter Device 1',
                              ipAddress: '192.168.1.100',
                              port: 502,
                              deviceType: 'ADAM-6051',
                              channels: [],
                              status: 'online',
                              hierarchyNodeId: '5',
                              createdAt: new Date(),
                              updatedAt: new Date()
                            }
                          ],
                          path: ['Industrial ADAM Enterprise', 'Main Production Site', 'Assembly Area', 'Line 1 - Primary Assembly'],
                          level: 4
                        }
                      ],
                      properties: { capacity: 100, shift_pattern: '24/7' },
                      assignedUsers: [],
                      path: ['Industrial ADAM Enterprise', 'Main Production Site', 'Assembly Area'],
                      level: 3
                    }
                  ],
                  properties: { area_type: 'production', supervisor: 'John Smith' },
                  assignedUsers: [],
                  path: ['Industrial ADAM Enterprise', 'Main Production Site'],
                  level: 2
                },
                {
                  id: '6',
                  name: 'Quality Control Area',
                  type: 'Area',
                  parentId: '2',
                  children: [],
                  properties: { area_type: 'quality' },
                  assignedUsers: [],
                  path: ['Industrial ADAM Enterprise', 'Main Production Site'],
                  level: 2
                }
              ],
              properties: { location: 'Building A', manager: 'Sarah Johnson' },
              assignedUsers: [],
              path: ['Industrial ADAM Enterprise'],
              level: 1
            }
          ],
          properties: { company: 'Industrial ADAM Corp', established: '2024' },
          assignedUsers: [],
          path: [],
          level: 0
        }
      ]
      
      setHierarchyTree(mockHierarchy)
      // Expand all nodes by default for development
      const allNodeIds = new Set<string>()
      const collectIds = (nodes: HierarchyNode[]) => {
        nodes.forEach(node => {
          allNodeIds.add(node.id)
          if (node.children) {
            collectIds(node.children)
          }
        })
      }
      collectIds(mockHierarchy)
      setExpandedNodes(allNodeIds)
    }
  }, [])

  // Load hierarchy statistics
  const loadStats = useCallback(async () => {
    try {
      const response = await hierarchyService.getHierarchyStats()
      if (response.success && response.data) {
        setStats(response.data)
      } else {
        // Mock stats for development
        setStats({
          totalNodes: 6,
          nodesByLevel: {
            Enterprise: 1,
            Site: 1,
            Area: 2,
            Line: 1,
            Equipment: 1
          },
          equipmentAssignments: 1,
          userAssignments: 0,
          orphanedNodes: 0
        })
      }
    } catch (err) {
      console.error('Failed to load hierarchy stats:', err)
    }
  }, [])

  // Validate hierarchy
  const validateHierarchy = useCallback(async () => {
    setShowValidation(true)
    try {
      const response = await hierarchyService.validateHierarchy()
      if (response.success && response.data) {
        setValidationIssues(response.data.issues)
      } else {
        // Mock validation for development
        setValidationIssues([])
      }
    } catch (err) {
      console.error('Failed to validate hierarchy:', err)
      setValidationIssues([])
    }
  }, [])

  // Load data on component mount
  useEffect(() => {
    loadHierarchy()
    loadStats()
  }, [loadHierarchy, loadStats])

  // Filter tree based on search
  useEffect(() => {
    if (!searchTerm) {
      setFilteredTree(hierarchyTree)
      return
    }

    const filterNodes = (nodes: HierarchyNode[]): HierarchyNode[] => {
      return nodes.filter(node => {
        const matchesSearch = node.name.toLowerCase().includes(searchTerm.toLowerCase())
        const hasMatchingChildren = node.children && filterNodes(node.children).length > 0
        
        if (matchesSearch || hasMatchingChildren) {
          return {
            ...node,
            children: hasMatchingChildren ? filterNodes(node.children) : node.children
          }
        }
        
        return false
      }).filter(Boolean) as HierarchyNode[]
    }

    setFilteredTree(filterNodes(hierarchyTree))
  }, [searchTerm, hierarchyTree])

  // Handle node selection
  const handleNodeSelect = useCallback(async (node: HierarchyNode) => {
    setSelectedNode(node.id)
    setNodeDetails(node)
    
    // Load equipment for selected node
    try {
      const response = await hierarchyService.getEquipmentForNode(node.id)
      if (response.success && response.data) {
        setNodeEquipment(response.data)
      } else {
        setNodeEquipment(node.deviceConfigs || [])
      }
    } catch (err) {
      console.error('Failed to load node equipment:', err)
      setNodeEquipment(node.deviceConfigs || [])
    }
  }, [])

  // Handle node expansion
  const handleNodeExpand = useCallback((nodeId: string, expanded: boolean) => {
    setExpandedNodes(prev => {
      const newExpanded = new Set(prev)
      if (expanded) {
        newExpanded.add(nodeId)
      } else {
        newExpanded.delete(nodeId)
      }
      return newExpanded
    })
  }, [])

  // Handle node operations
  const handleNodeAdd = useCallback((parentNode: HierarchyNode) => {
    console.log('Add child node to:', parentNode.name)
    // TODO: Open add node modal
  }, [])

  const handleNodeEdit = useCallback((node: HierarchyNode) => {
    console.log('Edit node:', node.name)
    // TODO: Open edit node modal
  }, [])

  const handleNodeDelete = useCallback(async (node: HierarchyNode) => {
    if (!confirm(`Are you sure you want to delete "${node.name}" and all its children?`)) {
      return
    }
    
    try {
      const response = await hierarchyService.deleteNode(node.id, { cascade: true })
      if (response.success) {
        await loadHierarchy(false)
        await loadStats()
        setSelectedNode(undefined)
        setNodeDetails(null)
      } else {
        setError(response.error?.message || 'Failed to delete node')
      }
    } catch (err) {
      setError('An unexpected error occurred while deleting node')
    }
  }, [loadHierarchy, loadStats])

  const handleNodeMove = useCallback(async (nodeId: string, newParentId: string) => {
    try {
      const response = await hierarchyService.moveNode(nodeId, newParentId)
      if (response.success) {
        await loadHierarchy(false)
        await loadStats()
      } else {
        setError(response.error?.message || 'Failed to move node')
      }
    } catch (err) {
      setError('An unexpected error occurred while moving node')
    }
  }, [loadHierarchy, loadStats])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Hierarchy Management</h1>
          <p className="text-muted-foreground mt-2">
            Manage ISA-95 organizational hierarchy structure
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={validateHierarchy}>
            <CheckCircle className="h-4 w-4 mr-2" />
            Validate
          </Button>
          <Button size="lg" onClick={() => handleNodeAdd({} as HierarchyNode)}>
            <Plus className="h-5 w-5 mr-2" />
            Add Node
          </Button>
        </div>
      </div>

      {/* Statistics Cards */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Total Nodes</p>
                  <p className="text-2xl font-bold">{stats.totalNodes}</p>
                </div>
                <TreePine className="h-8 w-8 text-muted-foreground" />
              </div>
            </CardContent>
          </Card>
          
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Sites</p>
                  <p className="text-2xl font-bold">{stats.nodesByLevel.Site}</p>
                </div>
                <Factory className="h-8 w-8 text-green-600" />
              </div>
            </CardContent>
          </Card>
          
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Areas</p>
                  <p className="text-2xl font-bold">{stats.nodesByLevel.Area}</p>
                </div>
                <MapPin className="h-8 w-8 text-yellow-600" />
              </div>
            </CardContent>
          </Card>
          
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Lines</p>
                  <p className="text-2xl font-bold">{stats.nodesByLevel.Line}</p>
                </div>
                <Zap className="h-8 w-8 text-orange-600" />
              </div>
            </CardContent>
          </Card>
          
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Equipment</p>
                  <p className="text-2xl font-bold">{stats.nodesByLevel.Equipment}</p>
                </div>
                <Settings className="h-8 w-8 text-purple-600" />
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Validation Issues */}
      {showValidation && validationIssues.length > 0 && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <div>
            <div className="font-medium">Hierarchy Validation Issues Found</div>
            <ul className="mt-2 list-disc list-inside text-sm">
              {validationIssues.map((issue, index) => (
                <li key={index}>
                  <strong>{issue.nodeName}:</strong> {issue.issue}
                </li>
              ))}
            </ul>
          </div>
        </Alert>
      )}

      {/* Error Alert */}
      {error && (
        <Alert variant="destructive">
          <div>{error}</div>
        </Alert>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Hierarchy Tree */}
        <div className="lg:col-span-2 space-y-4">
          {/* Search and Controls */}
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center gap-4">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Search hierarchy nodes..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
                <Button variant="outline" size="sm" onClick={() => loadHierarchy()}>
                  <RefreshCw className="h-4 w-4 mr-2" />
                  Refresh
                </Button>
                <Button variant="outline" size="sm">
                  <Download className="h-4 w-4 mr-2" />
                  Export
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Tree View */}
          {loading === 'loading' ? (
            <Card>
              <CardContent className="p-8 text-center">
                <LoadingSpinner size="lg" />
                <div className="mt-4 text-muted-foreground">Loading hierarchy...</div>
              </CardContent>
            </Card>
          ) : (
            <HierarchyTree
              nodes={filteredTree}
              selectedNode={selectedNode}
              expandedNodes={expandedNodes}
              onNodeSelect={handleNodeSelect}
              onNodeExpand={handleNodeExpand}
              onNodeAdd={handleNodeAdd}
              onNodeEdit={handleNodeEdit}
              onNodeDelete={handleNodeDelete}
              onNodeMove={handleNodeMove}
              enableDragDrop={true}
              showActions={true}
            />
          )}
        </div>

        {/* Node Details Panel */}
        <div className="space-y-4">
          {nodeDetails ? (
            <>
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    {nodeDetails.type === 'Enterprise' && <Building className="h-5 w-5 text-blue-600" />}
                    {nodeDetails.type === 'Site' && <Factory className="h-5 w-5 text-green-600" />}
                    {nodeDetails.type === 'Area' && <MapPin className="h-5 w-5 text-yellow-600" />}
                    {nodeDetails.type === 'Line' && <Zap className="h-5 w-5 text-orange-600" />}
                    {nodeDetails.type === 'Equipment' && <Settings className="h-5 w-5 text-purple-600" />}
                    {nodeDetails.name}
                  </CardTitle>
                  <CardDescription>
                    {nodeDetails.type} Level Node
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <h4 className="font-medium mb-2">Properties</h4>
                    {nodeDetails.properties && Object.keys(nodeDetails.properties).length > 0 ? (
                      <div className="space-y-2">
                        {Object.entries(nodeDetails.properties).map(([key, value]) => (
                          <div key={key} className="flex justify-between text-sm">
                            <span className="text-muted-foreground">{key}:</span>
                            <span className="font-mono">{String(value)}</span>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <p className="text-muted-foreground text-sm">No properties defined</p>
                    )}
                  </div>
                  
                  <div>
                    <h4 className="font-medium mb-2">Path</h4>
                    <div className="text-sm text-muted-foreground">
                      {nodeDetails.path.join(' → ')}
                      {nodeDetails.path.length > 0 && ' → '}
                      <strong>{nodeDetails.name}</strong>
                    </div>
                  </div>
                  
                  <div className="flex justify-between pt-2 border-t">
                    <Button variant="outline" size="sm" onClick={() => handleNodeEdit(nodeDetails)}>
                      <Edit className="h-4 w-4 mr-2" />
                      Edit
                    </Button>
                    <Button 
                      variant="outline" 
                      size="sm" 
                      className="text-destructive"
                      onClick={() => handleNodeDelete(nodeDetails)}
                    >
                      <Trash2 className="h-4 w-4 mr-2" />
                      Delete
                    </Button>
                  </div>
                </CardContent>
              </Card>
              
              {/* Equipment Assignments */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Settings className="h-5 w-5" />
                    Equipment ({nodeEquipment.length})
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {nodeEquipment.length > 0 ? (
                    <div className="space-y-2">
                      {nodeEquipment.map((device) => (
                        <div key={device.id} className="flex items-center justify-between p-2 border rounded-md">
                          <div>
                            <div className="font-medium text-sm">{device.name}</div>
                            <div className="text-xs text-muted-foreground">
                              {device.deviceType} - {device.ipAddress}:{device.port}
                            </div>
                          </div>
                          <Badge variant={device.status === 'online' ? 'default' : 'secondary'}>
                            {device.status}
                          </Badge>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-muted-foreground text-sm">No equipment assigned</p>
                  )}
                </CardContent>
              </Card>
            </>
          ) : (
            <Card>
              <CardContent className="p-8 text-center">
                <TreePine className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                <h3 className="font-semibold mb-2">Select a Node</h3>
                <p className="text-muted-foreground text-sm">
                  Click on a node in the tree to view its details and manage equipment assignments.
                </p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}

export default HierarchyManagement