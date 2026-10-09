import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  useAccountsQuery,
  useAssetsQuery,
  useCreateWorkOrderMutation,
  useSitesQuery,
} from '../../shared/api/queries';
import type { WorkOrderDto } from '../../shared/api/types';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Modal } from '../../shared/design-system/Modal';
import { Select } from '../../shared/design-system/Select';

const createWorkOrderSchema = z.object({
  serviceAccountId: z.string().min(1, 'Customer account is required'),
  primarySiteId: z.string().min(1, 'Operational site is required'),
  workTypeCode: z.string().min(1, 'Work type is required'),
  priority: z.number().int(),
  summary: z.string().trim().min(5, 'Summary must be at least 5 characters'),
  description: z.string().trim().optional(),
  primaryAssetId: z.string().optional(),
});

type CreateWorkOrderFormData = z.infer<typeof createWorkOrderSchema>;

interface CreateWorkOrderModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated?: (workOrder: WorkOrderDto) => void;
}

export const CreateWorkOrderModal: React.FC<CreateWorkOrderModalProps> = ({
  isOpen,
  onClose,
  onCreated,
}) => {
  const [serverError, setServerError] = useState<string | null>(null);
  const createMutation = useCreateWorkOrderMutation();

  const { data: accountsData } = useAccountsQuery('', 1, 100);
  const { data: sitesData } = useSitesQuery(undefined, undefined, '', 1, 100);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<CreateWorkOrderFormData>({
    resolver: zodResolver(createWorkOrderSchema),
    defaultValues: {
      serviceAccountId: '',
      primarySiteId: '',
      workTypeCode: 'CORRECTIVE',
      priority: 2, // Standard
      summary: '',
      description: '',
      primaryAssetId: '',
    },
  });

  const selectedSiteId = watch('primarySiteId');
  const selectedAccountId = watch('serviceAccountId');

  // Load assets filtered by selected site if chosen
  const { data: assetsData } = useAssetsQuery(
    selectedSiteId || undefined,
    selectedAccountId || undefined,
    undefined,
    '',
    1,
    50
  );

  const onSubmit = async (data: CreateWorkOrderFormData) => {
    setServerError(null);
    try {
      const created = await createMutation.mutateAsync({
        serviceAccountId: data.serviceAccountId,
        primarySiteId: data.primarySiteId,
        workTypeCode: data.workTypeCode,
        priority: Number(data.priority),
        summary: data.summary.trim(),
        description: data.description ? data.description.trim() : null,
        primaryAssetId: data.primaryAssetId || null,
      });
      reset();
      onClose();
      onCreated?.(created);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to create work order';
      setServerError(message);
    }
  };

  const accountOptions = [
    { value: '', label: '-- Select Customer Service Account --' },
    ...(accountsData?.items.map((a) => ({
      value: a.id,
      label: `${a.accountNumber} - ${a.displayName}`,
    })) || []),
  ];

  const siteOptions = [
    { value: '', label: '-- Select Operational Site --' },
    ...(sitesData?.items.map((s) => ({
      value: s.id,
      label: `${s.siteNumber} - ${s.name} (${s.city})`,
    })) || []),
  ];

  const assetOptions = [
    { value: '', label: '-- None / Facility-Level Scope --' },
    ...(assetsData?.items.map((a) => ({
      value: a.id,
      label: `${a.assetNumber} - ${a.manufacturerName} ${a.modelCode} (${a.status})`,
    })) || []),
  ];

  return (
    <Modal
      isOpen={isOpen}
      onClose={() => {
        reset();
        setServerError(null);
        onClose();
      }}
      title="Create Work Order"
      subtitle="Initiate an operational service commitment."
      size="lg"
    >
      <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {serverError && (
          <div
            role="alert"
            style={{
              padding: '12px',
              backgroundColor: 'var(--danger-subtle)',
              border: '1px solid var(--danger)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--danger-text)',
              fontSize: '13px',
            }}
          >
            {serverError}
          </div>
        )}

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <Select
            label="Service Customer Account"
            {...register('serviceAccountId')}
            error={errors.serviceAccountId?.message}
            options={accountOptions}
            required
          />

          <Select
            label="Operational Site"
            {...register('primarySiteId')}
            error={errors.primarySiteId?.message}
            options={siteOptions}
            required
          />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <Select
            label="Work Type"
            {...register('workTypeCode')}
            options={[
              { value: 'CORRECTIVE', label: 'Corrective / Repair' },
              { value: 'PREVENTIVE', label: 'Preventive Maintenance' },
              { value: 'INSPECTION', label: 'Safety & Compliance Inspection' },
              { value: 'INSTALLATION', label: 'Equipment Installation' },
              { value: 'EMERGENCY', label: 'Emergency Breakdown' },
            ]}
          />

          <Select
            label="Operational Priority"
            {...register('priority', { valueAsNumber: true })}
            options={[
              { value: '1', label: 'Low (Routine / Backlog)' },
              { value: '2', label: 'Standard (Standard SLA)' },
              { value: '3', label: 'High (Expedited Service)' },
              { value: '4', label: 'Critical (Outage / Emergency SLA)' },
            ]}
          />
        </div>

        <Select
          label="Primary In-Scope Asset (Optional)"
          {...register('primaryAssetId')}
          options={assetOptions}
        />

        <Input
          label="Summary"
          placeholder="Brief operational summary of the issue or scope..."
          {...register('summary')}
          error={errors.summary?.message}
          required
        />

        <div>
          <label style={{ display: 'block', fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px' }}>
            Work Scope Description
          </label>
          <textarea
            {...register('description')}
            rows={3}
            placeholder="Detailed scope, symptoms, access instructions, or special tooling needed..."
            style={{
              width: '100%',
              padding: '8px 12px',
              fontSize: '13px',
              fontFamily: 'inherit',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--border-subtle)',
              backgroundColor: 'var(--bg-surface)',
              color: 'var(--text-primary)',
              resize: 'vertical',
            }}
          />
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
          <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? 'Creating...' : 'Create Work Order'}
          </Button>
        </div>
      </form>
    </Modal>
  );
};
