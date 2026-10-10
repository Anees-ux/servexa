import React, { useState } from 'react';
import { usePauseWorkMutation } from '../../shared/api/queries';

interface PauseWorkModalProps {
  isOpen: boolean;
  onClose: () => void;
  assignmentId: string;
  workOrderNumber: string;
}

export const PauseWorkModal: React.FC<PauseWorkModalProps> = ({
  isOpen,
  onClose,
  assignmentId,
  workOrderNumber,
}) => {
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);

  const pauseMutation = usePauseWorkMutation();

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) {
      setError('Please provide a reason for pausing work (e.g. waiting for parts, site access, meal break).');
      return;
    }

    setError(null);
    try {
      await pauseMutation.mutateAsync({
        assignmentId,
        request: { reason: reason.trim() },
      });
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to pause work session.';
      setError(message);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
      <div className="w-full max-w-md bg-white dark:bg-neutral-900 border border-neutral-200 dark:border-neutral-800 rounded-lg shadow-xl overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        <div className="px-6 py-4 border-b border-neutral-200 dark:border-neutral-800 flex justify-between items-center">
          <div>
            <h2 className="text-lg font-semibold text-neutral-900 dark:text-neutral-100">Pause Field Work</h2>
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

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-neutral-700 dark:text-neutral-300 mb-1">
              Pause Reason <span className="text-red-500">*</span>
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Explain why work is being paused (e.g., waiting for replacement valve, customer meeting)..."
              rows={3}
              required
              className="w-full text-sm rounded-md border border-neutral-300 dark:border-neutral-700 bg-white dark:bg-neutral-800 text-neutral-900 dark:text-neutral-100 p-2.5 focus:outline-none focus:ring-2 focus:ring-amber-500"
            />
          </div>

          <div className="pt-2 flex justify-end space-x-3">
            <button
              type="button"
              onClick={onClose}
              disabled={pauseMutation.isPending}
              className="px-4 py-2 text-xs font-medium rounded-md border border-neutral-300 dark:border-neutral-700 bg-white dark:bg-neutral-800 text-neutral-700 dark:text-neutral-300 hover:bg-neutral-50 dark:hover:bg-neutral-700 focus:outline-none"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={pauseMutation.isPending}
              className="px-4 py-2 text-xs font-medium rounded-md bg-amber-600 text-white hover:bg-amber-700 disabled:opacity-50 transition-colors focus:outline-none focus:ring-2 focus:ring-amber-500"
            >
              {pauseMutation.isPending ? 'Pausing Work...' : 'Confirm Pause'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
