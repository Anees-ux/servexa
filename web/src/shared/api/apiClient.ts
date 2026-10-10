import type {
  AccountDto,
  AssetDto,
  AuthResponseDto,
  ChangeAssetStatusRequest,
  CreateAccountRequest,
  CreateAssetRequest,
  CreateEquipmentModelRequest,
  CreateSiteRequest,
  CreateWorkOrderRequest,
  CurrentUserDto,
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

export interface ProblemDetailsResponse {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  public status: number;
  public problem?: ProblemDetailsResponse;

  constructor(status: number, message: string, problem?: ProblemDetailsResponse) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

class ApiClient {
  private baseUrl = '/api/v1';
  private token: string | null = null;

  constructor() {
    this.token = typeof window !== 'undefined' ? localStorage.getItem('servexa_access_token') : null;
  }

  public setToken(token: string | null) {
    this.token = token;
    if (typeof window !== 'undefined') {
      if (token) {
        localStorage.setItem('servexa_access_token', token);
      } else {
        localStorage.removeItem('servexa_access_token');
      }
    }
  }

  public getToken(): string | null {
    return this.token;
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      ...(options.headers as Record<string, string>),
    };

    if (this.token) {
      headers.Authorization = `Bearer ${this.token}`;
    }

    let response: Response;
    try {
      response = await fetch(url, {
        ...options,
        headers,
      });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Network request failed';
      throw new ApiError(0, `Unable to reach Servexa API at ${url}. Please ensure the backend is running. (${message})`);
    }

    if (!response.ok) {
      let problem: ProblemDetailsResponse | undefined;
      let errorDetail = response.statusText;
      try {
        problem = await response.json();
        if (problem) {
          errorDetail = problem.detail || problem.title || JSON.stringify(problem);
        }
      } catch {
        // fallback to status text
      }
      throw new ApiError(response.status, `API Error (${response.status}): ${errorDetail}`, problem);
    }

    if (response.status === 204) {
      return {} as T;
    }

    return response.json();
  }

  /**
   * Development-only session login. Authenticates against the verified seed identity in the backend.
   */
  public async loginDevelopmentSession(userType: 'admin' | 'dispatcher' = 'admin'): Promise<AuthResponseDto> {
    const res = await this.request<AuthResponseDto>('/dev/token', {
      method: 'POST',
      body: JSON.stringify({ userType }),
    });
    this.setToken(res.accessToken);
    return res;
  }

  public async getCurrentUser(): Promise<CurrentUserDto> {
    return this.request<CurrentUserDto>('/auth/me');
  }

  public async getAccounts(search?: string, pageNumber = 1, pageSize = 20): Promise<PagedResult<AccountDto>> {
    const params = new URLSearchParams();
    if (search) params.set('search', search);
    params.set('pageNumber', pageNumber.toString());
    params.set('pageSize', pageSize.toString());

    return this.request<PagedResult<AccountDto>>(`/accounts?${params.toString()}`);
  }

  public async createAccount(req: CreateAccountRequest): Promise<AccountDto> {
    return this.request<AccountDto>('/accounts', {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async getSites(
    branchId?: string,
    accountId?: string,
    search?: string,
    pageNumber = 1,
    pageSize = 20
  ): Promise<PagedResult<SiteDto>> {
    const params = new URLSearchParams();
    if (branchId) params.set('branchId', branchId);
    if (accountId) params.set('accountId', accountId);
    if (search) params.set('search', search);
    params.set('pageNumber', pageNumber.toString());
    params.set('pageSize', pageSize.toString());

    return this.request<PagedResult<SiteDto>>(`/sites?${params.toString()}`);
  }

  public async createSite(req: CreateSiteRequest): Promise<SiteDto> {
    return this.request<SiteDto>('/sites', {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  // Equipment Models API
  public async getEquipmentModels(search?: string, pageNumber = 1, pageSize = 20): Promise<PagedResult<EquipmentModelDto>> {
    const params = new URLSearchParams();
    if (search) params.set('search', search);
    params.set('pageNumber', pageNumber.toString());
    params.set('pageSize', pageSize.toString());

    return this.request<PagedResult<EquipmentModelDto>>(`/equipment-models?${params.toString()}`);
  }

  public async createEquipmentModel(req: CreateEquipmentModelRequest): Promise<EquipmentModelDto> {
    return this.request<EquipmentModelDto>('/equipment-models', {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  // Asset Registry API
  public async getAssets(
    siteId?: string,
    ownerAccountId?: string,
    equipmentModelId?: string,
    search?: string,
    pageNumber = 1,
    pageSize = 20
  ): Promise<PagedResult<AssetDto>> {
    const params = new URLSearchParams();
    if (siteId) params.set('siteId', siteId);
    if (ownerAccountId) params.set('ownerAccountId', ownerAccountId);
    if (equipmentModelId) params.set('equipmentModelId', equipmentModelId);
    if (search) params.set('search', search);
    params.set('pageNumber', pageNumber.toString());
    params.set('pageSize', pageSize.toString());

    return this.request<PagedResult<AssetDto>>(`/assets?${params.toString()}`);
  }

  public async getAssetById(id: string): Promise<AssetDto> {
    return this.request<AssetDto>(`/assets/${id}`);
  }

  public async createAsset(req: CreateAssetRequest): Promise<AssetDto> {
    return this.request<AssetDto>('/assets', {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async changeAssetStatus(id: string, req: ChangeAssetStatusRequest): Promise<AssetDto> {
    return this.request<AssetDto>(`/assets/${id}/status`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  // Work Orders API
  public async getWorkOrders(
    serviceAccountId?: string,
    primarySiteId?: string,
    operationalStatus?: number,
    priority?: number,
    search?: string,
    pageNumber = 1,
    pageSize = 20
  ): Promise<PagedResult<WorkOrderDto>> {
    const params = new URLSearchParams();
    if (serviceAccountId) params.set('serviceAccountId', serviceAccountId);
    if (primarySiteId) params.set('primarySiteId', primarySiteId);
    if (operationalStatus !== undefined && operationalStatus !== null) params.set('operationalStatus', operationalStatus.toString());
    if (priority !== undefined && priority !== null) params.set('priority', priority.toString());
    if (search) params.set('search', search);
    params.set('pageNumber', pageNumber.toString());
    params.set('pageSize', pageSize.toString());

    return this.request<PagedResult<WorkOrderDto>>(`/work-orders?${params.toString()}`);
  }

  public async getWorkOrderById(id: string): Promise<WorkOrderDto> {
    return this.request<WorkOrderDto>(`/work-orders/${id}`);
  }

  public async createWorkOrder(req: CreateWorkOrderRequest): Promise<WorkOrderDto> {
    return this.request<WorkOrderDto>('/work-orders', {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async transitionWorkOrderStatus(id: string, req: TransitionWorkOrderStatusRequest): Promise<WorkOrderDto> {
    return this.request<WorkOrderDto>(`/work-orders/${id}/transition`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async getWorkOrderCompletionEvaluation(id: string): Promise<WorkOrderCompletionReadinessDto> {
    return this.request<WorkOrderCompletionReadinessDto>(`/work-orders/${id}/completion-evaluation`);
  }

  public async getWorkOrderCompletionHistory(id: string): Promise<WorkOrderCompletionEvaluationDto[]> {
    return this.request<WorkOrderCompletionEvaluationDto[]>(`/work-orders/${id}/completion-evaluations`);
  }

  public async completeWorkOrder(id: string, req: CompleteWorkOrderRequest): Promise<WorkOrderDto> {
    return this.request<WorkOrderDto>(`/work-orders/${id}/complete`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async addWorkOrderScopeItem(id: string, req: AddScopeItemRequest): Promise<WorkOrderScopeItemDto> {
    return this.request<WorkOrderScopeItemDto>(`/work-orders/${id}/scope-items`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async updateWorkOrderScopeItemStatus(
    id: string,
    itemId: string,
    req: UpdateScopeItemStatusRequest
  ): Promise<WorkOrderScopeItemDto> {
    return this.request<WorkOrderScopeItemDto>(`/work-orders/${id}/scope-items/${itemId}/status`, {
      method: 'PATCH',
      body: JSON.stringify(req),
    });
  }

  // Work Tasks & Inspections API
  public async getWorkTasks(
    workOrderId: string,
    params?: { assignmentId?: string; assetId?: string }
  ): Promise<WorkTaskDto[]> {
    const query = new URLSearchParams();
    if (params?.assignmentId) query.set('assignmentId', params.assignmentId);
    if (params?.assetId) query.set('assetId', params.assetId);
    const qs = query.toString();
    return this.request<WorkTaskDto[]>(`/work-orders/${workOrderId}/tasks${qs ? `?${qs}` : ''}`);
  }

  public async createWorkTask(workOrderId: string, req: CreateWorkTaskRequest): Promise<WorkTaskDto> {
    return this.request<WorkTaskDto>(`/work-orders/${workOrderId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async updateWorkTaskStatus(
    workOrderId: string,
    taskId: string,
    req: UpdateWorkTaskStatusRequest
  ): Promise<WorkTaskDto> {
    return this.request<WorkTaskDto>(`/work-orders/${workOrderId}/tasks/${taskId}/status`, {
      method: 'PATCH',
      body: JSON.stringify(req),
    });
  }

  // Scheduling API
  public async getBookings(
    workOrderId?: string,
    siteId?: string,
    status?: number,
    dispatchStatus?: number,
    fromUtc?: string,
    toUtc?: string,
    pageNumber = 1,
    pageSize = 20
  ): Promise<PagedResult<BookingDto>> {
    const params = new URLSearchParams();
    if (workOrderId) params.set('workOrderId', workOrderId);
    if (siteId) params.set('siteId', siteId);
    if (status !== undefined && status !== null) params.set('status', status.toString());
    if (dispatchStatus !== undefined && dispatchStatus !== null) params.set('dispatchStatus', dispatchStatus.toString());
    if (fromUtc) params.set('fromUtc', fromUtc);
    if (toUtc) params.set('toUtc', toUtc);
    params.set('pageNumber', pageNumber.toString());
    params.set('pageSize', pageSize.toString());

    return this.request<PagedResult<BookingDto>>(`/bookings?${params.toString()}`);
  }

  public async getBookingById(id: string): Promise<BookingDto> {
    return this.request<BookingDto>(`/bookings/${id}`);
  }

  public async createBooking(req: CreateBookingRequest): Promise<BookingDto> {
    return this.request<BookingDto>('/bookings', {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async rescheduleBooking(id: string, req: RescheduleBookingRequest): Promise<BookingDto> {
    return this.request<BookingDto>(`/bookings/${id}/reschedule`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async cancelBooking(id: string, req: CancelBookingRequest): Promise<BookingDto> {
    return this.request<BookingDto>(`/bookings/${id}/cancel`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async assignResource(id: string, req: AssignResourceRequest): Promise<BookingDto> {
    return this.request<BookingDto>(`/bookings/${id}/assignments`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async unassignResource(id: string, assignmentId: string, reason: string): Promise<BookingDto> {
    const params = new URLSearchParams({ reason });
    return this.request<BookingDto>(`/bookings/${id}/assignments/${assignmentId}?${params.toString()}`, {
      method: 'DELETE',
    });
  }

  public async dispatchBooking(id: string): Promise<BookingDto> {
    return this.request<BookingDto>(`/bookings/${id}/dispatch`, {
      method: 'POST',
    });
  }

  // Resources API
  public async getResources(
    status?: number,
    resourceType?: number,
    branchId?: string,
    pageNumber = 1,
    pageSize = 50
  ): Promise<PagedResult<ResourceDto>> {
    const params = new URLSearchParams();
    if (status !== undefined && status !== null) params.set('status', status.toString());
    if (resourceType !== undefined && resourceType !== null) params.set('resourceType', resourceType.toString());
    if (branchId) params.set('branchId', branchId);
    params.set('pageNumber', pageNumber.toString());
    params.set('pageSize', pageSize.toString());

    return this.request<PagedResult<ResourceDto>>(`/resources?${params.toString()}`);
  }

  public async getResourceSchedule(id: string, startUtc: string, endUtc: string): Promise<ResourceScheduleDto> {
    const params = new URLSearchParams({ startUtc, endUtc });
    return this.request<ResourceScheduleDto>(`/resources/${id}/schedule?${params.toString()}`);
  }

  // Field Execution API
  public async getMyAssignedJobs(assignmentStatus?: number): Promise<AssignedJobDto[]> {
    const params = new URLSearchParams();
    if (assignmentStatus !== undefined && assignmentStatus !== null) {
      params.set('assignmentStatus', assignmentStatus.toString());
    }
    const q = params.toString() ? `?${params.toString()}` : '';
    return this.request<AssignedJobDto[]>(`/field/me/assignments${q}`);
  }

  public async startTechnicianTravel(assignmentId: string): Promise<ExecutionSessionDto> {
    return this.request<ExecutionSessionDto>(`/field/assignments/${assignmentId}/travel`, {
      method: 'POST',
    });
  }

  public async markTechnicianArrived(assignmentId: string): Promise<ExecutionSessionDto> {
    return this.request<ExecutionSessionDto>(`/field/assignments/${assignmentId}/arrive`, {
      method: 'POST',
    });
  }

  public async startTechnicianWork(assignmentId: string): Promise<ExecutionSessionDto> {
    return this.request<ExecutionSessionDto>(`/field/assignments/${assignmentId}/start`, {
      method: 'POST',
    });
  }

  public async pauseTechnicianWork(assignmentId: string, req: PauseWorkRequest): Promise<ExecutionSessionDto> {
    return this.request<ExecutionSessionDto>(`/field/assignments/${assignmentId}/pause`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }

  public async resumeTechnicianWork(assignmentId: string): Promise<ExecutionSessionDto> {
    return this.request<ExecutionSessionDto>(`/field/assignments/${assignmentId}/resume`, {
      method: 'POST',
    });
  }

  public async completeTechnicianExecution(assignmentId: string, req: CompleteExecutionRequest): Promise<ExecutionSessionDto> {
    return this.request<ExecutionSessionDto>(`/field/assignments/${assignmentId}/complete`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  }
}

export const api = new ApiClient();
