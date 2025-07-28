"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { Textarea } from "@/components/ui/textarea"
import { Copy, Save, RotateCcw } from "lucide-react"

interface Device {
  id: string
  name: string
  ipAddress: string
  port: number
  unitId: number
  status: "connected" | "warning" | "disconnected" | "error"
  lastContact: string
  enabled: boolean
}

interface Channel {
  number: number
  name: string
  description: string
  enabled: boolean
  startRegister: number
  registerCount: number
  dataType: "UInt16" | "UInt32" | "UInt64"
  scaleFactor: number
  offset: number
  minValue: number
  maxValue: number
  counterType: "incremental" | "absolute"
  overflowBehavior: "wrap" | "saturate" | "error"
  unit: string
}

interface ChannelConfigurationProps {
  device: Device
  onClose: () => void
}

const defaultChannel: Omit<Channel, "number"> = {
  name: "",
  description: "",
  enabled: false,
  startRegister: 0,
  registerCount: 2,
  dataType: "UInt32",
  scaleFactor: 1.0,
  offset: 0,
  minValue: 0,
  maxValue: 4294967295,
  counterType: "incremental",
  overflowBehavior: "wrap",
  unit: "count",
}

export function ChannelConfiguration({ device, onClose }: ChannelConfigurationProps) {
  const [channels, setChannels] = useState<Channel[]>(
    Array.from({ length: 16 }, (_, i) => ({
      number: i,
      ...defaultChannel,
      name: i < 3 ? ["Good_Products", "Rejects", "Total_Count"][i] : "",
      enabled: i < 3,
      startRegister: i * 2,
      description:
        i < 3
          ? ["Products that passed quality check", "Products that failed quality check", "Total products processed"][i]
          : "",
    })),
  )

  const [selectedChannel, setSelectedChannel] = useState<number | null>(null)

  const handleChannelChange = (channelNumber: number, field: keyof Channel, value: any) => {
    setChannels((prev) =>
      prev.map((channel) => (channel.number === channelNumber ? { ...channel, [field]: value } : channel)),
    )
  }

  const handleBulkEnable = (enabled: boolean) => {
    setChannels((prev) => prev.map((channel) => ({ ...channel, enabled })))
  }

  const handleCopyChannel = (sourceChannel: number, targetChannel: number) => {
    const source = channels[sourceChannel]
    if (source) {
      setChannels((prev) =>
        prev.map((channel) =>
          channel.number === targetChannel
            ? { ...source, number: targetChannel, name: `${source.name}_Copy` }
            : channel,
        ),
      )
    }
  }

  const handleSave = () => {
    console.log("Saving channel configuration:", channels)
    onClose()
  }

  return (
    <div className="space-y-6">
      {/* Channel Overview Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Channel Overview</CardTitle>
            <div className="flex items-center space-x-2">
              <Button variant="outline" size="sm" onClick={() => handleBulkEnable(true)}>
                Enable All
              </Button>
              <Button variant="outline" size="sm" onClick={() => handleBulkEnable(false)}>
                Disable All
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Ch</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Enabled</TableHead>
                <TableHead>Register</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Unit</TableHead>
                <TableHead>Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {channels.map((channel) => (
                <TableRow key={channel.number} className={selectedChannel === channel.number ? "bg-blue-50" : ""}>
                  <TableCell className="font-mono">{channel.number}</TableCell>
                  <TableCell>
                    <Input
                      value={channel.name}
                      onChange={(e) => handleChannelChange(channel.number, "name", e.target.value)}
                      placeholder={`Channel_${channel.number}`}
                      className="w-40"
                    />
                  </TableCell>
                  <TableCell>
                    <Switch
                      checked={channel.enabled}
                      onCheckedChange={(checked) => handleChannelChange(channel.number, "enabled", checked)}
                    />
                  </TableCell>
                  <TableCell>
                    <Input
                      type="number"
                      value={channel.startRegister}
                      onChange={(e) =>
                        handleChannelChange(channel.number, "startRegister", Number.parseInt(e.target.value))
                      }
                      className="w-20"
                    />
                  </TableCell>
                  <TableCell>
                    <Badge variant={channel.enabled ? "default" : "secondary"}>{channel.dataType}</Badge>
                  </TableCell>
                  <TableCell>
                    <Input
                      value={channel.unit}
                      onChange={(e) => handleChannelChange(channel.number, "unit", e.target.value)}
                      className="w-20"
                    />
                  </TableCell>
                  <TableCell>
                    <Button variant="outline" size="sm" onClick={() => setSelectedChannel(channel.number)}>
                      Configure
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Detailed Channel Configuration */}
      {selectedChannel !== null && (
        <Card>
          <CardHeader>
            <CardTitle>Channel {selectedChannel} Configuration</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Channel Name</Label>
                <Input
                  value={channels[selectedChannel].name}
                  onChange={(e) => handleChannelChange(selectedChannel, "name", e.target.value)}
                  placeholder="e.g., Good_Products"
                />
              </div>
              <div>
                <Label>Unit of Measurement</Label>
                <Input
                  value={channels[selectedChannel].unit}
                  onChange={(e) => handleChannelChange(selectedChannel, "unit", e.target.value)}
                  placeholder="e.g., bottles, cases, pieces"
                />
              </div>
            </div>

            <div>
              <Label>Description</Label>
              <Textarea
                value={channels[selectedChannel].description}
                onChange={(e) => handleChannelChange(selectedChannel, "description", e.target.value)}
                placeholder="Detailed explanation of what this counter measures"
                rows={2}
              />
            </div>

            <div className="grid grid-cols-3 gap-4">
              <div>
                <Label>Start Register</Label>
                <Input
                  type="number"
                  value={channels[selectedChannel].startRegister}
                  onChange={(e) =>
                    handleChannelChange(selectedChannel, "startRegister", Number.parseInt(e.target.value))
                  }
                />
              </div>
              <div>
                <Label>Register Count</Label>
                <Select
                  value={channels[selectedChannel].registerCount.toString()}
                  onValueChange={(value) =>
                    handleChannelChange(selectedChannel, "registerCount", Number.parseInt(value))
                  }
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1">1 (16-bit)</SelectItem>
                    <SelectItem value="2">2 (32-bit)</SelectItem>
                    <SelectItem value="4">4 (64-bit)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Data Type</Label>
                <Select
                  value={channels[selectedChannel].dataType}
                  onValueChange={(value) => handleChannelChange(selectedChannel, "dataType", value)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="UInt16">UInt16</SelectItem>
                    <SelectItem value="UInt32">UInt32</SelectItem>
                    <SelectItem value="UInt64">UInt64</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Scale Factor</Label>
                <Input
                  type="number"
                  step="0.001"
                  value={channels[selectedChannel].scaleFactor}
                  onChange={(e) =>
                    handleChannelChange(selectedChannel, "scaleFactor", Number.parseFloat(e.target.value))
                  }
                />
              </div>
              <div>
                <Label>Offset</Label>
                <Input
                  type="number"
                  value={channels[selectedChannel].offset}
                  onChange={(e) => handleChannelChange(selectedChannel, "offset", Number.parseInt(e.target.value))}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Min Value</Label>
                <Input
                  type="number"
                  value={channels[selectedChannel].minValue}
                  onChange={(e) => handleChannelChange(selectedChannel, "minValue", Number.parseInt(e.target.value))}
                />
              </div>
              <div>
                <Label>Max Value</Label>
                <Input
                  type="number"
                  value={channels[selectedChannel].maxValue}
                  onChange={(e) => handleChannelChange(selectedChannel, "maxValue", Number.parseInt(e.target.value))}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Counter Type</Label>
                <Select
                  value={channels[selectedChannel].counterType}
                  onValueChange={(value) => handleChannelChange(selectedChannel, "counterType", value)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="incremental">Incremental</SelectItem>
                    <SelectItem value="absolute">Absolute</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Overflow Behavior</Label>
                <Select
                  value={channels[selectedChannel].overflowBehavior}
                  onValueChange={(value) => handleChannelChange(selectedChannel, "overflowBehavior", value)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="wrap">Wrap</SelectItem>
                    <SelectItem value="saturate">Saturate</SelectItem>
                    <SelectItem value="error">Error</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="flex items-center space-x-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  const sourceChannel = selectedChannel
                  const targetChannel = (selectedChannel + 1) % 16
                  handleCopyChannel(sourceChannel, targetChannel)
                }}
              >
                <Copy className="w-4 h-4 mr-2" />
                Copy to Next Channel
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setChannels((prev) =>
                    prev.map((channel) =>
                      channel.number === selectedChannel ? { ...defaultChannel, number: selectedChannel } : channel,
                    ),
                  )
                }}
              >
                <RotateCcw className="w-4 h-4 mr-2" />
                Reset to Default
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Action Buttons */}
      <div className="flex justify-end space-x-2">
        <Button variant="outline" onClick={onClose}>
          Cancel
        </Button>
        <Button onClick={handleSave}>
          <Save className="w-4 h-4 mr-2" />
          Save Channel Configuration
        </Button>
      </div>
    </div>
  )
}
