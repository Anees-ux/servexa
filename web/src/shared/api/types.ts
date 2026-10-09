export interface AccountDto {
  id: string;
  tenantId: string;
  accountNumber: string;
  legalName: string;
  displayName: string;
  accountType: string;
  status: string;
  defaultBranchId?: string | null;
  paymentTermsDays?: number | null;
  currencyCode: string;
  isCreditHold: boolean;
  creditHoldReason?: string | null;
  createdAtUtc: string;
  modifiedAtUtc: string;
}

export interface SiteDto {
  id: string;
  tenantId: string;
  siteNumber: string;
  name: string;
  branchId: string;
  territoryId?: string | null;
  timeZoneId: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  stateProvince: string;
  postalCode: string;
  countryCode: string;
  latitude?: number | null;
  longitude?: number | null;
  status: string;
  accessNotes?: string | null;
  hazardNotes?: string | null;
  createdAtUtc: string;
  modifiedAtUtc: string;
  primaryAccountId?: string | null;
}

export interface AuthResponseDto {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  tenantId: string;
  userId: string;
  email: string;
  displayName: string;
  roles: string[];
  permissions: string[];
}

export interface CurrentUserDto {
  tenantId: string;
  userId?: string | null;
  email?: string | null;
  displayName?: string | null;
  roles: string[];
  permissions: string[];
  isAuthenticated: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateAccountRequest {
  accountNumber: string;
  legalName: string;
  displayName: string;
  accountType?: number;
  defaultBranchId?: string | null;
  paymentTermsDays?: number;
  currencyCode?: string;
}

export interface CreateSiteRequest {
  siteNumber: string;
  name: string;
  branchId: string;
  timeZoneId: string;
  addressLine1: string;
  city: string;
  stateProvince: string;
  postalCode: string;
  countryCode?: string;
  addressLine2?: string | null;
  territoryId?: string | null;
  accessNotes?: string | null;
  hazardNotes?: string | null;
  primaryAccountId?: string | null;
}
