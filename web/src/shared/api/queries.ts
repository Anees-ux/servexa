import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './apiClient';
import type {
  AccountDto,
  AssetDto,
  ChangeAssetStatusRequest,
  CreateAccountRequest,
  CreateAssetRequest,
  CreateEquipmentModelRequest,
  CreateSiteRequest,
  CreateWorkOrderRequest,
  EquipmentModelDto,
  PagedResult,
  SiteDto,
  TransitionWorkOrderStatusRequest,
  WorkOrderDto,
} from './types';

// Query Keys
export const accountKeys = {
  all: ['accounts'] as const,
  lists: () => [...accountKeys.all, 'list'] as const,
  list: (params: { search?: string; pageNumber?: number; pageSize?: number }) =>
    [...accountKeys.lists(), params] as const,
  detail: (id: string) => [...accountKeys.all, 'detail', id] as const,
};

export const siteKeys = {
  all: ['sites'] as const,
  lists: () => [...siteKeys.all, 'list'] as const,
  list: (params: { branchId?: string; accountId?: string; search?: string; pageNumber?: number; pageSize?: number }) =>
    [...siteKeys.lists(), params] as const,
  detail: (id: string) => [...siteKeys.all, 'detail', id] as const,
};

export const equipmentModelKeys = {
  all: ['equipment-models'] as const,
  lists: () => [...equipmentModelKeys.all, 'list'] as const,
  list: (params: { search?: string; pageNumber?: number; pageSize?: number }) =>
    [...equipmentModelKeys.lists(), params] as const,
};

export const assetKeys = {
  all: ['assets'] as const,
  lists: () => [...assetKeys.all, 'list'] as const,
  list: (params: { siteId?: string; ownerAccountId?: string; equipmentModelId?: string; search?: string; pageNumber?: number; pageSize?: number }) =>
    [...assetKeys.lists(), params] as const,
  detail: (id: string) => [...assetKeys.all, 'detail', id] as const,
};

export const workOrderKeys = {
  all: ['work-orders'] as const,
  lists: () => [...workOrderKeys.all, 'list'] as const,
  list: (params: { serviceAccountId?: string; primarySiteId?: string; operationalStatus?: number; priority?: number; search?: string; pageNumber?: number; pageSize?: number }) =>
    [...workOrderKeys.lists(), params] as const,
  detail: (id: string) => [...workOrderKeys.all, 'detail', id] as const,
};

// Customer Accounts
export function useAccountsQuery(search?: string, pageNumber = 1, pageSize = 20) {
  return useQuery<PagedResult<AccountDto>, Error>({
    queryKey: accountKeys.list({ search, pageNumber, pageSize }),
    queryFn: () => api.getAccounts(search, pageNumber, pageSize),
  });
}

export function useCreateAccountMutation() {
  const queryClient = useQueryClient();

  return useMutation<AccountDto, Error, CreateAccountRequest>({
    mutationFn: (request: CreateAccountRequest) => api.createAccount(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: accountKeys.all });
    },
  });
}

// Sites
export function useSitesQuery(
  branchId?: string,
  accountId?: string,
  search?: string,
  pageNumber = 1,
  pageSize = 20
) {
  return useQuery<PagedResult<SiteDto>, Error>({
    queryKey: siteKeys.list({ branchId, accountId, search, pageNumber, pageSize }),
    queryFn: () => api.getSites(branchId, accountId, search, pageNumber, pageSize),
  });
}

export function useCreateSiteMutation() {
  const queryClient = useQueryClient();

  return useMutation<SiteDto, Error, CreateSiteRequest>({
    mutationFn: (request: CreateSiteRequest) => api.createSite(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: siteKeys.all });
    },
  });
}

// Equipment Models
export function useEquipmentModelsQuery(search?: string, pageNumber = 1, pageSize = 50) {
  return useQuery<PagedResult<EquipmentModelDto>, Error>({
    queryKey: equipmentModelKeys.list({ search, pageNumber, pageSize }),
    queryFn: () => api.getEquipmentModels(search, pageNumber, pageSize),
  });
}

export function useCreateEquipmentModelMutation() {
  const queryClient = useQueryClient();

  return useMutation<EquipmentModelDto, Error, CreateEquipmentModelRequest>({
    mutationFn: (request: CreateEquipmentModelRequest) => api.createEquipmentModel(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: equipmentModelKeys.all });
    },
  });
}

// Asset Registry
export function useAssetsQuery(
  siteId?: string,
  ownerAccountId?: string,
  equipmentModelId?: string,
  search?: string,
  pageNumber = 1,
  pageSize = 20
) {
  return useQuery<PagedResult<AssetDto>, Error>({
    queryKey: assetKeys.list({ siteId, ownerAccountId, equipmentModelId, search, pageNumber, pageSize }),
    queryFn: () => api.getAssets(siteId, ownerAccountId, equipmentModelId, search, pageNumber, pageSize),
  });
}

export function useAssetByIdQuery(id?: string) {
  return useQuery<AssetDto, Error>({
    queryKey: id ? assetKeys.detail(id) : ['assets', 'detail', 'none'],
    queryFn: () => api.getAssetById(id!),
    enabled: Boolean(id),
  });
}

export function useCreateAssetMutation() {
  const queryClient = useQueryClient();

  return useMutation<AssetDto, Error, CreateAssetRequest>({
    mutationFn: (request: CreateAssetRequest) => api.createAsset(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: assetKeys.all });
    },
  });
}

export function useChangeAssetStatusMutation() {
  const queryClient = useQueryClient();

  return useMutation<AssetDto, Error, { id: string; request: ChangeAssetStatusRequest }>({
    mutationFn: ({ id, request }) => api.changeAssetStatus(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: assetKeys.all });
      queryClient.invalidateQueries({ queryKey: assetKeys.detail(data.id) });
    },
  });
}

// Work Orders
export function useWorkOrdersQuery(
  serviceAccountId?: string,
  primarySiteId?: string,
  operationalStatus?: number,
  priority?: number,
  search?: string,
  pageNumber = 1,
  pageSize = 20
) {
  return useQuery<PagedResult<WorkOrderDto>, Error>({
    queryKey: workOrderKeys.list({ serviceAccountId, primarySiteId, operationalStatus, priority, search, pageNumber, pageSize }),
    queryFn: () => api.getWorkOrders(serviceAccountId, primarySiteId, operationalStatus, priority, search, pageNumber, pageSize),
  });
}

export function useWorkOrderByIdQuery(id?: string) {
  return useQuery<WorkOrderDto, Error>({
    queryKey: id ? workOrderKeys.detail(id) : ['work-orders', 'detail', 'none'],
    queryFn: () => api.getWorkOrderById(id!),
    enabled: Boolean(id),
  });
}

export function useCreateWorkOrderMutation() {
  const queryClient = useQueryClient();

  return useMutation<WorkOrderDto, Error, CreateWorkOrderRequest>({
    mutationFn: (request: CreateWorkOrderRequest) => api.createWorkOrder(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: workOrderKeys.all });
    },
  });
}

export function useTransitionWorkOrderStatusMutation() {
  const queryClient = useQueryClient();

  return useMutation<WorkOrderDto, Error, { id: string; request: TransitionWorkOrderStatusRequest }>({
    mutationFn: ({ id, request }) => api.transitionWorkOrderStatus(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: workOrderKeys.all });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.detail(data.id) });
    },
  });
}
