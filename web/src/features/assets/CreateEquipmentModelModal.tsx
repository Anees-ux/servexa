import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useCreateEquipmentModelMutation } from '../../shared/api/queries';
import type { EquipmentModelDto } from '../../shared/api/types';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Modal } from '../../shared/design-system/Modal';
import { Select } from '../../shared/design-system/Select';

const createModelSchema = z.object({
  manufacturerName: z.string().trim().min(2, 'Manufacturer name is required'),
  modelCode: z.string().trim().min(2, 'Model code is required'),
  displayName: z.string().trim().min(2, 'Display name is required'),
  categoryCode: z.string().trim().min(2, 'Category code is required'),
  trackingPolicy: z.number().int(),
});

type CreateModelFormData = z.infer<typeof createModelSchema>;

interface CreateEquipmentModelModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated?: (model: EquipmentModelDto) => void;
}

export const CreateEquipmentModelModal: React.FC<CreateEquipmentModelModalProps> = ({
  isOpen,
  onClose,
  onCreated,
}) => {
  const [serverError, setServerError] = useState<string | null>(null);
  const createMutation = useCreateEquipmentModelMutation();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateModelFormData>({
    resolver: zodResolver(createModelSchema),
    defaultValues: {
      manufacturerName: '',
      modelCode: '',
      displayName: '',
      categoryCode: 'HVAC',
      trackingPolicy: 1, // Serialized
    },
  });

  const onSubmit = async (data: CreateModelFormData) => {
    setServerError(null);
    try {
      const created = await createMutation.mutateAsync({
        manufacturerName: data.manufacturerName,
        modelCode: data.modelCode,
        displayName: data.displayName,
        categoryCode: data.categoryCode,
        trackingPolicy: Number(data.trackingPolicy),
      });
      reset();
      onClose();
      onCreated?.(created);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to register equipment model';
      setServerError(message);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={() => {
        reset();
        setServerError(null);
        onClose();
      }}
      title="Define Equipment Model"
      subtitle="Master manufacturer specifications and tracking policy."
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

        <Input
          label="Manufacturer Name"
          placeholder="e.g. Trane, Carrier, Caterpillar"
          {...register('manufacturerName')}
          error={errors.manufacturerName?.message}
          required
        />

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <Input
            label="Model Code"
            placeholder="e.g. CVHE-500, 3516B"
            {...register('modelCode')}
            error={errors.modelCode?.message}
            required
          />

          <Select
            label="Category"
            {...register('categoryCode')}
            error={errors.categoryCode?.message}
            options={[
              { value: 'HVAC', label: 'HVAC & Chillers' },
              { value: 'ELECTRICAL', label: 'Electrical & Power' },
              { value: 'GENERATOR', label: 'Generators & Turbines' },
              { value: 'PLUMBING', label: 'Plumbing & Hydraulic' },
              { value: 'MECHANICAL', label: 'Mechanical & Conveyors' },
              { value: 'FIRE_SAFETY', label: 'Fire & Life Safety' },
            ]}
          />
        </div>

        <Input
          label="Display Name"
          placeholder="e.g. Centrifugal Chiller 500-Ton High Efficiency"
          {...register('displayName')}
          error={errors.displayName?.message}
          required
        />

        <Select
          label="Tracking Policy"
          {...register('trackingPolicy', { valueAsNumber: true })}
          options={[
            { value: '1', label: 'Serialized (Requires unique serial number per asset)' },
            { value: '2', label: 'Non-Serialized (Tracked by model and asset tag)' },
          ]}
        />

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
          <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? 'Registering...' : 'Save Equipment Model'}
          </Button>
        </div>
      </form>
    </Modal>
  );
};
