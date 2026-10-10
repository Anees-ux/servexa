import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Modal } from '../../shared/design-system/Modal';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { useCancelBookingMutation } from '../../shared/api/queries';
import { ApiError } from '../../shared/api/apiClient';
import type { BookingDto } from '../../shared/api/types';

interface CancelBookingModalProps {
  booking: BookingDto | null;
  isOpen: boolean;
  onClose: () => void;
}

interface FormValues {
  reason: string;
}

export const CancelBookingModal: React.FC<CancelBookingModalProps> = ({
  booking,
  isOpen,
  onClose,
}) => {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const cancelMutation = useCancelBookingMutation();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: { reason: '' },
  });

  if (!booking) return null;

  const onSubmit = async (data: FormValues) => {
    setErrorMessage(null);
    try {
      await cancelMutation.mutateAsync({
        id: booking.id,
        request: {
          reason: data.reason.trim(),
        },
      });

      reset();
      onClose();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setErrorMessage(err.problem?.detail || err.message);
      } else {
        setErrorMessage((err as Error).message || 'Failed to cancel booking.');
      }
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Cancel Booking ${booking.bookingNumber}`} maxWidth="480px">
      <form onSubmit={handleSubmit(onSubmit)} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {errorMessage && (
          <div
            style={{
              padding: '12px',
              backgroundColor: '#fef2f2',
              color: '#991b1b',
              borderRadius: '6px',
              border: '1px solid #fecaca',
              fontSize: '13px',
            }}
          >
            {errorMessage}
          </div>
        )}

        <p style={{ fontSize: '13px', color: 'var(--text-secondary)', margin: 0 }}>
          Are you sure you want to cancel visit <strong>{booking.bookingNumber}</strong>?
          All active technician assignments and time commitments will be released.
        </p>

        <Input
          label="Cancellation Reason *"
          placeholder="e.g. Customer cancelled, duplicate appointment, site inaccessible..."
          {...register('reason', { required: 'Cancellation reason is required' })}
          error={errors.reason?.message}
        />

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
          <Button variant="outline" type="button" onClick={onClose} disabled={cancelMutation.isPending}>
            Keep Booking
          </Button>
          <Button variant="danger" type="submit" isLoading={cancelMutation.isPending}>
            Confirm Cancellation
          </Button>
        </div>
      </form>
    </Modal>
  );
};
