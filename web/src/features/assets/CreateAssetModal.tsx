import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  useAccountsQuery,
  useCreateAssetMutation,
  useEquipmentModelsQuery,
  useSitesQuery,
} from '../../shared/api/queries';
import type { AssetDto } from '../../shared/api/types';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Modal } from '../../shared/design-system/Modal';
import { Select } from '../../shared/design-system/Select';

const createAssetSchema = z.object({
  equipmentModelId: z.string().min(1, 'Equipment model is required'),
  assetNumber: z.string().trim().optional(),
  serialNumber: z.string().trim().optional(),
  currentOwnerAccountId: z.string().optional(),
  currentSiteId: z.string().optional(),
  status: z.number().int(),
});

type CreateAssetFormData = z.infer<typeof createAssetSchema>;

interface CreateAssetModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated?: (asset: AssetDto) => void;
}

export const CreateAssetModal: React.FC<CreateAssetModalProps> = ({
  isOpen,
  onClose,
  onCreated,
}) => {
  const [serverError, setServerError] = useState<string | null>(null);
  const createMutation = useCreateAssetMutation();

  const { data: modelsData } = useEquipmentModelsQuery('', 1, 100);
  const { data: accountsData } = useAccountsQuery('', 1, 100);
  const { data: sitesData } = useSitesQuery(undefined, undefined, '', 1, 100);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<CreateAssetFormData>({
    resolver: zodResolver(createAssetSchema),
    defaultValues: {
      equipmentModelId: '',
      assetNumber: '',
      serialNumber: '',
      currentOwnerAccountId: '',
      currentSiteId: '',
      status: 2, // Active
    },
  });

  const selectedModelId = watch('equipmentModelId');
  const selectedModel = modelsData?.items.find((m) => m.id === selectedModelId);

  const onSubmit = async (data: CreateAssetFormData) => {
    setServerError(null);
    try {
      const created = await createMutation.mutateAsync({
        equipmentModelId: data.equipmentModelId,
        assetNumber: data.assetNumber ? data.assetNumber.trim() : null,
        serialNumber: data.serialNumber ? data.serialNumber.trim() : null,
        currentOwnerAccountId: data.currentOwnerAccountId || null,
        currentSiteId: data.currentSiteId || null,
        status: Number(data.status),
      });
      reset();
      onClose();
      onCreated?.(created);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to register asset';
      setServerError(message);
    }
  };

  const modelOptions = [
    { value: '', label: '-- Select Equipment Model --' },
    ...(modelsData?.items.map((m) => ({
      value: m.id,
      label: `${m.manufacturerName} ${m.modelCode} - ${m.displayName} (${m.trackingPolicy})`,
    })) || []),
  ];

  const accountOptions = [
    { value: '', label: '-- Select Owner Account (Optional) --' },
    ...(accountsData?.items.map((a) => ({
      value: a.id,
      label: `${a.accountNumber} - ${a.displayName}`,
    })) || []),
  ];

  const siteOptions = [
    { value: '', label: '-- Select Installed Site (Optional) --' },
    ...(sitesData?.items.map((s) => ({
      value: s.id,
      label: `${s.siteNumber} - ${s.name} (${s.city})`,
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
      title="Register New Asset"
      subtitle="Register an operational equipment asset in the tenant registry."
      size="md"
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

        <Select
          label="Equipment Model"
          {...register('equipmentModelId')}
          error={errors.equipmentModelId?.message}
          options={modelOptions}
          required
        />

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <Input
            label="Asset Tag / Number"
            placeholder="Auto-allocated (AST-...) if blank"
            {...register('assetNumber')}
            error={errors.assetNumber?.message}
          />

          <Input
            label="Serial Number"
            placeholder={selectedModel?.trackingPolicyValue === 1 ? 'Required for serialized model' : 'Optional serial number'}
            {...register('serialNumber')}
            error={errors.serialNumber?.message}
          />
        </div>

        <Select
          label="Customer Owner Account"
          {...register('currentOwnerAccountId')}
          error={errors.currentOwnerAccountId?.message}
          options={accountOptions}
        />

        <Select
          label="Installed Operational Site"
          {...register('currentSiteId')}
          error={errors.currentSiteId?.message}
          options={siteOptions}
        />

        <Select
          label="Initial Operational Status"
          {...register('status', { valueAsNumber: true })}
          options={[
            { value: '2', label: 'Active (Commissioned & Operational)' },
            { value: '1', label: 'Pre-Installation (In staging or transit)' },
          ]}
        />

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
          <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? 'Registering...' : 'Register Asset'}
          </Button>
        </div>
      </form>
    </Modal>
  );
};
