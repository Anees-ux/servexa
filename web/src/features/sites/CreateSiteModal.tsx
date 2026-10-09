import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useCreateSiteMutation } from '../../shared/api/queries';
import type { SiteDto } from '../../shared/api/types';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Modal } from '../../shared/design-system/Modal';

const createSiteSchema = z.object({
  siteNumber: z.string().trim().min(3, 'Site number must be at least 3 characters'),
  name: z.string().trim().min(2, 'Site name is required'),
  branchId: z.string().uuid('Valid Branch ID is required'),
  timeZoneId: z.string().trim().min(1, 'Time zone is required'),
  addressLine1: z.string().trim().min(3, 'Address line 1 is required'),
  addressLine2: z.string().trim().optional(),
  city: z.string().trim().min(2, 'City is required'),
  stateProvince: z.string().trim().min(2, 'State / Province is required'),
  postalCode: z.string().trim().min(2, 'Postal code is required'),
  countryCode: z.string().trim().length(3, 'Country code must be 3 characters (e.g. USA)'),
  primaryAccountId: z.string().trim().optional(),
});

type CreateSiteFormData = z.infer<typeof createSiteSchema>;

interface CreateSiteModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated?: (site: SiteDto) => void;
}

export const CreateSiteModal: React.FC<CreateSiteModalProps> = ({
  isOpen,
  onClose,
  onCreated,
}) => {
  const [serverError, setServerError] = useState<string | null>(null);
  const createMutation = useCreateSiteMutation();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateSiteFormData>({
    resolver: zodResolver(createSiteSchema),
    defaultValues: {
      siteNumber: '',
      name: '',
      branchId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      timeZoneId: 'UTC',
      addressLine1: '',
      addressLine2: '',
      city: '',
      stateProvince: '',
      postalCode: '',
      countryCode: 'USA',
      primaryAccountId: '',
    },
  });

  const onSubmit = async (data: CreateSiteFormData) => {
    setServerError(null);
    try {
      const created = await createMutation.mutateAsync({
        siteNumber: data.siteNumber,
        name: data.name,
        branchId: data.branchId,
        timeZoneId: data.timeZoneId,
        addressLine1: data.addressLine1,
        addressLine2: data.addressLine2 || null,
        city: data.city,
        stateProvince: data.stateProvince,
        postalCode: data.postalCode,
        countryCode: data.countryCode,
        primaryAccountId: data.primaryAccountId?.trim() ? data.primaryAccountId.trim() : null,
      });

      if (onCreated) {
        onCreated(created);
      }
      reset();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to create site.';
      setServerError(message);
    }
  };

  const handleModalClose = () => {
    reset();
    setServerError(null);
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleModalClose} title="Create Operational Site" maxWidth="620px">
      <form onSubmit={handleSubmit(onSubmit)} noValidate style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {serverError && (
          <div
            role="alert"
            style={{
              padding: '10px 12px',
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

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: '12px' }}>
          <Input
            label="Site Number *"
            placeholder="e.g. SITE-010"
            error={errors.siteNumber?.message}
            {...register('siteNumber')}
          />
          <Input
            label="Site Name *"
            placeholder="e.g. Midwest Facility Alpha"
            error={errors.name?.message}
            {...register('name')}
          />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <Input
            label="Branch ID *"
            placeholder="Branch UUID"
            error={errors.branchId?.message}
            {...register('branchId')}
          />
          <Input
            label="Time Zone *"
            placeholder="e.g. UTC, America/Chicago"
            error={errors.timeZoneId?.message}
            {...register('timeZoneId')}
          />
        </div>

        <Input
          label="Address Line 1 *"
          placeholder="e.g. 100 Industrial Parkway"
          error={errors.addressLine1?.message}
          {...register('addressLine1')}
        />

        <Input
          label="Address Line 2 (Optional)"
          placeholder="e.g. Suite 400, Building B"
          error={errors.addressLine2?.message}
          {...register('addressLine2')}
        />

        <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr 1fr', gap: '12px' }}>
          <Input
            label="City *"
            placeholder="e.g. Chicago"
            error={errors.city?.message}
            {...register('city')}
          />
          <Input
            label="State *"
            placeholder="e.g. IL"
            error={errors.stateProvince?.message}
            {...register('stateProvince')}
          />
          <Input
            label="Postal Code *"
            placeholder="e.g. 60601"
            error={errors.postalCode?.message}
            {...register('postalCode')}
          />
          <Input
            label="Country"
            maxLength={3}
            error={errors.countryCode?.message}
            {...register('countryCode')}
          />
        </div>

        <Input
          label="Primary Account ID (Optional)"
          placeholder="Customer Account UUID if associated"
          error={errors.primaryAccountId?.message}
          {...register('primaryAccountId')}
        />

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
          <Button type="button" variant="outline" onClick={handleModalClose} disabled={isSubmitting || createMutation.isPending}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" isLoading={isSubmitting || createMutation.isPending}>
            Save Operational Site
          </Button>
        </div>
      </form>
    </Modal>
  );
};
