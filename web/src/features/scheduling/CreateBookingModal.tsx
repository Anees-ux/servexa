import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Modal } from '../../shared/design-system/Modal';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Select } from '../../shared/design-system/Select';
import {
  useWorkOrdersQuery,
  useResourcesQuery,
  useCreateBookingMutation,
  useSitesQuery,
} from '../../shared/api/queries';
import { ApiError } from '../../shared/api/apiClient';
import { localDateTimeToUtcIso } from '../../shared/utils/timezone';

interface CreateBookingModalProps {
  isOpen: boolean;
  onClose: () => void;
  preselectedWorkOrderId?: string;
}

interface FormValues {
  workOrderId: string;
  plannedStartDate: string;
  plannedStartTime: string;
  plannedEndDate: string;
  plannedEndTime: string;
  siteTimeZoneId: string;
  primaryResourceId: string;
  schedulingNotes: string;
}

export const CreateBookingModal: React.FC<CreateBookingModalProps> = ({
  isOpen,
  onClose,
  preselectedWorkOrderId,
}) => {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Load available work orders (Approved, Scheduled, InProgress)
  const { data: workOrdersData, isLoading: isLoadingWo } = useWorkOrdersQuery(
    undefined,
    undefined,
    undefined,
    undefined,
    undefined,
    1,
    50
  );

  // Load available technicians
  const { data: resourcesData, isLoading: isLoadingRes } = useResourcesQuery(
    1, // Active
    0, // Technician
    undefined,
    1,
    50
  );

  const createBookingMutation = useCreateBookingMutation();

  // Default times: tomorrow 09:00 - 11:00 UTC
  const defaultDateStr = React.useMemo(() => {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  }, []);

  // Load sites to resolve authoritative site timezone
  const { data: sitesData } = useSitesQuery(undefined, undefined, '', 1, 100);

  const schedulableWorkOrders = (workOrdersData?.items || []).filter(
    (wo) =>
      wo.operationalStatus.toLowerCase() === 'approved' ||
      wo.operationalStatus.toLowerCase() === 'scheduled' ||
      wo.operationalStatus.toLowerCase() === 'inprogress'
  );

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: {
      workOrderId: preselectedWorkOrderId || '',
      plannedStartDate: defaultDateStr,
      plannedStartTime: '09:00',
      plannedEndDate: defaultDateStr,
      plannedEndTime: '11:00',
      siteTimeZoneId: 'UTC',
      primaryResourceId: '',
      schedulingNotes: '',
    },
  });

  const selectedWoId = watch('workOrderId') || preselectedWorkOrderId;
  const selectedWo = schedulableWorkOrders.find((wo) => wo.id === selectedWoId);
  const selectedSite = sitesData?.items?.find((s) => s.id === selectedWo?.primarySiteId);
  const authoritativeTz = selectedSite?.timeZoneId || 'UTC';

  const onSubmit = async (data: FormValues) => {
    setErrorMessage(null);
    try {
      const startIso = localDateTimeToUtcIso(data.plannedStartDate, data.plannedStartTime, authoritativeTz);
      const endIso = localDateTimeToUtcIso(data.plannedEndDate, data.plannedEndTime, authoritativeTz);

      if (new Date(endIso) <= new Date(startIso)) {
        setErrorMessage('Planned end time must be after planned start time.');
        return;
      }

      await createBookingMutation.mutateAsync({
        workOrderId: data.workOrderId,
        plannedStartUtc: startIso,
        plannedEndUtc: endIso,
        siteTimeZoneId: authoritativeTz,
        schedulingNotes: data.schedulingNotes ? data.schedulingNotes.trim() : null,
        primaryResourceId: data.primaryResourceId ? data.primaryResourceId : null,
      });

      reset();
      onClose();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setErrorMessage(err.problem?.detail || err.message);
      } else {
        setErrorMessage((err as Error).message || 'Failed to create booking.');
      }
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Schedule Work Order Booking" maxWidth="600px">
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

        <Select
          label="Work Order *"
          {...register('workOrderId', { required: 'Work order is required' })}
          error={errors.workOrderId?.message}
          disabled={isLoadingWo || Boolean(preselectedWorkOrderId)}
        >
          <option value="">Select an Approved Work Order...</option>
          {schedulableWorkOrders.map((wo) => (
            <option key={wo.id} value={wo.id}>
              {wo.workOrderNumber} — {wo.summary} ({wo.operationalStatus})
            </option>
          ))}
        </Select>

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
          Site Timezone: <strong>{authoritativeTz}</strong>
          {selectedSite ? ` (${selectedSite.name})` : ''} — Scheduling times are entered in Site-local time and saved as UTC.
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <Input
            label="Start Date *"
            type="date"
            {...register('plannedStartDate', { required: 'Start date is required' })}
            error={errors.plannedStartDate?.message}
          />
          <Input
            label={`Start Time (${authoritativeTz}) *`}
            type="time"
            {...register('plannedStartTime', { required: 'Start time is required' })}
            error={errors.plannedStartTime?.message}
          />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
          <Input
            label="End Date *"
            type="date"
            {...register('plannedEndDate', { required: 'End date is required' })}
            error={errors.plannedEndDate?.message}
          />
          <Input
            label={`End Time (${authoritativeTz}) *`}
            type="time"
            {...register('plannedEndTime', { required: 'End time is required' })}
            error={errors.plannedEndTime?.message}
          />
        </div>

        <Select
          label="Primary Technician (Optional)"
          {...register('primaryResourceId')}
          disabled={isLoadingRes}
        >
          <option value="">Leave Unassigned (Assign Later in Dispatch)</option>
          {(resourcesData?.items || []).map((res) => (
            <option key={res.id} value={res.id}>
              {res.displayName} ({res.resourceCode})
            </option>
          ))}
        </Select>

        <Input
          label="Scheduling Notes (Max 1000 characters)"
          placeholder="Access instructions, customer window preferences..."
          maxLength={1000}
          {...register('schedulingNotes', { maxLength: { value: 1000, message: 'Scheduling notes must not exceed 1000 characters' } })}
          error={errors.schedulingNotes?.message}
        />

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
          <Button variant="outline" type="button" onClick={onClose} disabled={createBookingMutation.isPending}>
            Cancel
          </Button>
          <Button type="submit" isLoading={createBookingMutation.isPending}>
            Confirm & Schedule
          </Button>
        </div>
      </form>
    </Modal>
  );
};
