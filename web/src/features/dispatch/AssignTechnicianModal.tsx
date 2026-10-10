import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Modal } from '../../shared/design-system/Modal';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Select } from '../../shared/design-system/Select';
import {
  useResourcesQuery,
  useAssignResourceMutation,
} from '../../shared/api/queries';
import { ApiError } from '../../shared/api/apiClient';
import type { BookingDto } from '../../shared/api/types';

interface AssignTechnicianModalProps {
  booking: BookingDto | null;
  isOpen: boolean;
  onClose: () => void;
}

interface FormValues {
  resourceId: string;
  assignmentRole: number;
  selectionRationale: string;
}

export const AssignTechnicianModal: React.FC<AssignTechnicianModalProps> = ({
  booking,
  isOpen,
  onClose,
}) => {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Fetch active technician resources
  const { data: resourcesData, isLoading: isLoadingRes } = useResourcesQuery(
    1, // Active
    0, // Technician
    undefined,
    1,
    50
  );

  const assignMutation = useAssignResourceMutation();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: {
      resourceId: '',
      assignmentRole: 0, // Lead
      selectionRationale: '',
    },
  });

  if (!booking) return null;

  const onSubmit = async (data: FormValues) => {
    setErrorMessage(null);
    try {
      await assignMutation.mutateAsync({
        id: booking.id,
        request: {
          resourceId: data.resourceId,
          assignmentRole: Number(data.assignmentRole),
          selectionRationale: data.selectionRationale ? data.selectionRationale.trim() : null,
        },
      });

      reset();
      onClose();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setErrorMessage(err.problem?.detail || err.message);
      } else {
        setErrorMessage((err as Error).message || 'Failed to assign technician.');
      }
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Assign Resource to ${booking.bookingNumber}`} maxWidth="520px">
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

        <div style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
          Work Order: <strong>{booking.workOrderNumber}</strong> — {booking.workOrderSummary}
          <br />
          Scheduled Window:{' '}
          <strong>
            {new Date(booking.plannedStartUtc).toLocaleDateString()}{' '}
            {new Date(booking.plannedStartUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} –{' '}
            {new Date(booking.plannedEndUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
          </strong>
        </div>

        <Select
          label="Technician / Resource *"
          {...register('resourceId', { required: 'Please select a technician' })}
          error={errors.resourceId?.message}
          disabled={isLoadingRes}
        >
          <option value="">Select an Active Technician...</option>
          {(resourcesData?.items || []).map((res) => (
            <option key={res.id} value={res.id}>
              {res.displayName} ({res.resourceCode})
            </option>
          ))}
        </Select>

        <Select
          label="Assignment Role *"
          {...register('assignmentRole', { required: true })}
        >
          <option value={0}>Lead Technician (Primary)</option>
          <option value={1}>Support Technician (Assistant)</option>
        </Select>

        <Input
          label="Selection Rationale"
          placeholder="e.g. Nearest available tech with certified HVAC skillset"
          {...register('selectionRationale')}
        />

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
          <Button variant="outline" type="button" onClick={onClose} disabled={assignMutation.isPending}>
            Cancel
          </Button>
          <Button type="submit" isLoading={assignMutation.isPending}>
            Confirm Assignment
          </Button>
        </div>
      </form>
    </Modal>
  );
};
