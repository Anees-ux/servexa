import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Modal } from '../../shared/design-system/Modal';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { useRescheduleBookingMutation } from '../../shared/api/queries';
import { ApiError } from '../../shared/api/apiClient';
import type { BookingDto } from '../../shared/api/types';
import {
  localDateTimeToUtcIso,
  utcToLocalDateTime,
  formatSiteDateTime,
} from '../../shared/utils/timezone';

interface RescheduleBookingModalProps {
  booking: BookingDto | null;
  isOpen: boolean;
  onClose: () => void;
}

interface FormValues {
  newStartDate: string;
  newStartTime: string;
  newEndDate: string;
  newEndTime: string;
  reason: string;
}

export const RescheduleBookingModal: React.FC<RescheduleBookingModalProps> = ({
  booking,
  isOpen,
  onClose,
}) => {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const rescheduleMutation = useRescheduleBookingMutation();

  const siteTz = booking?.siteTimeZoneId || 'UTC';
  const initialStart = React.useMemo(
    () => (booking ? utcToLocalDateTime(booking.plannedStartUtc, siteTz) : { date: '', time: '' }),
    [booking, siteTz]
  );
  const initialEnd = React.useMemo(
    () => (booking ? utcToLocalDateTime(booking.plannedEndUtc, siteTz) : { date: '', time: '' }),
    [booking, siteTz]
  );

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    values: {
      newStartDate: initialStart.date,
      newStartTime: initialStart.time,
      newEndDate: initialEnd.date,
      newEndTime: initialEnd.time,
      reason: '',
    },
  });

  if (!booking) return null;

  const onSubmit = async (data: FormValues) => {
    setErrorMessage(null);
    try {
      const startIso = localDateTimeToUtcIso(data.newStartDate, data.newStartTime, siteTz);
      const endIso = localDateTimeToUtcIso(data.newEndDate, data.newEndTime, siteTz);

      if (new Date(endIso) <= new Date(startIso)) {
        setErrorMessage('New end time must be after new start time.');
        return;
      }

      await rescheduleMutation.mutateAsync({
        id: booking.id,
        request: {
          newStartUtc: startIso,
          newEndUtc: endIso,
          reason: data.reason.trim(),
          initiator: 'Dispatcher',
        },
      });

      reset();
      onClose();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setErrorMessage(err.problem?.detail || err.message);
      } else {
        setErrorMessage((err as Error).message || 'Failed to reschedule booking.');
      }
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Reschedule Booking ${booking.bookingNumber}`} maxWidth="550px">
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

        <div
          style={{
            fontSize: '12px',
            padding: '8px 12px',
            backgroundColor: '#f1f5f9',
            borderRadius: '6px',
            color: '#334155',
            border: '1px solid #e2e8f0',
          }}
        >
          Site Timezone: <strong>{siteTz}</strong> — All reschedule times are Site-local.
        </div>

        <div style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
          Currently scheduled: <strong>{formatSiteDateTime(booking.plannedStartUtc, siteTz)}</strong> to{' '}
          <strong>{formatSiteDateTime(booking.plannedEndUtc, siteTz)}</strong>.
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <Input
            label="New Start Date *"
            type="date"
            {...register('newStartDate', { required: 'Start date is required' })}
            error={errors.newStartDate?.message}
          />
          <Input
            label={`New Start Time (${siteTz}) *`}
            type="time"
            {...register('newStartTime', { required: 'Start time is required' })}
            error={errors.newStartTime?.message}
          />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <Input
            label="New End Date *"
            type="date"
            {...register('newEndDate', { required: 'End date is required' })}
            error={errors.newEndDate?.message}
          />
          <Input
            label={`New End Time (${siteTz}) *`}
            type="time"
            {...register('newEndTime', { required: 'End time is required' })}
            error={errors.newEndTime?.message}
          />
        </div>

        <Input
          label="Reschedule Reason (Max 500 characters) *"
          placeholder="e.g. Customer requested morning appointment, emergency reschedule..."
          maxLength={500}
          {...register('reason', {
            required: 'Reason is required for audit revision',
            maxLength: { value: 500, message: 'Reason must not exceed 500 characters' },
          })}
          error={errors.reason?.message}
        />

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
          <Button variant="outline" type="button" onClick={onClose} disabled={rescheduleMutation.isPending}>
            Cancel
          </Button>
          <Button type="submit" isLoading={rescheduleMutation.isPending}>
            Save Reschedule
          </Button>
        </div>
      </form>
    </Modal>
  );
};
