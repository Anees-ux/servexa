import React, { useState } from 'react';
import { useCompleteExecutionMutation } from '../../shared/api/queries';

interface CompleteExecutionModalProps {
  isOpen: boolean;
  onClose: () => void;
  assignmentId: string;
  workOrderNumber: string;
}

export const CompleteExecutionModal: React.FC<CompleteExecutionModalProps> = ({
  isOpen,
  onClose,
  assignmentId,
  workOrderNumber,
}) => {
  const [workSummary, setWorkSummary] = useState('');
  const [error, setError] = useState<string | null>(null);

  const completeMutation = useCompleteExecutionMutation();

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!workSummary.trim()) {
      setError('A summary of work performed is required to complete field execution.');
      return;
    }

    setError(null);
    try {
      await completeMutation.mutateAsync({
        assignmentId,
        request: { workSummary: workSummary.trim() },
      });
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to complete execution session.';
      setError(message);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
      <div className="w-full max-w-lg bg-white dark:bg-neutral-900 border border-neutral-200 dark:border-neutral-800 rounded-lg shadow-xl overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        <div className="px-6 py-4 border-b border-neutral-200 dark:border-neutral-800 flex justify-between items-center">
          <div>
            <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-100">Complete Field Execution</h2>
            <p className="text-xs text-neutral-500 dark:text-neutral-400 mt-0.5">
              Work Order: <span className="font-mono font-medium">{workOrderNumber}</span>
            </p>
          </div>
          <button
            onClick={onClose}
            type="button"
            className="text-neutral-400 hover:text-neutral-600 dark:hover:text-neutral-200 text-xl font-medium focus:outline-none"
            aria-label="Close dialog"
          >
            ×
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="p-3 text-xs bg-red-50 dark:bg-red-950/30 text-red-700 dark:text-red-400 border border-red-200 dark:border-red-900 rounded-md">
              {error}
            </div>
          )}

          <div className="bg-emerald-50 dark:bg-emerald-950/20 border border-emerald-200 dark:border-emerald-900/40 rounded-md p-3 text-xs text-emerald-800 dark:text-emerald-300">
            <strong>Operational Gate:</strong> Completing this execution session closes the technician work interval and evaluates the Work Order for operational completion.
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300 mb-1">
              Work Summary & Observations <span className="text-red-500">*</span>
            </label>
            <textarea
              value={workSummary}
              onChange={(e) => setWorkSummary(e.target.value)}
              placeholder="Detail all corrective actions taken, parts replaced, diagnostic readings, and equipment status..."
              rows={4}
              required
              className="w-full text-sm rounded-md border border-neutral-300 dark:border-neutral-700 bg-white dark:bg-neutral-800 text-neutral-900 dark:text-neutral-100 p-2.5 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          <div className="pt-2 flex justify-end space-x-3">
            <button
              type="button"
              onClick={onClose}
              disabled={completeMutation.isPending}
              className="px-4 py-2 text-xs font-medium rounded-md border border-neutral-300 dark:border-neutral-700 bg-white dark:bg-neutral-800 text-neutral-700 dark:text-neutral-300 hover:bg-neutral-50 dark:hover:bg-neutral-700 focus:outline-none"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={completeMutation.isPending}
              className="px-4 py-2 text-xs font-medium rounded-md bg-emerald-600 text-white hover:bg-emerald-700 disabled:opacity-50 transition-colors focus:outline-none focus:ring-2 focus:ring-emerald-500 font-semibold"
            >
              {completeMutation.isPending ? 'Completing Work...' : 'Mark Execution Complete'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
