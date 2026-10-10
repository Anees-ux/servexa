import React, { useState } from 'react';
import {
  ListChecks,
  CheckCircle2,
  AlertTriangle,
  ShieldCheck,
  Plus,
  Play,
  SkipForward,
  Check,
  Loader2,
  Cpu,
} from 'lucide-react';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Modal } from '../../shared/design-system/Modal';
import { StatusBadge } from '../../shared/design-system/StatusBadge';
import {
  useWorkTasksQuery,
  useCreateWorkTaskMutation,
  useUpdateWorkTaskStatusMutation,
} from '../../shared/api/queries';
import type { WorkOrderDto, WorkTaskDto } from '../../shared/api/types';

interface WorkTasksSectionProps {
  workOrder: WorkOrderDto;
}

export const WorkTasksSection: React.FC<WorkTasksSectionProps> = ({ workOrder }) => {
  const { data: tasks = [], isLoading, error } = useWorkTasksQuery(workOrder.id);
  const createTaskMutation = useCreateWorkTaskMutation();
  const updateStatusMutation = useUpdateWorkTaskStatusMutation();

  // Add Task Modal State
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [taskType, setTaskType] = useState<number>(1);
  const [isRequired, setIsRequired] = useState(true);
  const [gate, setGate] = useState<number>(3);
  const [selectedAssetId, setSelectedAssetId] = useState<string>('');
  const [addError, setAddError] = useState<string | null>(null);

  // Skip Task Modal State
  const [skipTaskId, setSkipTaskId] = useState<string | null>(null);
  const [skipReason, setSkipReason] = useState('');
  const [skipError, setSkipError] = useState<string | null>(null);

  const isTerminal =
    workOrder.operationalStatus === 'OperationallyComplete' ||
    workOrder.operationalStatus === 'Cancelled';

  const handleCreateTask = async (e: React.FormEvent) => {
    e.preventDefault();
    setAddError(null);

    if (!title.trim()) {
      setAddError('Task title is required.');
      return;
    }

    try {
      await createTaskMutation.mutateAsync({
        workOrderId: workOrder.id,
        request: {
          title: title.trim(),
          description: description.trim() || null,
          taskType,
          sequence: tasks.length + 1,
          isRequired,
          gate,
          assetId: selectedAssetId || null,
        },
      });

      setTitle('');
      setDescription('');
      setTaskType(1);
      setIsRequired(true);
      setGate(3);
      setSelectedAssetId('');
      setIsAddModalOpen(false);
    } catch (err: unknown) {
      setAddError(err instanceof Error ? err.message : 'Failed to create task.');
    }
  };

  const handleStartTask = async (taskId: string) => {
    try {
      await updateStatusMutation.mutateAsync({
        workOrderId: workOrder.id,
        taskId,
        request: { status: 2 }, // InProgress
      });
    } catch (err: unknown) {
      console.error('Failed to start task', err);
    }
  };

  const handleCompleteTask = async (taskId: string) => {
    try {
      await updateStatusMutation.mutateAsync({
        workOrderId: workOrder.id,
        taskId,
        request: {
          status: 3, // Completed
        },
      });
    } catch (err: unknown) {
      console.error('Failed to complete task', err);
    }
  };

  const handleOpenSkipModal = (task: WorkTaskDto) => {
    setSkipTaskId(task.id);
    setSkipReason('');
    setSkipError(null);
  };

  const handleConfirmSkip = async () => {
    if (!skipTaskId) return;

    const task = tasks.find((t) => t.id === skipTaskId);
    if (task?.isRequired && !skipReason.trim()) {
      setSkipError('A valid skip reason is mandatory for required tasks (FIE-003).');
      return;
    }

    try {
      await updateStatusMutation.mutateAsync({
        workOrderId: workOrder.id,
        taskId: skipTaskId,
        request: {
          status: 4, // Skipped
          skipReason: skipReason.trim() || null,
        },
      });
      setSkipTaskId(null);
    } catch (err: unknown) {
      setSkipError(err instanceof Error ? err.message : 'Failed to skip task.');
    }
  };

  const getTaskTypeBadge = (type: string) => {
    switch (type) {
      case 'SafetyCheck':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-amber-500/10 text-amber-500 border border-amber-500/20">
            <ShieldCheck className="w-3 h-3" /> Safety Check
          </span>
        );
      case 'Inspection':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-sky-500/10 text-sky-500 border border-sky-500/20">
            <CheckCircle2 className="w-3 h-3" /> Inspection
          </span>
        );
      case 'ChecklistItem':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-purple-500/10 text-purple-500 border border-purple-500/20">
            <ListChecks className="w-3 h-3" /> Checklist
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-zinc-500/10 text-zinc-400 border border-zinc-500/20">
            Standard
          </span>
        );
    }
  };

  const getTaskStatusBadge = (status: string) => {
    switch (status) {
      case 'Completed':
        return <StatusBadge status="Completed" />;
      case 'InProgress':
        return <StatusBadge status="InProgress" />;
      case 'Skipped':
        return <StatusBadge status="Paused" />;
      default:
        return <StatusBadge status="Draft" />;
    }
  };

  return (
    <div className="space-y-4">
      {/* Header and Add Action */}
      <div className="flex items-center justify-between pb-2 border-b border-border">
        <div>
          <h3 className="text-sm font-semibold text-foreground flex items-center gap-2">
            <ListChecks className="w-4 h-4 text-primary" />
            Work Tasks & Checklists (FIE-003, FIE-011)
          </h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Operational tasks, safety inspections, and multi-asset checklist execution.
          </p>
        </div>
        {!isTerminal && (
          <Button
            size="sm"
            onClick={() => setIsAddModalOpen(true)}
            className="flex items-center gap-1.5"
          >
            <Plus className="w-3.5 h-3.5" /> Add Task
          </Button>
        )}
      </div>

      {/* Loading & Error states */}
      {isLoading && (
        <div className="flex items-center justify-center p-8 text-muted-foreground">
          <Loader2 className="w-5 h-5 animate-spin mr-2" />
          Loading work tasks...
        </div>
      )}

      {error && (
        <div className="p-3 bg-destructive/10 border border-destructive/20 rounded-md text-destructive text-sm flex items-center gap-2">
          <AlertTriangle className="w-4 h-4 shrink-0" />
          Failed to load work tasks: {error.message}
        </div>
      )}

      {/* Task List */}
      {!isLoading && tasks.length === 0 ? (
        <div className="text-center py-8 border border-dashed border-border rounded-lg bg-card/30">
          <ListChecks className="w-8 h-8 text-muted-foreground mx-auto mb-2 opacity-50" />
          <p className="text-sm font-medium text-foreground">No tasks or checklist items defined</p>
          <p className="text-xs text-muted-foreground mt-1 max-w-sm mx-auto">
            Add discrete standard tasks, safety protocols, or equipment inspections required for work order completion.
          </p>
          {!isTerminal && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsAddModalOpen(true)}
              className="mt-4"
            >
              <Plus className="w-3.5 h-3.5 mr-1" /> Create First Task
            </Button>
          )}
        </div>
      ) : (
        <div className="space-y-2">
          {tasks.map((task) => {
            const isCompleted = task.status === 'Completed';
            const isSkipped = task.status === 'Skipped';
            const isPending = task.status === 'Pending';
            const isInProgress = task.status === 'InProgress';

            return (
              <div
                key={task.id}
                className="p-3 bg-card border border-border rounded-lg flex flex-col md:flex-row md:items-center justify-between gap-3 hover:border-border/80 transition-colors"
              >
                <div className="space-y-1.5 flex-1 min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-xs font-mono font-semibold px-1.5 py-0.5 rounded bg-muted text-muted-foreground">
                      #{task.sequence}
                    </span>
                    <span className="text-sm font-medium text-foreground truncate">
                      {task.title}
                    </span>
                    {getTaskTypeBadge(task.taskType)}
                    {task.isRequired && (
                      <span className="text-xs px-1.5 py-0.2 rounded bg-rose-500/10 text-rose-500 border border-rose-500/20 font-medium">
                        Required
                      </span>
                    )}
                    {task.gate !== 'None' && (
                      <span className="text-xs px-1.5 py-0.2 rounded bg-amber-500/10 text-amber-500 border border-amber-500/20">
                        Gate: {task.gate}
                      </span>
                    )}
                    {getTaskStatusBadge(task.status)}
                  </div>

                  {task.description && (
                    <p className="text-xs text-muted-foreground line-clamp-2">
                      {task.description}
                    </p>
                  )}

                  <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground pt-0.5">
                    {task.assetId && (
                      <span className="inline-flex items-center gap-1 text-sky-400 font-mono">
                        <Cpu className="w-3 h-3" /> Asset: {task.assetId.substring(0, 8)}...
                      </span>
                    )}
                    {task.completedAtUtc && (
                      <span>Completed: {new Date(task.completedAtUtc).toLocaleTimeString()}</span>
                    )}
                    {task.skipReason && (
                      <span className="italic text-amber-400">
                        Reason: &quot;{task.skipReason}&quot;
                      </span>
                    )}
                  </div>
                </div>

                {/* Actions */}
                {!isTerminal && (
                  <div className="flex items-center gap-1.5 shrink-0">
                    {isPending && (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleStartTask(task.id)}
                        disabled={updateStatusMutation.isPending}
                        className="text-xs text-sky-400 hover:text-sky-300"
                        title="Start Task"
                      >
                        <Play className="w-3 h-3 mr-1" /> Start
                      </Button>
                    )}

                    {(isPending || isInProgress) && (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleCompleteTask(task.id)}
                        disabled={updateStatusMutation.isPending}
                        className="text-xs text-emerald-400 hover:text-emerald-300 border-emerald-500/30"
                        title="Complete Task"
                      >
                        <Check className="w-3 h-3 mr-1" /> Complete
                      </Button>
                    )}

                    {!isCompleted && !isSkipped && (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleOpenSkipModal(task)}
                        disabled={updateStatusMutation.isPending}
                        className="text-xs text-muted-foreground hover:text-foreground"
                        title="Skip Task"
                      >
                        <SkipForward className="w-3 h-3 mr-1" /> Skip
                      </Button>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Add Task Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        title="Add Work Task / Inspection"
      >
        <form onSubmit={handleCreateTask} className="space-y-4">
          {addError && (
            <div className="p-3 bg-destructive/10 border border-destructive/20 rounded-md text-destructive text-sm flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 shrink-0" />
              {addError}
            </div>
          )}

          <div>
            <label className="block text-xs font-medium text-foreground mb-1">
              Task Title <span className="text-destructive">*</span>
            </label>
            <Input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g., Verify compressor suction pressure"
              maxLength={200}
              required
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-foreground mb-1">
              Description / Instructions (Optional)
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Detailed instructions or acceptance thresholds..."
              maxLength={2000}
              rows={3}
              className="w-full px-3 py-2 bg-background border border-input rounded-md text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-foreground mb-1">
                Task Type
              </label>
              <select
                value={taskType}
                onChange={(e) => setTaskType(Number(e.target.value))}
                className="w-full px-3 py-2 bg-background border border-input rounded-md text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
              >
                <option value={1}>Standard Task</option>
                <option value={2}>Safety Check</option>
                <option value={3}>Inspection</option>
                <option value={4}>Checklist Item</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-medium text-foreground mb-1">
                Completion Gate
              </label>
              <select
                value={gate}
                onChange={(e) => setGate(Number(e.target.value))}
                className="w-full px-3 py-2 bg-background border border-input rounded-md text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
              >
                <option value={0}>None</option>
                <option value={1}>Assignment Completion</option>
                <option value={2}>Booking Completion</option>
                <option value={3}>Work Order Completion</option>
              </select>
            </div>
          </div>

          {/* FIE-011: Multi-asset attribution selector */}
          <div>
            <label className="block text-xs font-medium text-foreground mb-1">
              Asset Attribution (FIE-011 Scope)
            </label>
            <select
              value={selectedAssetId}
              onChange={(e) => setSelectedAssetId(e.target.value)}
              className="w-full px-3 py-2 bg-background border border-input rounded-md text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
            >
              <option value="">General Work Order Scope (No specific asset)</option>
              {workOrder.assets?.map((a) => (
                <option key={a.assetId} value={a.assetId}>
                  Asset #{a.assetId.substring(0, 8)} ({a.role})
                </option>
              ))}
            </select>
            <p className="text-xs text-muted-foreground mt-1">
              Attribute this inspection or checklist item to a specific asset within the work order.
            </p>
          </div>

          <div className="flex items-center gap-2 pt-1">
            <input
              type="checkbox"
              id="isRequiredTask"
              checked={isRequired}
              onChange={(e) => setIsRequired(e.target.checked)}
              className="rounded border-input text-primary focus:ring-ring"
            />
            <label htmlFor="isRequiredTask" className="text-sm text-foreground cursor-pointer">
              Mandatory task (completion gate blocks until executed or waived)
            </label>
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t border-border">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsAddModalOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={createTaskMutation.isPending}
            >
              {createTaskMutation.isPending ? 'Creating...' : 'Create Task'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Skip Task Modal */}
      <Modal
        isOpen={Boolean(skipTaskId)}
        onClose={() => setSkipTaskId(null)}
        title="Skip Work Task"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Provide a mandatory operational justification for skipping this task.
          </p>

          {skipError && (
            <div className="p-3 bg-destructive/10 border border-destructive/20 rounded-md text-destructive text-sm flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 shrink-0" />
              {skipError}
            </div>
          )}

          <div>
            <label className="block text-xs font-medium text-foreground mb-1">
              Skip Reason <span className="text-destructive">*</span>
            </label>
            <textarea
              value={skipReason}
              onChange={(e) => setSkipReason(e.target.value)}
              placeholder="e.g., Equipment inaccessible due to customer site safety lockout"
              maxLength={500}
              rows={3}
              className="w-full px-3 py-2 bg-background border border-input rounded-md text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
              required
            />
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t border-border">
            <Button
              type="button"
              variant="outline"
              onClick={() => setSkipTaskId(null)}
            >
              Cancel
            </Button>
            <Button
              variant="danger"
              onClick={handleConfirmSkip}
              disabled={updateStatusMutation.isPending}
            >
              {updateStatusMutation.isPending ? 'Skipping...' : 'Confirm Skip'}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
