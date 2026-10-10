import React, { useState } from 'react';
import {
  ListChecks,
  CheckCircle2,
  ShieldCheck,
  Check,
  SkipForward,
  AlertTriangle,
  Cpu,
} from 'lucide-react';
import { Button } from '../../shared/design-system/Button';
import { Modal } from '../../shared/design-system/Modal';
import {
  useWorkTasksQuery,
  useUpdateWorkTaskStatusMutation,
} from '../../shared/api/queries';
import type { WorkTaskDto } from '../../shared/api/types';

interface TechnicianJobTasksProps {
  workOrderId: string;
}

export const TechnicianJobTasks: React.FC<TechnicianJobTasksProps> = ({ workOrderId }) => {
  const { data: tasks = [], isLoading } = useWorkTasksQuery(workOrderId);
  const updateStatusMutation = useUpdateWorkTaskStatusMutation();

  const [skipTaskId, setSkipTaskId] = useState<string | null>(null);
  const [skipReason, setSkipReason] = useState('');
  const [skipError, setSkipError] = useState<string | null>(null);

  const completedCount = tasks.filter((t) => t.status === 'Completed' || t.status === 'Skipped').length;
  const totalCount = tasks.length;
  const progressPercent = totalCount > 0 ? Math.round((completedCount / totalCount) * 100) : 0;

  const handleComplete = async (task: WorkTaskDto) => {
    try {
      await updateStatusMutation.mutateAsync({
        workOrderId,
        taskId: task.id,
        request: {
          status: 3, // Completed
        },
      });
    } catch (err) {
      console.error('Failed to complete task', err);
    }
  };

  const handleConfirmSkip = async () => {
    if (!skipTaskId) return;
    const task = tasks.find((t) => t.id === skipTaskId);
    if (task?.isRequired && !skipReason.trim()) {
      setSkipError('A justification is required to skip this task.');
      return;
    }

    try {
      await updateStatusMutation.mutateAsync({
        workOrderId,
        taskId: skipTaskId,
        request: {
          status: 4, // Skipped
          skipReason: skipReason.trim() || null,
        },
      });
      setSkipTaskId(null);
    } catch (err: unknown) {
      setSkipError(err instanceof Error ? err.message : 'Failed to skip task');
    }
  };

  if (isLoading) {
    return <div className="text-xs text-muted-foreground py-2">Loading tasks...</div>;
  }

  if (tasks.length === 0) {
    return null;
  }

  return (
    <div className="mt-3 pt-3 border-t border-border/60 space-y-2">
      <div className="flex items-center justify-between text-xs">
        <span className="font-semibold text-foreground flex items-center gap-1.5">
          <ListChecks className="w-3.5 h-3.5 text-primary" />
          Field Tasks & Inspections ({completedCount}/{totalCount})
        </span>
        <span className="text-muted-foreground font-mono">{progressPercent}% done</span>
      </div>

      {/* Progress Bar */}
      <div className="w-full bg-muted rounded-full h-1.5 overflow-hidden">
        <div
          className="bg-primary h-1.5 rounded-full transition-all duration-300"
          style={{ width: `${progressPercent}%` }}
        />
      </div>

      {/* Tasks List */}
      <div className="space-y-1.5 pt-1">
        {tasks.map((task) => {
          const isDone = task.status === 'Completed';
          const isSkipped = task.status === 'Skipped';

          return (
            <div
              key={task.id}
              className={`flex items-center justify-between p-2 rounded-md text-xs border transition-colors ${
                isDone
                  ? 'bg-emerald-500/5 border-emerald-500/20 text-muted-foreground'
                  : isSkipped
                  ? 'bg-muted/40 border-border/40 text-muted-foreground line-through'
                  : 'bg-card border-border text-foreground hover:border-border/80'
              }`}
            >
              <div className="flex items-center gap-2 min-w-0 flex-1">
                <span className="font-mono text-[10px] text-muted-foreground">#{task.sequence}</span>
                <span className={`truncate font-medium ${isDone ? 'line-through text-muted-foreground' : ''}`}>
                  {task.title}
                </span>

                {task.taskType === 'SafetyCheck' && (
                  <span className="inline-flex items-center gap-0.5 px-1 py-0.2 rounded text-[10px] bg-amber-500/10 text-amber-500">
                    <ShieldCheck className="w-2.5 h-2.5" /> Safety
                  </span>
                )}

                {task.taskType === 'Inspection' && (
                  <span className="inline-flex items-center gap-0.5 px-1 py-0.2 rounded text-[10px] bg-sky-500/10 text-sky-500">
                    <CheckCircle2 className="w-2.5 h-2.5" /> Inspection
                  </span>
                )}

                {task.assetId && (
                  <span className="inline-flex items-center gap-0.5 text-[10px] text-sky-400 font-mono">
                    <Cpu className="w-2.5 h-2.5" /> {task.assetId.substring(0, 6)}
                  </span>
                )}
              </div>

              {!isDone && !isSkipped && (
                <div className="flex items-center gap-1 shrink-0 ml-2">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => handleComplete(task)}
                    disabled={updateStatusMutation.isPending}
                    className="h-6 px-2 text-[11px] text-emerald-400 hover:text-emerald-300 border-emerald-500/30"
                  >
                    <Check className="w-3 h-3 mr-0.5" /> Done
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => {
                      setSkipTaskId(task.id);
                      setSkipReason('');
                      setSkipError(null);
                    }}
                    disabled={updateStatusMutation.isPending}
                    className="h-6 px-1.5 text-[11px] text-muted-foreground hover:text-foreground"
                  >
                    <SkipForward className="w-3 h-3" />
                  </Button>
                </div>
              )}

              {isDone && (
                <span className="text-[10px] text-emerald-500 font-medium inline-flex items-center gap-1">
                  <Check className="w-3 h-3" /> Done
                </span>
              )}

              {isSkipped && (
                <span className="text-[10px] text-muted-foreground italic">
                  Skipped
                </span>
              )}
            </div>
          );
        })}
      </div>

      {/* Skip Modal */}
      <Modal
        isOpen={Boolean(skipTaskId)}
        onClose={() => setSkipTaskId(null)}
        title="Skip Task"
      >
        <div className="space-y-3">
          {skipError && (
            <div className="p-2 bg-destructive/10 border border-destructive/20 rounded text-destructive text-xs flex items-center gap-1.5">
              <AlertTriangle className="w-3.5 h-3.5 shrink-0" />
              {skipError}
            </div>
          )}

          <div>
            <label className="block text-xs font-medium text-foreground mb-1">
              Reason for skipping task
            </label>
            <textarea
              value={skipReason}
              onChange={(e) => setSkipReason(e.target.value)}
              placeholder="e.g. Unit powered off, locked out..."
              rows={2}
              maxLength={500}
              className="w-full px-2.5 py-1.5 bg-background border border-input rounded text-xs text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button size="sm" variant="outline" onClick={() => setSkipTaskId(null)}>
              Cancel
            </Button>
            <Button size="sm" variant="danger" onClick={handleConfirmSkip}>
              Confirm Skip
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
