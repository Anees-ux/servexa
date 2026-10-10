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
  BookingDto,
  CreateBookingRequest,
  RescheduleBookingRequest,
  CancelBookingRequest,
  AssignResourceRequest,
  ResourceDto,
  ResourceScheduleDto,
  AssignedJobDto,
  ExecutionSessionDto,
  PauseWorkRequest,
  CompleteExecutionRequest,
  WorkOrderCompletionReadinessDto,
  WorkOrderCompletionEvaluationDto,
  CompleteWorkOrderRequest,
  AddScopeItemRequest,
  UpdateScopeItemStatusRequest,
  WorkOrderScopeItemDto,
  WorkTaskDto,
  CreateWorkTaskRequest,
  UpdateWorkTaskStatusRequest,
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
  completionEvaluation: (id: string) => [...workOrderKeys.all, 'completion-evaluation', id] as const,
  completionHistory: (id: string) => [...workOrderKeys.all, 'completion-history', id] as const,
  tasks: (id: string, params?: { assignmentId?: string; assetId?: string }) => [...workOrderKeys.all, 'tasks', id, params] as const,
};

export const bookingKeys = {
  all: ['bookings'] as const,
  lists: () => [...bookingKeys.all, 'list'] as const,
  list: (params: { workOrderId?: string; siteId?: string; status?: number; dispatchStatus?: number; fromUtc?: string; toUtc?: string; pageNumber?: number; pageSize?: number }) =>
    [...bookingKeys.lists(), params] as const,
  detail: (id: string) => [...bookingKeys.all, 'detail', id] as const,
};

export const resourceKeys = {
  all: ['resources'] as const,
  lists: () => [...resourceKeys.all, 'list'] as const,
  list: (params: { status?: number; resourceType?: number; branchId?: string; pageNumber?: number; pageSize?: number }) =>
    [...resourceKeys.lists(), params] as const,
  schedule: (id: string, startUtc: string, endUtc: string) =>
    [...resourceKeys.all, 'schedule', id, startUtc, endUtc] as const,
};

export const fieldKeys = {
  all: ['field'] as const,
  myJobs: (assignmentStatus?: number) => [...fieldKeys.all, 'my-jobs', assignmentStatus] as const,
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

export function useWorkOrderCompletionEvaluationQuery(id?: string, enabled = true) {
  return useQuery<WorkOrderCompletionReadinessDto, Error>({
    queryKey: id ? workOrderKeys.completionEvaluation(id) : ['work-orders', 'completion-evaluation', 'none'],
    queryFn: () => api.getWorkOrderCompletionEvaluation(id!),
    enabled: Boolean(id) && enabled,
  });
}

export function useWorkOrderCompletionHistoryQuery(id?: string, enabled = true) {
  return useQuery<WorkOrderCompletionEvaluationDto[], Error>({
    queryKey: id ? workOrderKeys.completionHistory(id) : ['work-orders', 'completion-history', 'none'],
    queryFn: () => api.getWorkOrderCompletionHistory(id!),
    enabled: Boolean(id) && enabled,
  });
}

export function useCompleteWorkOrderMutation() {
  const queryClient = useQueryClient();

  return useMutation<WorkOrderDto, Error, { id: string; request: CompleteWorkOrderRequest }>({
    mutationFn: ({ id, request }) => api.completeWorkOrder(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: workOrderKeys.all });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.detail(data.id) });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.completionEvaluation(data.id) });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.completionHistory(data.id) });
    },
  });
}

export function useAddScopeItemMutation() {
  const queryClient = useQueryClient();

  return useMutation<WorkOrderScopeItemDto, Error, { id: string; request: AddScopeItemRequest }>({
    mutationFn: ({ id, request }) => api.addWorkOrderScopeItem(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: workOrderKeys.all });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.detail(data.workOrderId) });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.completionEvaluation(data.workOrderId) });
    },
  });
}

