import type {
  AccountDto,
  AuthResponseDto,
  CreateAccountRequest,
  CreateSiteRequest,
  CurrentUserDto,
  PagedResult,
  SiteDto,
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
}

export const api = new ApiClient();
