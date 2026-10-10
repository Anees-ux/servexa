/**
 * API Transport Contracts
 * Source of truth: OpenAPI-generated schemas from Servexa.Api
 */
import type { components, paths } from './generated';

export type { paths };
export type Schemas = components['schemas'];

// Generic PagedResult wrapper
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages?: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

// Auth types (frontend session & development tokens)
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

// Accounts
export type AccountDto = Schemas['AccountDto'];
export type CreateAccountRequest = Schemas['CreateAccountCommand'];

// Sites
export type SiteDto = Schemas['SiteDto'];
export type CreateSiteRequest = Schemas['CreateSiteCommand'];

// Equipment Models
export type EquipmentModelDto = Schemas['EquipmentModelDto'];
export type CreateEquipmentModelRequest = Schemas['CreateEquipmentModelCommand'];

// Assets
export type AssetDto = Schemas['AssetDto'];
export type AssetLifecycleEventDto = Schemas['AssetLifecycleEventDto'];
export type CreateAssetRequest = Schemas['CreateAssetCommand'];
export type ChangeAssetStatusRequest = Schemas['ChangeAssetStatusRequest'];

// Work Orders
export type WorkOrderDto = Schemas['WorkOrderDto'];
export type WorkOrderAssetDto = Schemas['WorkOrderAssetDto'];
export type WorkOrderStatusHistoryDto = Schemas['WorkOrderStatusHistoryDto'];
export type CreateWorkOrderRequest = Omit<Schemas['CreateWorkOrderCommand'], 'workOrderNumber' | 'serviceRequestId' | 'billToAccountId'> & {
  workOrderNumber?: string | null;
  serviceRequestId?: string | null;
  billToAccountId?: string | null;
};
export type TransitionWorkOrderStatusRequest = Schemas['TransitionWorkOrderStatusRequest'];
export type WorkOrderScopeItemDto = Schemas['WorkOrderScopeItemDto'];
export type CompletionGateResultDto = Schemas['CompletionGateResultDto'];
export type WorkOrderCompletionEvaluationDto = Schemas['WorkOrderCompletionEvaluationDto'];
export type WorkOrderCompletionReadinessDto = Schemas['WorkOrderCompletionReadinessDto'];
export type CompleteWorkOrderRequest = Schemas['CompleteWorkOrderRequest'];
export type AddScopeItemRequest = Schemas['AddScopeItemRequest'];
export type UpdateScopeItemStatusRequest = Schemas['UpdateScopeItemStatusRequest'];

// Scheduling & Bookings
export type BookingDto = Schemas['BookingDto'];
export type ResourceAssignmentDto = Schemas['ResourceAssignmentDto'];
export type BookingScheduleRevisionDto = Schemas['BookingScheduleRevisionDto'];
export type BookingStatusHistoryDto = Schemas['BookingStatusHistoryDto'];
export type CreateBookingRequest = Schemas['CreateBookingCommand'];
export type RescheduleBookingRequest = Schemas['RescheduleBookingRequest'];
export type CancelBookingRequest = Schemas['CancelBookingRequest'];
export type AssignResourceRequest = Schemas['AssignResourceRequest'];

// Resources
export type ResourceDto = Schemas['ResourceDto'];
export type ResourceScheduleDto = Schemas['ResourceScheduleDto'];
export type ResourceCommitmentDto = Schemas['ResourceCommitmentDto'];

// Field Execution & Technician Work Queue
export type AssignedJobDto = Schemas['AssignedJobDto'];
export type ExecutionSessionDto = Schemas['ExecutionSessionDto'];
export type ExecutionIntervalDto = Schemas['ExecutionIntervalDto'];
export type PauseWorkRequest = Schemas['PauseWorkRequest'];
export type CompleteExecutionRequest = Schemas['CompleteExecutionRequest'];

// Field Tasks & Inspections
export type WorkTaskDto = Schemas['WorkTaskDto'];
export type CreateWorkTaskRequest = Schemas['CreateWorkTaskRequest'];
export type UpdateWorkTaskStatusRequest = Schemas['UpdateWorkTaskStatusRequest'];