export function useUpdateScopeItemStatusMutation() {
  const queryClient = useQueryClient();

  return useMutation<WorkOrderScopeItemDto, Error, { id: string; itemId: string; request: UpdateScopeItemStatusRequest }>({
    mutationFn: ({ id, itemId, request }) => api.updateWorkOrderScopeItemStatus(id, itemId, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: workOrderKeys.all });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.detail(data.workOrderId) });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.completionEvaluation(data.workOrderId) });
    },
  });
}

// Work Tasks & Inspections Queries & Mutations
export function useWorkTasksQuery(
  workOrderId?: string,
  params?: { assignmentId?: string; assetId?: string },
  enabled = true
) {
  return useQuery<WorkTaskDto[], Error>({
    queryKey: workOrderId ? workOrderKeys.tasks(workOrderId, params) : ['work-orders', 'tasks', 'none'],
    queryFn: () => api.getWorkTasks(workOrderId!, params),
    enabled: Boolean(workOrderId) && enabled,
  });
}

export function useCreateWorkTaskMutation() {
  const queryClient = useQueryClient();

  return useMutation<WorkTaskDto, Error, { workOrderId: string; request: CreateWorkTaskRequest }>({
    mutationFn: ({ workOrderId, request }) => api.createWorkTask(workOrderId, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: workOrderKeys.tasks(data.workOrderId) });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.completionEvaluation(data.workOrderId) });
    },
  });
}

export function useUpdateWorkTaskStatusMutation() {
  const queryClient = useQueryClient();

  return useMutation<WorkTaskDto, Error, { workOrderId: string; taskId: string; request: UpdateWorkTaskStatusRequest }>({
    mutationFn: ({ workOrderId, taskId, request }) => api.updateWorkTaskStatus(workOrderId, taskId, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: workOrderKeys.tasks(data.workOrderId) });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.completionEvaluation(data.workOrderId) });
    },
  });
}

// Scheduling & Bookings Queries & Mutations
export function useBookingsQuery(
  workOrderId?: string,
  siteId?: string,
  status?: number,
  dispatchStatus?: number,
  fromUtc?: string,
  toUtc?: string,
  pageNumber = 1,
  pageSize = 20
) {
  return useQuery<PagedResult<BookingDto>, Error>({
    queryKey: bookingKeys.list({ workOrderId, siteId, status, dispatchStatus, fromUtc, toUtc, pageNumber, pageSize }),
    queryFn: () => api.getBookings(workOrderId, siteId, status, dispatchStatus, fromUtc, toUtc, pageNumber, pageSize),
  });
}

export function useBookingByIdQuery(id?: string) {
  return useQuery<BookingDto, Error>({
    queryKey: id ? bookingKeys.detail(id) : ['bookings', 'detail', 'none'],
    queryFn: () => api.getBookingById(id!),
    enabled: Boolean(id),
  });
}

export function useCreateBookingMutation() {
  const queryClient = useQueryClient();

  return useMutation<BookingDto, Error, CreateBookingRequest>({
    mutationFn: (request: CreateBookingRequest) => api.createBooking(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.all });
    },
  });
}

export function useRescheduleBookingMutation() {
  const queryClient = useQueryClient();

  return useMutation<BookingDto, Error, { id: string; request: RescheduleBookingRequest }>({
    mutationFn: ({ id, request }) => api.rescheduleBooking(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
      queryClient.invalidateQueries({ queryKey: bookingKeys.detail(data.id) });
    },
  });
}

export function useCancelBookingMutation() {
  const queryClient = useQueryClient();

  return useMutation<BookingDto, Error, { id: string; request: CancelBookingRequest }>({
    mutationFn: ({ id, request }) => api.cancelBooking(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
      queryClient.invalidateQueries({ queryKey: bookingKeys.detail(data.id) });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.all });
    },
  });
}

export function useAssignResourceMutation() {
  const queryClient = useQueryClient();

  return useMutation<BookingDto, Error, { id: string; request: AssignResourceRequest }>({
    mutationFn: ({ id, request }) => api.assignResource(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
      queryClient.invalidateQueries({ queryKey: bookingKeys.detail(data.id) });
      queryClient.invalidateQueries({ queryKey: resourceKeys.all });
    },
  });
}

