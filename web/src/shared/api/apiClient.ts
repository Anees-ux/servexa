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
}

export const api = new ApiClient();
