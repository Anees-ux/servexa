import React, { useState } from 'react';
import {
  CheckCircle2,
  XCircle,
  AlertTriangle,
  ClipboardList,
  ShieldAlert,
  History,
  Plus,
  Loader2,
  FileCheck2,
} from 'lucide-react';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Modal } from '../../shared/design-system/Modal';
import { StatusBadge } from '../../shared/design-system/StatusBadge';
import {
  useWorkOrderCompletionEvaluationQuery,
  useWorkOrderCompletionHistoryQuery,
  useCompleteWorkOrderMutation,
  useAddScopeItemMutation,
  useUpdateScopeItemStatusMutation,
} from '../../shared/api/queries';
import type { WorkOrderDto, CompletionGateResultDto } from '../../shared/api/types';

interface WorkOrderCompletionSectionProps {
  workOrder: WorkOrderDto;
  onWorkOrderUpdated: (updated: WorkOrderDto) => void;
}

export const WorkOrderCompletionSection: React.FC<WorkOrderCompletionSectionProps> = ({
  workOrder,
  onWorkOrderUpdated,
}) => {
  const [isCompleteModalOpen, setIsCompleteModalOpen] = useState(false);
  const [completionNotes, setCompletionNotes] = useState('');
  const [completionError, setCompletionError] = useState<string | null>(null);

  // Add scope item state
  const [isAddScopeOpen, setIsAddScopeOpen] = useState(false);
  const [newScopeDesc, setNewScopeDesc] = useState('');
  const [newScopeRequired, setNewScopeRequired] = useState(true);
  const [scopeError, setScopeError] = useState<string | null>(null);

  const { data: readiness, isLoading: isEvaluating } = useWorkOrderCompletionEvaluationQuery(
    workOrder.id,
    workOrder.operationalStatus !== 'Cancelled'
  );

  const { data: evaluationHistory } = useWorkOrderCompletionHistoryQuery(
    workOrder.id,
    workOrder.operationalStatus !== 'Cancelled'
  );

  const completeMutation = useCompleteWorkOrderMutation();
  const addScopeMutation = useAddScopeItemMutation();
  const updateScopeStatusMutation = useUpdateScopeItemStatusMutation();

  const handleOpenCompleteModal = () => {
    setCompletionError(null);
    setCompletionNotes('');
    setIsCompleteModalOpen(true);
  };

  const handleConfirmCompletion = async () => {
    setCompletionError(null);
    try {
      const updated = await completeMutation.mutateAsync({
        id: workOrder.id,
        request: {
          notes: completionNotes.trim() ? completionNotes.trim() : null,
        },
      });
      setIsCompleteModalOpen(false);
      onWorkOrderUpdated(updated);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to complete work order';
      setCompletionError(msg);
    }
  };

  const handleAddScopeItem = async (e: React.FormEvent) => {
    e.preventDefault();
    setScopeError(null);
    if (!newScopeDesc.trim()) {
      setScopeError('Scope item description is required.');
      return;
    }

    try {
      const nextSeq = (readiness?.scopeItems?.length ?? 0) + 1;
      await addScopeMutation.mutateAsync({
        id: workOrder.id,
        request: {
          description: newScopeDesc.trim(),
          sequence: nextSeq,
          isRequiredForCompletion: newScopeRequired,
        },
      });
      setNewScopeDesc('');
      setIsAddScopeOpen(false);
    } catch (err: unknown) {
      setScopeError(err instanceof Error ? err.message : 'Failed to add scope item');
    }
  };

  const handleUpdateScopeStatus = async (itemId: string, status: number) => {
    try {
      await updateScopeStatusMutation.mutateAsync({
        id: workOrder.id,
        itemId,
        request: { status },
      });
    } catch (err: unknown) {
      console.error('Failed to update scope status', err);
    }
  };

  const isCompleteEligible = readiness?.isCompleteEligible ?? false;
  const gates = readiness?.gates ?? [];
  const scopeItems = readiness?.scopeItems ?? [];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Completion Gates & Readiness Box */}
      <div
        style={{
          border: '1px solid var(--border-subtle)',
          borderRadius: 'var(--radius-md)',
          padding: '16px',
          backgroundColor: 'var(--bg-subtle)',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <FileCheck2 size={16} color="var(--primary)" />
            <h4 style={{ margin: 0, fontSize: '14px', fontWeight: 600, color: 'var(--text-primary)' }}>
              Operational Completion Gates & Readiness
            </h4>
          </div>
          {isEvaluating ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px', color: 'var(--text-muted)' }}>
              <Loader2 size={14} className="animate-spin" />
              Evaluating...
            </div>
          ) : (
            <div
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '6px',
                padding: '3px 10px',
                borderRadius: '9999px',
                fontSize: '12px',
                fontWeight: 600,
                backgroundColor: isCompleteEligible ? 'rgba(34, 197, 94, 0.15)' : 'rgba(239, 68, 68, 0.12)',
                color: isCompleteEligible ? 'var(--success)' : 'var(--danger)',
                border: `1px solid ${isCompleteEligible ? 'rgba(34, 197, 94, 0.3)' : 'rgba(239, 68, 68, 0.3)'}`,
              }}
            >
              {isCompleteEligible ? <CheckCircle2 size={13} /> : <AlertTriangle size={13} />}
              {isCompleteEligible ? 'Ready for Operational Completion' : 'Completion Blocked / Follow-up Required'}
            </div>
          )}
        </div>

        {/* Gate Checklist */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {gates.map((gate: CompletionGateResultDto) => (
            <div
              key={gate.gateName}
              style={{
                display: 'flex',
                alignItems: 'flex-start',
                gap: '10px',
                padding: '8px 12px',
                backgroundColor: gate.passed ? 'rgba(34, 197, 94, 0.05)' : 'rgba(239, 68, 68, 0.06)',
                borderRadius: 'var(--radius-sm)',
                borderLeft: `3px solid ${gate.passed ? 'var(--success)' : 'var(--danger)'}`,
              }}
            >
              <div style={{ marginTop: '2px' }}>
                {gate.passed ? (
                  <CheckCircle2 size={15} color="var(--success)" />
                ) : (
                  <XCircle size={15} color="var(--danger)" />
                )}
              </div>
              <div style={{ flex: 1, fontSize: '12px' }}>
                <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>
                  {gate.gateName}
                </div>
                <div style={{ color: 'var(--text-secondary)', marginTop: '1px' }}>
                  {gate.description}
                </div>
                {gate.blockingReason && (
                  <div
                    style={{
                      marginTop: '4px',
                      color: 'var(--danger-text)',
                      fontWeight: 500,
                      backgroundColor: 'rgba(239, 68, 68, 0.08)',
                      padding: '4px 8px',
                      borderRadius: '4px',
                    }}
                  >
                    Blocker: {gate.blockingReason}
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>

        {/* Action button if eligible or in progress */}
        {workOrder.operationalStatus !== 'OperationallyComplete' &&
          workOrder.operationalStatus !== 'Cancelled' && (
            <div style={{ marginTop: '14px', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
              <Button
                variant={isCompleteEligible ? 'primary' : 'outline'}
                onClick={handleOpenCompleteModal}
                disabled={!isCompleteEligible}
              >
                <CheckCircle2 size={15} />
                Complete Operationally
              </Button>
            </div>
          )}
      </div>

      {/* Authorized Operational Scope Items */}
      <div
        style={{
          border: '1px solid var(--border-subtle)',
          borderRadius: 'var(--radius-md)',
          padding: '16px',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <ClipboardList size={16} color="var(--primary)" />
            <h4 style={{ margin: 0, fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)' }}>
              Authorized Operational Scope Items ({scopeItems.length})
            </h4>
          </div>
          {workOrder.operationalStatus !== 'OperationallyComplete' &&
            workOrder.operationalStatus !== 'Cancelled' && (
              <Button size="sm" variant="outline" onClick={() => setIsAddScopeOpen(true)}>
                <Plus size={13} />
                Add Scope Item
              </Button>
            )}
        </div>

        {scopeItems.length === 0 ? (
          <p style={{ fontSize: '12px', color: 'var(--text-muted)', margin: 0 }}>
            No specific gated scope items registered. Baseline work order scope applies.
          </p>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '12px' }}>
              <thead>
                <tr style={{ backgroundColor: 'var(--bg-subtle)', textAlign: 'left' }}>
                  <th style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>Seq</th>
                  <th style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>Description</th>
                  <th style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>Type</th>
                  <th style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>Gated</th>
                  <th style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>Status</th>
                  {workOrder.operationalStatus !== 'OperationallyComplete' && (
                    <th style={{ padding: '6px 8px', color: 'var(--text-secondary)', textAlign: 'right' }}>Actions</th>
                  )}
                </tr>
              </thead>
              <tbody>
                {scopeItems.map((item) => (
                  <tr key={item.id} style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                    <td style={{ padding: '6px 8px', fontWeight: 600 }}>#{item.sequence}</td>
                    <td style={{ padding: '6px 8px', color: 'var(--text-primary)' }}>{item.description}</td>
                    <td style={{ padding: '6px 8px', color: 'var(--text-muted)' }}>{item.scopeType}</td>
                    <td style={{ padding: '6px 8px' }}>
                      {item.isRequiredForCompletion ? (
                        <span style={{ fontSize: '11px', color: 'var(--danger)', fontWeight: 600 }}>Required</span>
                      ) : (
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>Optional</span>
                      )}
                    </td>
                    <td style={{ padding: '6px 8px' }}>
                      <StatusBadge status={item.status} />
                    </td>
                    {workOrder.operationalStatus !== 'OperationallyComplete' && (
                      <td style={{ padding: '6px 8px', textAlign: 'right' }}>
                        {item.status === 'Authorized' && (
                          <div style={{ display: 'inline-flex', gap: '6px' }}>
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => handleUpdateScopeStatus(item.id, 2)} // 2 = Fulfilled
                              disabled={updateScopeStatusMutation.isPending}
                            >
                              Fulfill
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => handleUpdateScopeStatus(item.id, 3)} // 3 = NotRequired
                              disabled={updateScopeStatusMutation.isPending}
                            >
                              Waive
                            </Button>
                          </div>
                        )}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Append-Only Evaluation Ledger History */}
      {evaluationHistory && evaluationHistory.length > 0 && (
        <div
          style={{
            border: '1px solid var(--border-subtle)',
            borderRadius: 'var(--radius-md)',
            padding: '16px',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '10px' }}>
            <History size={15} color="var(--text-secondary)" />
            <h4 style={{ margin: 0, fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)' }}>
              Append-Only Completion Evaluation Ledger ({evaluationHistory.length})
            </h4>
          </div>
          <div style={{ overflowX: 'auto', maxHeight: '160px', overflowY: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '12px' }}>
              <thead>
                <tr style={{ backgroundColor: 'var(--bg-subtle)', textAlign: 'left' }}>
                  <th style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>Evaluated At</th>
                  <th style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>Outcome</th>
                  <th style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>Summary</th>
                  <th style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>Notes</th>
                </tr>
              </thead>
              <tbody>
                {evaluationHistory.map((ev) => (
                  <tr key={ev.id} style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                    <td style={{ padding: '6px 8px', color: 'var(--text-muted)' }}>
                      {new Date(ev.evaluatedAtUtc).toLocaleString()}
                    </td>
                    <td style={{ padding: '6px 8px' }}>
                      <span
                        style={{
                          fontSize: '11px',
                          fontWeight: 600,
                          padding: '2px 6px',
                          borderRadius: '4px',
                          backgroundColor:
                            ev.outcome === 'OperationallyComplete'
                              ? 'rgba(34, 197, 94, 0.15)'
                              : 'rgba(239, 68, 68, 0.15)',
                          color:
                            ev.outcome === 'OperationallyComplete'
                              ? 'var(--success)'
                              : 'var(--danger)',
                        }}
                      >
                        {ev.outcome}
                      </span>
                    </td>
                    <td style={{ padding: '6px 8px', color: 'var(--text-primary)' }}>{ev.summary || '—'}</td>
                    <td style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>{ev.notes || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Completion Confirmation Modal */}
      <Modal
        isOpen={isCompleteModalOpen}
        onClose={() => {
          if (!completeMutation.isPending) setIsCompleteModalOpen(false);
        }}
        title="Authorized Work Order Completion"
        subtitle={`Verify completion gates and record operational signoff for ${workOrder.workOrderNumber}`}
        size="md"
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          {completionError && (
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
              <ShieldAlert size={16} />
              <span>{completionError}</span>
            </div>
          )}

          <div
            style={{
              padding: '12px',
              backgroundColor: 'rgba(34, 197, 94, 0.08)',
              border: '1px solid rgba(34, 197, 94, 0.25)',
              borderRadius: 'var(--radius-md)',
              fontSize: '13px',
              color: 'var(--text-primary)',
            }}
          >
            <strong>All {gates.length} operational completion gates satisfied:</strong>
            <ul style={{ margin: '6px 0 0', paddingLeft: '20px', fontSize: '12px', color: 'var(--text-secondary)' }}>
              <li>All scheduled visits completed ({readiness?.completedBookingsCount}/{readiness?.totalBookingsCount})</li>
              <li>Technician execution evidence recorded ({readiness?.completedExecutionSessionsCount} completed session(s))</li>
              <li>Authorized scope items fulfilled or cleared</li>
              <li>Operational status eligibility verified</li>
            </ul>
          </div>

          <div>
            <label
              htmlFor="completion-notes"
              style={{ display: 'block', fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px' }}
            >
              Operational Completion Notes (Optional)
            </label>
            <textarea
              id="completion-notes"
              rows={3}
              maxLength={2000}
              placeholder="Enter signoff notes or closure rationale..."
              value={completionNotes}
              onChange={(e) => setCompletionNotes(e.target.value)}
              style={{
                width: '100%',
                padding: '8px 12px',
                borderRadius: 'var(--radius-md)',
                border: '1px solid var(--border)',
                backgroundColor: 'var(--bg-input)',
                color: 'var(--text-primary)',
                fontSize: '13px',
                resize: 'vertical',
              }}
            />
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
            <Button
              variant="outline"
              onClick={() => setIsCompleteModalOpen(false)}
              disabled={completeMutation.isPending}
            >
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={handleConfirmCompletion}
              disabled={completeMutation.isPending || !isCompleteEligible}
            >
              {completeMutation.isPending ? (
                <>
                  <Loader2 size={15} className="animate-spin" />
                  Recording Closure...
                </>
              ) : (
                <>
                  <CheckCircle2 size={15} />
                  Authorize Operational Completion
                </>
              )}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Add Scope Item Modal */}
      <Modal
        isOpen={isAddScopeOpen}
        onClose={() => setIsAddScopeOpen(false)}
        title="Add Authorized Scope Item"
        subtitle={`Add an authorized operational task/requirement to ${workOrder.workOrderNumber}`}
        size="md"
      >
        <form onSubmit={handleAddScopeItem} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {scopeError && (
            <div
              role="alert"
              style={{
                padding: '10px',
                backgroundColor: 'var(--danger-subtle)',
                color: 'var(--danger-text)',
                borderRadius: 'var(--radius-md)',
                fontSize: '13px',
              }}
            >
              {scopeError}
            </div>
          )}

          <Input
            label="Scope Item Description"
            placeholder="e.g. Calibrate pressure transducer, inspect seals..."
            maxLength={500}
            value={newScopeDesc}
            onChange={(e) => setNewScopeDesc(e.target.value)}
            required
          />

          <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={newScopeRequired}
              onChange={(e) => setNewScopeRequired(e.target.checked)}
            />
            <span>Required for operational completion (Gated)</span>
          </label>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '10px' }}>
            <Button variant="outline" type="button" onClick={() => setIsAddScopeOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit" disabled={addScopeMutation.isPending}>
              {addScopeMutation.isPending ? 'Adding...' : 'Add Scope Item'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