export function useUnassignResourceMutation() {
  const queryClient = useQueryClient();

  return useMutation<BookingDto, Error, { id: string; assignmentId: string; reason: string }>({
    mutationFn: ({ id, assignmentId, reason }) => api.unassignResource(id, assignmentId, reason),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
      queryClient.invalidateQueries({ queryKey: bookingKeys.detail(data.id) });
      queryClient.invalidateQueries({ queryKey: resourceKeys.all });
    },
  });
}

export function useDispatchBookingMutation() {
  const queryClient = useQueryClient();

  return useMutation<BookingDto, Error, string>({
    mutationFn: (id: string) => api.dispatchBooking(id),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
      queryClient.invalidateQueries({ queryKey: bookingKeys.detail(data.id) });
      queryClient.invalidateQueries({ queryKey: fieldKeys.all });
    },
  });
}

// Resources Queries
export function useResourcesQuery(
  status?: number,
  resourceType?: number,
  branchId?: string,
  pageNumber = 1,
  pageSize = 50
) {
  return useQuery<PagedResult<ResourceDto>, Error>({
    queryKey: resourceKeys.list({ status, resourceType, branchId, pageNumber, pageSize }),
    queryFn: () => api.getResources(status, resourceType, branchId, pageNumber, pageSize),
  });
}

export function useResourceScheduleQuery(id?: string, startUtc?: string, endUtc?: string) {
  return useQuery<ResourceScheduleDto, Error>({
    queryKey: id && startUtc && endUtc ? resourceKeys.schedule(id, startUtc, endUtc) : ['resources', 'schedule', 'none'],
    queryFn: () => api.getResourceSchedule(id!, startUtc!, endUtc!),
    enabled: Boolean(id && startUtc && endUtc),
  });
}

// Field Execution Queries & Mutations
export function useMyAssignedJobsQuery(assignmentStatus?: number) {
  return useQuery<AssignedJobDto[], Error>({
    queryKey: fieldKeys.myJobs(assignmentStatus),
    queryFn: () => api.getMyAssignedJobs(assignmentStatus),
  });
}

export function useStartTravelMutation() {
  const queryClient = useQueryClient();

  return useMutation<ExecutionSessionDto, Error, string>({
    mutationFn: (assignmentId: string) => api.startTechnicianTravel(assignmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fieldKeys.all });
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
    },
  });
}

export function useMarkArrivedMutation() {
  const queryClient = useQueryClient();

  return useMutation<ExecutionSessionDto, Error, string>({
    mutationFn: (assignmentId: string) => api.markTechnicianArrived(assignmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fieldKeys.all });
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
    },
  });
}

export function useStartWorkMutation() {
  const queryClient = useQueryClient();

  return useMutation<ExecutionSessionDto, Error, string>({
    mutationFn: (assignmentId: string) => api.startTechnicianWork(assignmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fieldKeys.all });
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.all });
    },
  });
}

export function usePauseWorkMutation() {
  const queryClient = useQueryClient();

  return useMutation<ExecutionSessionDto, Error, { assignmentId: string; request: PauseWorkRequest }>({
    mutationFn: ({ assignmentId, request }) => api.pauseTechnicianWork(assignmentId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fieldKeys.all });
    },
  });
}

export function useResumeWorkMutation() {
  const queryClient = useQueryClient();

  return useMutation<ExecutionSessionDto, Error, string>({
    mutationFn: (assignmentId: string) => api.resumeTechnicianWork(assignmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fieldKeys.all });
    },
  });
}

export function useCompleteExecutionMutation() {
  const queryClient = useQueryClient();

  return useMutation<ExecutionSessionDto, Error, { assignmentId: string; request: CompleteExecutionRequest }>({
    mutationFn: ({ assignmentId, request }) => api.completeTechnicianExecution(assignmentId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: fieldKeys.all });
      queryClient.invalidateQueries({ queryKey: bookingKeys.all });
      queryClient.invalidateQueries({ queryKey: workOrderKeys.all });
    },
  });
}
