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

// Equipment Model Types
export interface EquipmentModelDto {
  id: string;
  tenantId: string;
  manufacturerName: string;
  modelCode: string;
  displayName: string;
  categoryCode: string;
  trackingPolicy: string;
  trackingPolicyValue: number;
  status: string;
  statusValue: number;
  createdAtUtc: string;
  modifiedAtUtc: string;
}

export interface CreateEquipmentModelRequest {
  manufacturerName: string;
  modelCode: string;
  displayName: string;
  categoryCode: string;
  trackingPolicy: number;
}

// Asset Registry Types
export interface AssetLifecycleEventDto {
  id: string;
  eventType: string;
  eventTypeValue: number;
  previousStatus?: string | null;
  previousStatusValue?: number | null;
  newStatus?: string | null;
  newStatusValue?: number | null;
  fromSiteId?: string | null;
  toSiteId?: string | null;
  fromOwnerAccountId?: string | null;
  toOwnerAccountId?: string | null;
  reason?: string | null;
  actorUserId?: string | null;
  occurredAtUtc: string;
  recordedAtUtc: string;
}

export interface AssetDto {
  id: string;
  tenantId: string;
  assetNumber: string;
  equipmentModelId: string;
  equipmentModelName?: string | null;
  manufacturerName?: string | null;
  modelCode?: string | null;
  serialNumber?: string | null;
  currentSiteId?: string | null;
  currentSiteName?: string | null;
  currentOwnerAccountId?: string | null;
  currentOwnerAccountName?: string | null;
  status: string;
  statusValue: number;
  installedAtUtc?: string | null;
  decommissionedAtUtc?: string | null;
  createdAtUtc: string;
  modifiedAtUtc: string;
  lifecycleEvents?: AssetLifecycleEventDto[];
}

export interface CreateAssetRequest {
  assetNumber?: string | null;
  equipmentModelId: string;
  serialNumber?: string | null;
  currentSiteId?: string | null;
  currentOwnerAccountId?: string | null;
  status?: number;
}

export interface ChangeAssetStatusRequest {
  targetStatus: number;
  reason?: string | null;
  siteId?: string | null;
}

// Work Order Types
export interface WorkOrderAssetDto {
  id: string;
  assetId: string;
  assetNumber?: string | null;
  modelDisplayName?: string | null;
  role: string;
  roleValue: number;
  siteIdAtTime: string;
  status: string;
  statusValue: number;
}

export interface WorkOrderStatusHistoryDto {
  id: string;
  fromStatus: string;
  fromStatusValue: number;
  toStatus: string;
  toStatusValue: number;
  pauseReasonCode?: string | null;
  changedByUserId?: string | null;
  changedAtUtc: string;
  reason?: string | null;
}

export interface WorkOrderDto {
  id: string;
  tenantId: string;
  workOrderNumber: string;
  serviceRequestId?: string | null;
  serviceAccountId: string;
  serviceAccountName?: string | null;
  billToAccountId: string;
  billToAccountName?: string | null;
  primarySiteId: string;
  primarySiteName?: string | null;
  workTypeCode: string;
  priority: string;
  priorityValue: number;
  operationalStatus: string;
  operationalStatusValue: number;
  summary: string;
  description?: string | null;
  pauseReasonCode?: string | null;
  pauseNote?: string | null;
  operationallyCompletedAtUtc?: string | null;
  createdAtUtc: string;
  modifiedAtUtc: string;
  assets: WorkOrderAssetDto[];
  statusHistory: WorkOrderStatusHistoryDto[];
}

export interface CreateWorkOrderRequest {
  workOrderNumber?: string | null;
  serviceRequestId?: string | null;
  serviceAccountId: string;
  billToAccountId?: string | null;
  primarySiteId: string;
  workTypeCode: string;
  priority: number;
  summary: string;
  description?: string | null;
  primaryAssetId?: string | null;
}

export interface TransitionWorkOrderStatusRequest {
  targetStatus: number;
  pauseReasonCode?: string | null;
  pauseNote?: string | null;
  reason?: string | null;
}
