import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './apiClient';
import type { AccountDto, CreateAccountRequest, CreateSiteRequest, PagedResult, SiteDto } from './types';

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
