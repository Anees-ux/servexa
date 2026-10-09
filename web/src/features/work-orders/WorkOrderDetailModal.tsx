import React, { useState } from 'react';
import { AlertCircle, CheckCircle2, Clock, History, PauseCircle, PlayCircle, XCircle } from 'lucide-react';
import { useTransitionWorkOrderStatusMutation } from '../../shared/api/queries';
import type { WorkOrderDto } from '../../shared/api/types';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Modal } from '../../shared/design-system/Modal';
import { Select } from '../../shared/design-system/Select';
import { StatusBadge } from '../../shared/design-system/StatusBadge';

interface WorkOrderDetailModalProps {
  workOrder: WorkOrderDto | null;
  isOpen: boolean;
  onClose: () => void;
  onUpdated?: (updated: WorkOrderDto) => void;
}

export const WorkOrderDetailModal: React.FC<WorkOrderDetailModalProps> = ({
  workOrder,
  isOpen,
  onClose,
  onUpdated,
}) => {
  const [reason, setReason] = useState('');
  const [pauseReasonCode, setPauseReasonCode] = useState('WAIT_PARTS');
  const [pauseNote, setPauseNote] = useState('');
  const [actionError, setActionError] = useState<string | null>(null);
  const [isCancelConfirmOpen, setIsCancelConfirmOpen] = useState(false);
  const [cancellationReason, setCancellationReason] = useState('');
  const [cancelError, setCancelError] = useState<string | null>(null);

  const transitionMutation = useTransitionWorkOrderStatusMutation();

  if (!workOrder) return null;

  const handleOpenCancelDialog = () => {
    setActionError(null);
    setCancelError(null);
    setCancellationReason('');
    setIsCancelConfirmOpen(true);
  };

  const handleConfirmCancel = async () => {
    const trimmedReason = cancellationReason.trim();
    if (!trimmedReason) {
      setCancelError('A non-empty cancellation reason is required.');
      return;
    }

    setCancelError(null);
    setActionError(null);
    try {
      const updated = await transitionMutation.mutateAsync({
        id: workOrder.id,
        request: {
          targetStatus: 7, // Cancelled
          reason: trimmedReason,
        },
      });
      setIsCancelConfirmOpen(false);
      setCancellationReason('');
      onUpdated?.(updated);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Cancellation failed';
      setCancelError(message);
    }
  };

  const handleTransition = async (targetStatus: number) => {
    if (targetStatus === 7) {
      handleOpenCancelDialog();
      return;
    }

    setActionError(null);
    try {
      const updated = await transitionMutation.mutateAsync({
        id: workOrder.id,
        request: {
          targetStatus,
          pauseReasonCode: targetStatus === 5 ? pauseReasonCode : null,
          pauseNote: targetStatus === 5 ? pauseNote : null,
          reason: reason ? reason.trim() : null,
        },
      });
      setReason('');
      setPauseNote('');
      onUpdated?.(updated);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Status transition failed';
      setActionError(message);
    }
  };

  const getPriorityStyle = (priority: string): React.CSSProperties => {
    const p = priority.toLowerCase();
    if (p === 'critical') return { backgroundColor: '#fee2e2', color: '#991b1b', borderColor: '#fca5a5' };
    if (p === 'high') return { backgroundColor: '#ffedd5', color: '#9a3412', borderColor: '#fdba74' };
    if (p === 'standard') return { backgroundColor: '#e0f2fe', color: '#0369a1', borderColor: '#bae6fd' };
    return { backgroundColor: '#f1f5f9', color: '#475569', borderColor: '#cbd5e1' };
  };

  return (
    <>
      <Modal
        isOpen={isOpen}
      onClose={onClose}
      title={`Work Order ${workOrder.workOrderNumber}`}
      subtitle={`${workOrder.workTypeCode} — ${workOrder.summary}`}
      size="lg"
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
        {actionError && (
          <div
            role="alert"
            style={{
              padding: '12px',
              backgroundColor: 'var(--danger-subtle)',
              border: '1px solid var(--danger)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--danger-text)',
              fontSize: '13px',
              display: 'flex',
              alignItems: 'center',
              gap: '8px',
            }}
          >
            <AlertCircle size={16} />
            <span>{actionError}</span>
          </div>
        )}

        {/* Metadata Grid */}
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr 1fr',
            gap: '14px',
            padding: '16px',
            backgroundColor: 'var(--bg-subtle)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <div>
            <span style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-muted)', fontWeight: 600 }}>
              Operational Status
            </span>
            <div style={{ marginTop: '4px' }}>
              <StatusBadge status={workOrder.operationalStatus} />
            </div>
          </div>

          <div>
            <span style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-muted)', fontWeight: 600 }}>
              Priority SLA
            </span>
            <div style={{ marginTop: '4px' }}>
              <span
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  padding: '2px 8px',
                  fontSize: '12px',
                  fontWeight: 600,
                  borderRadius: '9999px',
                  border: '1px solid',
                  ...getPriorityStyle(workOrder.priority),
                }}
              >
                {workOrder.priority}
              </span>
            </div>
          </div>

          <div>
            <span style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-muted)', fontWeight: 600 }}>
              Customer Account
            </span>
            <div style={{ fontWeight: 600, fontSize: '13px', marginTop: '4px', color: 'var(--text-primary)' }}>
              {workOrder.serviceAccountName}
            </div>
          </div>

          <div>
            <span style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-muted)', fontWeight: 600 }}>
              Operational Site
            </span>
            <div style={{ fontWeight: 500, fontSize: '13px', marginTop: '4px', color: 'var(--text-primary)' }}>
              {workOrder.primarySiteName}
            </div>
          </div>

          <div>
            <span style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-muted)', fontWeight: 600 }}>
              Created Timestamp
            </span>
            <div style={{ fontSize: '13px', marginTop: '4px', color: 'var(--text-secondary)' }}>
              {new Date(workOrder.createdAtUtc).toLocaleString()}
            </div>
          </div>

          <div>
            <span style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-muted)', fontWeight: 600 }}>
              Completed At
            </span>
            <div style={{ fontSize: '13px', marginTop: '4px', color: 'var(--text-secondary)' }}>
              {workOrder.operationallyCompletedAtUtc
                ? new Date(workOrder.operationallyCompletedAtUtc).toLocaleString()
                : 'Pending Execution'}
            </div>
          </div>
        </div>

        {/* Description & Assets */}
        {workOrder.description && (
          <div>
            <span style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)' }}>Scope Description:</span>
            <p style={{ margin: '4px 0 0', fontSize: '13px', color: 'var(--text-primary)' }}>{workOrder.description}</p>
          </div>
        )}

        {workOrder.pauseReasonCode && (
          <div
            style={{
              padding: '12px',
              backgroundColor: '#fef3c7',
              border: '1px solid #fde68a',
              borderRadius: 'var(--radius-md)',
              color: '#92400e',
              fontSize: '13px',
            }}
          >
            <strong>Paused Execution ({workOrder.pauseReasonCode}):</strong> {workOrder.pauseNote || 'No pause notes entered.'}
          </div>
        )}

        {/* Lifecycle Transitions Panel */}
        <div style={{ borderTop: '1px solid var(--border-subtle)', paddingTop: '16px' }}>
          <h4 style={{ fontSize: '14px', fontWeight: 600, color: 'var(--text-primary)', marginBottom: '12px' }}>
            Operational Lifecycle Actions
          </h4>

          {workOrder.operationalStatus === 'Draft' && (
            <div style={{ display: 'flex', gap: '10px' }}>
              <Button
                variant="primary"
                onClick={() => handleTransition(2)} // 2 = Approved
                disabled={transitionMutation.isPending}
              >
                <CheckCircle2 size={15} />
                Approve Work Order
              </Button>
              <Button
                variant="danger"
                onClick={handleOpenCancelDialog} // 7 = Cancelled
                disabled={transitionMutation.isPending}
              >
                <XCircle size={15} />
                Cancel Work Order
              </Button>
            </div>
          )}

          {workOrder.operationalStatus === 'Approved' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <div style={{ display: 'flex', gap: '10px' }}>
                <Button
                  variant="outline"
                  onClick={() => handleTransition(3)} // 3 = Scheduled
                  disabled={transitionMutation.isPending}
                >
                  <Clock size={15} />
                  Mark Scheduled
                </Button>
                <Button
                  variant="primary"
                  onClick={() => handleTransition(4)} // 4 = InProgress
                  disabled={transitionMutation.isPending}
                >
                  <PlayCircle size={15} />
                  Start Execution
                </Button>
                <Button
                  variant="danger"
                  onClick={handleOpenCancelDialog} // 7 = Cancelled
                  disabled={transitionMutation.isPending}
                >
                  <XCircle size={15} />
                  Cancel
                </Button>
              </div>
            </div>
          )}

          {workOrder.operationalStatus === 'Scheduled' && (
            <div style={{ display: 'flex', gap: '10px' }}>
              <Button
                variant="primary"
                onClick={() => handleTransition(4)} // 4 = InProgress
                disabled={transitionMutation.isPending}
              >
                <PlayCircle size={15} />
                Start Field Execution
              </Button>
              <Button
                variant="danger"
                onClick={handleOpenCancelDialog} // 7 = Cancelled
                disabled={transitionMutation.isPending}
              >
                <XCircle size={15} />
                Cancel
              </Button>
            </div>
          )}

          {workOrder.operationalStatus === 'InProgress' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <div style={{ display: 'grid', gridTemplateColumns: '200px 1fr', gap: '10px' }}>
                <Select
                  label="Pause Reason"
                  value={pauseReasonCode}
                  onChange={(e) => setPauseReasonCode(e.target.value)}
                  options={[
                    { value: 'WAIT_PARTS', label: 'Waiting for Parts' },
                    { value: 'WEATHER', label: 'Severe Weather' },
                    { value: 'ACCESS_DENIED', label: 'Site Access Delayed' },
                    { value: 'SAFETY_HOLD', label: 'Safety Hold' },
                    { value: 'CUSTOMER_REQUEST', label: 'Customer Requested Pause' },
                  ]}
                />
                <Input
                  label="Pause Note"
                  placeholder="Explain why work was temporarily suspended..."
                  value={pauseNote}
                  onChange={(e) => setPauseNote(e.target.value)}
                />
              </div>
              <div style={{ display: 'flex', gap: '10px' }}>
                <Button
                  variant="outline"
                  onClick={() => handleTransition(5)} // 5 = Paused
                  disabled={transitionMutation.isPending}
                >
                  <PauseCircle size={15} />
                  Pause Execution
                </Button>
                <Button
                  variant="primary"
                  onClick={() => handleTransition(6)} // 6 = OperationallyComplete
                  disabled={transitionMutation.isPending}
                >
                  <CheckCircle2 size={15} />
                  Complete Operationally
                </Button>
                <Button
                  variant="danger"
                  onClick={handleOpenCancelDialog} // 7 = Cancelled
                  disabled={transitionMutation.isPending}
                >
                  <XCircle size={15} />
                  Cancel
                </Button>
              </div>
            </div>
          )}

          {workOrder.operationalStatus === 'Paused' && (
            <div style={{ display: 'flex', gap: '10px' }}>
              <Button
                variant="primary"
                onClick={() => handleTransition(4)} // 4 = InProgress
                disabled={transitionMutation.isPending}
              >
                <PlayCircle size={15} />
                Resume Execution
              </Button>
              <Button
                variant="danger"
                onClick={handleOpenCancelDialog} // 7 = Cancelled
                disabled={transitionMutation.isPending}
              >
                <XCircle size={15} />
                Cancel Work Order
              </Button>
            </div>
          )}

          {workOrder.operationalStatus === 'OperationallyComplete' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              <Input
                label="Reopen Reason (Audit Mandatory)"
                placeholder="e.g. Defect recurrence, QA inspection failed..."
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
              <Button
                variant="outline"
                onClick={() => handleTransition(2)} // 2 = Approved
                disabled={transitionMutation.isPending || !reason.trim()}
              >
                Reopen Work Order for Rework
              </Button>
            </div>
          )}

          {workOrder.operationalStatus === 'Cancelled' && (
            <p style={{ fontSize: '13px', color: 'var(--text-muted)' }}>
              Work order is cancelled. No further operational transitions are permitted.
            </p>
          )}
        </div>

        {/* Status History Ledger */}
        <div style={{ borderTop: '1px solid var(--border-subtle)', paddingTop: '16px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '10px' }}>
            <History size={15} color="var(--text-secondary)" />
            <h4 style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>
              Audit Transition History ({workOrder.statusHistory.length})
            </h4>
          </div>
          <div style={{ overflowX: 'auto', maxHeight: '180px', overflowY: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '12px' }}>
              <thead>
                <tr style={{ backgroundColor: 'var(--bg-subtle)', textAlign: 'left' }}>
                  <th style={{ padding: '6px 10px', color: 'var(--text-secondary)' }}>From</th>
                  <th style={{ padding: '6px 10px', color: 'var(--text-secondary)' }}>To</th>
                  <th style={{ padding: '6px 10px', color: 'var(--text-secondary)' }}>Reason / Notes</th>
                  <th style={{ padding: '6px 10px', color: 'var(--text-secondary)' }}>Timestamp</th>
                </tr>
              </thead>
              <tbody>
                {workOrder.statusHistory.map((h) => (
                  <tr key={h.id} style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                    <td style={{ padding: '6px 10px' }}>
                      <StatusBadge status={h.fromStatus} />
                    </td>
                    <td style={{ padding: '6px 10px' }}>
                      <StatusBadge status={h.toStatus} />
                    </td>
                    <td style={{ padding: '6px 10px', color: 'var(--text-primary)' }}>
                      {h.reason || h.pauseReasonCode || '—'}
                    </td>
                    <td style={{ padding: '6px 10px', color: 'var(--text-muted)' }}>
                      {new Date(h.changedAtUtc).toLocaleString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '8px' }}>
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>
    </Modal>

    {/* Accessible Cancellation Confirmation Dialog */}
    <Modal
      isOpen={isCancelConfirmOpen}
      onClose={() => {
        if (!transitionMutation.isPending) {
          setIsCancelConfirmOpen(false);
          setCancelError(null);
        }
      }}
      title="Cancel Work Order"
      subtitle={`Confirm operational cancellation for ${workOrder.workOrderNumber}`}
      size="md"
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {cancelError && (
          <div
            role="alert"
            style={{
              padding: '12px',
              backgroundColor: 'var(--danger-subtle)',
              border: '1px solid var(--danger)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--danger-text)',
              fontSize: '13px',
              display: 'flex',
              alignItems: 'center',
              gap: '8px',
            }}
          >
            <AlertCircle size={16} />
            <span>{cancelError}</span>
          </div>
        )}

        <p style={{ margin: 0, fontSize: '13px', color: 'var(--text-secondary)' }}>
          Cancelling this work order will permanently transition it to a terminal cancelled state.
          A non-empty cancellation reason is strictly required for the compliance audit ledger.
        </p>

        <div>
          <label
            htmlFor="cancellation-reason-input"
            style={{ display: 'block', fontSize: '13px', fontWeight: 600, marginBottom: '6px', color: 'var(--text-primary)' }}
          >
            Cancellation Reason <span style={{ color: 'var(--danger)' }}>*</span>
          </label>
          <Input
            id="cancellation-reason-input"
            placeholder="e.g., Customer requested cancellation, duplicate request..."
            value={cancellationReason}
            onChange={(e) => {
              setCancellationReason(e.target.value);
              if (cancelError) setCancelError(null);
            }}
            required
            aria-required="true"
            autoFocus
          />
          {!cancellationReason.trim() && (
            <span style={{ fontSize: '12px', color: 'var(--danger)', marginTop: '4px', display: 'block' }}>
              Cancellation reason cannot be empty or whitespace.
            </span>
          )}
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '8px' }}>
          <Button
            variant="outline"
            onClick={() => {
              setIsCancelConfirmOpen(false);
              setCancelError(null);
            }}
            disabled={transitionMutation.isPending}
          >
            Dismiss
          </Button>
          <Button
            variant="danger"
            onClick={handleConfirmCancel}
            disabled={transitionMutation.isPending || !cancellationReason.trim()}
          >
            <XCircle size={15} />
            {transitionMutation.isPending ? 'Cancelling...' : 'Confirm Cancellation'}
          </Button>
        </div>
      </div>
    </Modal>
  </>
  );
};
