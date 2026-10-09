import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useCreateAccountMutation } from '../../shared/api/queries';
import type { AccountDto } from '../../shared/api/types';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Modal } from '../../shared/design-system/Modal';
import { Select } from '../../shared/design-system/Select';

const createAccountSchema = z.object({
  accountNumber: z.string().trim().min(3, 'Account number must be at least 3 characters'),
  legalName: z.string().trim().min(2, 'Legal name is required'),
  displayName: z.string().trim().min(2, 'Display name is required'),
  accountType: z.number().int(),
  paymentTermsDays: z.number().int().min(0, 'Payment terms cannot be negative'),
  currencyCode: z.string().trim().length(3, 'Currency code must be exactly 3 characters (e.g. USD)'),
});

type CreateAccountFormData = z.infer<typeof createAccountSchema>;

interface CreateAccountModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated?: (account: AccountDto) => void;
}

export const CreateAccountModal: React.FC<CreateAccountModalProps> = ({
  isOpen,
  onClose,
  onCreated,
}) => {
  const [serverError, setServerError] = useState<string | null>(null);
  const createMutation = useCreateAccountMutation();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateAccountFormData>({
    resolver: zodResolver(createAccountSchema),
    defaultValues: {
      accountNumber: '',
      legalName: '',
      displayName: '',
      accountType: 1,
      paymentTermsDays: 30,
      currencyCode: 'USD',
    },
  });

  const onSubmit = async (data: CreateAccountFormData) => {
    setServerError(null);
    try {
      const created = await createMutation.mutateAsync({
        accountNumber: data.accountNumber,
        legalName: data.legalName,
        displayName: data.displayName,
        accountType: data.accountType,
        paymentTermsDays: data.paymentTermsDays,
        currencyCode: data.currencyCode,
      });

      if (onCreated) {
        onCreated(created);
      }
      reset();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to create customer account.';
      setServerError(message);
    }
  };

  const handleModalClose = () => {
    reset();
    setServerError(null);
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleModalClose} title="Create Customer Account">
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
            label="Account Number *"
            placeholder="e.g. ACC-1001"
            error={errors.accountNumber?.message}
            {...register('accountNumber')}
          />
          <Input
            label="Legal Name *"
            placeholder="e.g. Acme Industrial Corp LLC"
            error={errors.legalName?.message}
            {...register('legalName')}
          />
        </div>

        <Input
          label="Display Name *"
          placeholder="e.g. Acme Industrial"
          error={errors.displayName?.message}
          {...register('displayName')}
        />

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '12px' }}>
          <Select
            label="Account Type"
            options={[
              { value: 1, label: 'Commercial' },
              { value: 2, label: 'Industrial' },
              { value: 3, label: 'Residential' },
              { value: 4, label: 'Enterprise' },
              { value: 5, label: 'Government' },
            ]}
            error={errors.accountType?.message}
            {...register('accountType', { valueAsNumber: true })}
          />
          <Input
            label="Terms (Days)"
            type="number"
            min={0}
            error={errors.paymentTermsDays?.message}
            {...register('paymentTermsDays', { valueAsNumber: true })}
          />
          <Input
            label="Currency"
            maxLength={3}
            error={errors.currencyCode?.message}
            {...register('currencyCode')}
          />
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
          <Button type="button" variant="outline" onClick={handleModalClose} disabled={isSubmitting || createMutation.isPending}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" isLoading={isSubmitting || createMutation.isPending}>
            Save Customer Account
          </Button>
        </div>
      </form>
    </Modal>
  );
};
