import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { deviceApi } from '@/lib/api/devices';
import { AdamDeviceConfig } from '@/lib/types/api';
import { useToast } from '@/hooks/use-toast';

export const useDevices = () => {
  return useQuery({
    queryKey: ['devices'],
    queryFn: async () => {
      const response = await deviceApi.getAll();
      return response.data;
    },
    refetchInterval: 5000, // Refresh every 5 seconds
  });
};

export const useDevice = (deviceId: string) => {
  return useQuery({
    queryKey: ['devices', deviceId],
    queryFn: async () => {
      const response = await deviceApi.getById(deviceId);
      return response.data;
    },
    enabled: !!deviceId,
  });
};

export const useCreateDevice = () => {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  
  return useMutation({
    mutationFn: async (device: AdamDeviceConfig) => {
      const response = await deviceApi.create(device);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
      toast({
        title: 'Device created',
        description: 'The device has been created successfully.',
      });
    },
    onError: (error: any) => {
      toast({
        title: 'Error creating device',
        description: error.response?.data?.error || 'An error occurred while creating the device.',
        variant: 'destructive',
      });
    },
  });
};

export const useUpdateDevice = () => {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  
  return useMutation({
    mutationFn: async ({ id, device }: { id: string; device: AdamDeviceConfig }) => {
      const response = await deviceApi.update(id, device);
      return response.data;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
      queryClient.invalidateQueries({ queryKey: ['devices', variables.id] });
      toast({
        title: 'Device updated',
        description: 'The device has been updated successfully.',
      });
    },
    onError: (error: any) => {
      toast({
        title: 'Error updating device',
        description: error.response?.data?.error || 'An error occurred while updating the device.',
        variant: 'destructive',
      });
    },
  });
};

export const useDeleteDevice = () => {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  
  return useMutation({
    mutationFn: async (id: string) => {
      await deviceApi.delete(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
      toast({
        title: 'Device deleted',
        description: 'The device has been deleted successfully.',
      });
    },
    onError: (error: any) => {
      toast({
        title: 'Error deleting device',
        description: error.response?.data?.error || 'An error occurred while deleting the device.',
        variant: 'destructive',
      });
    },
  });
};

export const useTestDevice = () => {
  const { toast } = useToast();
  
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await deviceApi.test(id);
      return response.data;
    },
    onSuccess: (data) => {
      toast({
        title: data.success ? 'Connection successful' : 'Connection failed',
        description: data.message,
        variant: data.success ? 'default' : 'destructive',
      });
    },
    onError: (error: any) => {
      toast({
        title: 'Error testing device',
        description: error.response?.data?.error || 'An error occurred while testing the device.',
        variant: 'destructive',
      });
    },
  });
};

export const useEnableDevice = () => {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  
  return useMutation({
    mutationFn: async (id: string) => {
      await deviceApi.enable(id);
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
      queryClient.invalidateQueries({ queryKey: ['devices', id] });
      toast({
        title: 'Device enabled',
        description: 'The device has been enabled successfully.',
      });
    },
    onError: (error: any) => {
      toast({
        title: 'Error enabling device',
        description: error.response?.data?.error || 'An error occurred while enabling the device.',
        variant: 'destructive',
      });
    },
  });
};

export const useDisableDevice = () => {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  
  return useMutation({
    mutationFn: async (id: string) => {
      await deviceApi.disable(id);
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
      queryClient.invalidateQueries({ queryKey: ['devices', id] });
      toast({
        title: 'Device disabled',
        description: 'The device has been disabled successfully.',
      });
    },
    onError: (error: any) => {
      toast({
        title: 'Error disabling device',
        description: error.response?.data?.error || 'An error occurred while disabling the device.',
        variant: 'destructive',
      });
    },
  });
};