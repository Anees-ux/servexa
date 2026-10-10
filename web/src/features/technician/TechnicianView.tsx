import React, { useState } from 'react';
import {
  Wrench,
  Navigation,
  MapPin,
  Play,
  Pause,
  CheckCircle2,
  RefreshCw,
  Building,
  Clock,
  AlertCircle,
  FileText,
  Cpu,
} from 'lucide-react';
import { Card } from '../../shared/design-system/Card';
import { Button } from '../../shared/design-system/Button';
import { StatusBadge } from '../../shared/design-system/StatusBadge';
import {
  useMyAssignedJobsQuery,
  useStartTravelMutation,
  useMarkArrivedMutation,
  useStartWorkMutation,
  useResumeWorkMutation,
} from '../../shared/api/queries';
import { PauseWorkModal } from './PauseWorkModal';
import { CompleteExecutionModal } from './CompleteExecutionModal';
import type { AssignedJobDto } from '../../shared/api/types';

export const TechnicianView: React.FC = () => {
  const [filterTab, setFilterTab] = useState<'all' | 'active' | 'completed'>('active');
  const [actionError, setActionError] = useState<string | null>(null);

  const [pauseModalJob, setPauseModalJob] = useState<AssignedJobDto | null>(null);
  const [completeModalJob, setCompleteModalJob] = useState<AssignedJobDto | null>(null);

  const { data: jobs = [], isLoading, isError, error, refetch } = useMyAssignedJobsQuery();

  const startTravelMutation = useStartTravelMutation();
  const markArrivedMutation = useMarkArrivedMutation();
  const startWorkMutation = useStartWorkMutation();
  const resumeWorkMutation = useResumeWorkMutation();

  const activeJobs = jobs.filter(
    (j) => j.assignmentStatus.toLowerCase() !== 'completed' && j.assignmentStatus.toLowerCase() !== 'cancelled'
  );
  const completedJobs = jobs.filter((j) => j.assignmentStatus.toLowerCase() === 'completed');

  const filteredJobs = jobs.filter((j) => {
    if (filterTab === 'active') {
      return j.assignmentStatus.toLowerCase() !== 'completed' && j.assignmentStatus.toLowerCase() !== 'cancelled';
    }
    if (filterTab === 'completed') {
      return j.assignmentStatus.toLowerCase() === 'completed';
    }
    return true;
  });

  const handleStartTravel = async (job: AssignedJobDto) => {
    setActionError(null);
    try {
      await startTravelMutation.mutateAsync(job.assignmentId);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to record travel status.';
      setActionError(message);
    }
  };

  const handleMarkArrived = async (job: AssignedJobDto) => {
    setActionError(null);
    try {
      await markArrivedMutation.mutateAsync(job.assignmentId);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to record on-site arrival.';
      setActionError(message);
    }
  };

  const handleStartWork = async (job: AssignedJobDto) => {
    setActionError(null);
    try {
      await startWorkMutation.mutateAsync(job.assignmentId);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to start work execution.';
      setActionError(message);
    }
  };

  const handleResumeWork = async (job: AssignedJobDto) => {
    setActionError(null);
    try {
      await resumeWorkMutation.mutateAsync(job.assignmentId);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to resume work execution.';
      setActionError(message);
    }
  };

  const formatDateTime = (dateStr: string) => {
    try {
      return new Intl.DateTimeFormat('en-US', {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }).format(new Date(dateStr));
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 border-b border-neutral-200 dark:border-neutral-800 pb-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900 dark:text-neutral-100 flex items-center gap-2">
            <Wrench className="w-6 h-6 text-primary-600 dark:text-primary-400" />
            Field Execution Queue
          </h1>
          <p className="text-sm text-neutral-500 dark:text-neutral-400 mt-1">
            Assigned field jobs, real-time travel and on-site execution management.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isLoading}
            className="flex items-center gap-1.5"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </div>

      {actionError && (
        <div className="p-4 bg-red-50 dark:bg-red-950/30 border border-red-200 dark:border-red-900 rounded-md flex items-start gap-3">
          <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <h4 className="text-sm font-semibold text-red-800 dark:text-red-300">Action Failed</h4>
            <p className="text-xs text-red-700 dark:text-red-400 mt-0.5">{actionError}</p>
          </div>
          <button
            onClick={() => setActionError(null)}
            className="text-red-500 hover:text-red-700 text-xs font-semibold"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card className="p-4 border-l-4 border-l-blue-500">
          <div className="flex justify-between items-center">
            <span className="text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
              Active & Pending
            </span>
            <Clock className="w-4 h-4 text-blue-500" />
          </div>
          <div className="text-2xl font-bold text-neutral-900 dark:text-neutral-100 mt-2">
            {activeJobs.length}
          </div>
          <p className="text-xs text-neutral-500 dark:text-neutral-400 mt-1">
            Jobs ready or currently in progress
          </p>
        </Card>

        <Card className="p-4 border-l-4 border-l-amber-500">
          <div className="flex justify-between items-center">
            <span className="text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
              In Execution
            </span>
            <Play className="w-4 h-4 text-amber-500" />
          </div>
          <div className="text-2xl font-bold text-neutral-900 dark:text-neutral-100 mt-2">
            {jobs.filter((j) => j.assignmentStatus.toLowerCase() === 'inprogress').length}
          </div>
          <p className="text-xs text-neutral-500 dark:text-neutral-400 mt-1">
            Active work timer on site
          </p>
        </Card>

        <Card className="p-4 border-l-4 border-l-emerald-500">
          <div className="flex justify-between items-center">
            <span className="text-xs font-semibold uppercase tracking-wider text-neutral-500 dark:text-neutral-400">
              Completed
            </span>
            <CheckCircle2 className="w-4 h-4 text-emerald-500" />
          </div>
          <div className="text-2xl font-bold text-neutral-900 dark:text-neutral-100 mt-2">
            {completedJobs.length}
          </div>
          <p className="text-xs text-neutral-500 dark:text-neutral-400 mt-1">
            Fulfilled field service visits
          </p>
        </Card>
      </div>

      {/* Filter Tabs */}
      <div className="flex border-b border-neutral-200 dark:border-neutral-800">
        <button
          onClick={() => setFilterTab('active')}
          className={`px-4 py-2 text-xs font-semibold border-b-2 transition-colors ${
            filterTab === 'active'
              ? 'border-primary-600 text-primary-600 dark:border-primary-400 dark:text-primary-400'
              : 'border-transparent text-neutral-500 hover:text-neutral-700 dark:hover:text-neutral-300'
          }`}
        >
          Active Jobs ({activeJobs.length})
        </button>
        <button
          onClick={() => setFilterTab('completed')}
          className={`px-4 py-2 text-xs font-semibold border-b-2 transition-colors ${
            filterTab === 'completed'
              ? 'border-primary-600 text-primary-600 dark:border-primary-400 dark:text-primary-400'
              : 'border-transparent text-neutral-500 hover:text-neutral-700 dark:hover:text-neutral-300'
          }`}
        >
          Completed ({completedJobs.length})
        </button>
        <button
          onClick={() => setFilterTab('all')}
          className={`px-4 py-2 text-xs font-semibold border-b-2 transition-colors ${
            filterTab === 'all'
              ? 'border-primary-600 text-primary-600 dark:border-primary-400 dark:text-primary-400'
              : 'border-transparent text-neutral-500 hover:text-neutral-700 dark:hover:text-neutral-300'
          }`}
        >
          All ({jobs.length})
        </button>
      </div>

      {/* Main Jobs List */}
      {isLoading ? (
        <Card className="p-12 text-center text-neutral-500 dark:text-neutral-400">
          <RefreshCw className="w-6 h-6 animate-spin mx-auto mb-2 text-primary-600" />
          <p className="text-sm">Loading assigned field jobs...</p>
        </Card>
      ) : isError ? (
        <Card className="p-8 text-center border-red-200 dark:border-red-900/50 bg-red-50/50 dark:bg-red-950/20">
          <AlertCircle className="w-8 h-8 mx-auto text-red-500 mb-2" />
          <p className="text-sm font-medium text-red-800 dark:text-red-300">Failed to load assigned jobs</p>
          <p className="text-xs text-red-600 dark:text-red-400 mt-1">
            {error instanceof Error ? error.message : 'An error occurred'}
          </p>
          <Button variant="outline" size="sm" onClick={() => refetch()} className="mt-4">
            Try Again
          </Button>
        </Card>
      ) : filteredJobs.length === 0 ? (
        <Card className="p-12 text-center text-neutral-500 dark:text-neutral-400">
          <Wrench className="w-10 h-10 mx-auto mb-3 opacity-30" />
          <h3 className="text-base font-semibold text-neutral-800 dark:text-neutral-200">No Jobs Found</h3>
          <p className="text-xs mt-1">You have no jobs matching the selected filter.</p>
        </Card>
      ) : (
        <div className="space-y-4">
          {filteredJobs.map((job) => {
            const statusLower = job.assignmentStatus.toLowerCase();
            const sessionStatusLower = job.sessionStatus?.toLowerCase() || '';

            return (
              <Card
                key={job.assignmentId}
                className="p-5 border border-neutral-200 dark:border-neutral-800 hover:border-neutral-300 dark:hover:border-neutral-700 transition-all shadow-sm"
              >
                <div className="flex flex-col lg:flex-row justify-between items-start lg:items-center gap-4">
                  {/* Job Header & Details */}
                  <div className="space-y-2 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-mono text-xs font-bold px-2 py-0.5 bg-neutral-100 dark:bg-neutral-800 text-neutral-800 dark:text-neutral-200 rounded">
                        {job.workOrderNumber}
                      </span>
                      <span className="font-mono text-xs px-2 py-0.5 bg-neutral-50 dark:bg-neutral-850 text-neutral-600 dark:text-neutral-400 rounded border border-neutral-200 dark:border-neutral-800">
                        {job.bookingNumber}
                      </span>
                      <StatusBadge status={job.priority} />
                      <StatusBadge status={job.assignmentStatus} />
                      {job.sessionStatus && (
                        <span className="text-[10px] font-semibold uppercase px-2 py-0.5 rounded bg-purple-100 text-purple-800 dark:bg-purple-950/40 dark:text-purple-300 border border-purple-200 dark:border-purple-800">
                          Session: {job.sessionStatus}
                        </span>
                      )}
                    </div>

                    <h2 className="text-base font-bold text-neutral-900 dark:text-neutral-100">
                      {job.workOrderSummary}
                    </h2>

                    {job.workOrderDescription && (
                      <p className="text-xs text-neutral-600 dark:text-neutral-400 line-clamp-2">
                        {job.workOrderDescription}
                      </p>
                    )}

                    {/* Metadata Grid */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-2 pt-2 text-xs text-neutral-600 dark:text-neutral-400">
                      <div className="flex items-center gap-1.5">
                        <Building className="w-3.5 h-3.5 text-neutral-400 flex-shrink-0" />
                        <span className="truncate">{job.customerName || 'No Customer'}</span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <MapPin className="w-3.5 h-3.5 text-neutral-400 flex-shrink-0" />
                        <span className="truncate">{job.siteName}</span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Clock className="w-3.5 h-3.5 text-neutral-400 flex-shrink-0" />
                        <span>
                          {formatDateTime(job.plannedStartUtc)} – {formatDateTime(job.plannedEndUtc)}
                        </span>
                      </div>
                      {job.primaryAssetName && (
                        <div className="flex items-center gap-1.5 sm:col-span-2">
                          <Cpu className="w-3.5 h-3.5 text-neutral-400 flex-shrink-0" />
                          <span className="truncate font-medium text-neutral-700 dark:text-neutral-300">
                            Asset: {job.primaryAssetName}{' '}
                            {job.primaryAssetNumber && `(${job.primaryAssetNumber})`}
                          </span>
                        </div>
                      )}
                    </div>

                    {/* Work Summary if Completed */}
                    {job.workSummary && (
                      <div className="mt-3 p-3 bg-neutral-50 dark:bg-neutral-850 rounded-md border border-neutral-200 dark:border-neutral-800 text-xs">
                        <div className="flex items-center gap-1.5 font-semibold text-neutral-700 dark:text-neutral-300 mb-1">
                          <FileText className="w-3.5 h-3.5 text-emerald-600" />
                          Completed Work Summary:
                        </div>
                        <p className="text-neutral-600 dark:text-neutral-400 whitespace-pre-line">
                          {job.workSummary}
                        </p>
                      </div>
                    )}
                  </div>

                  {/* Operational Action Controls */}
                  <div className="flex flex-wrap sm:flex-nowrap items-center gap-2 lg:flex-col lg:items-end w-full lg:w-auto pt-2 lg:pt-0 border-t lg:border-t-0 border-neutral-200 dark:border-neutral-800">
                    {/* Stage 1: Dispatched / Assigned -> Start Travel */}
                    {(statusLower === 'assigned' || statusLower === 'accepted') && (
                      <>
                        <Button
                          size="sm"
                          onClick={() => handleStartTravel(job)}
                          disabled={startTravelMutation.isPending}
                          className="flex items-center gap-1.5 bg-blue-600 hover:bg-blue-700 text-white w-full sm:w-auto"
                        >
                          <Navigation className="w-3.5 h-3.5" />
                          {startTravelMutation.isPending ? 'Updating...' : 'Start Travel'}
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleMarkArrived(job)}
                          disabled={markArrivedMutation.isPending}
                          className="flex items-center gap-1.5 w-full sm:w-auto"
                        >
                          <MapPin className="w-3.5 h-3.5 text-neutral-500" />
                          Arrived On Site
                        </Button>
                      </>
                    )}

                    {/* Stage 2: En Route -> Mark Arrived */}
                    {statusLower === 'enroute' && (
                      <Button
                        size="sm"
                        onClick={() => handleMarkArrived(job)}
                        disabled={markArrivedMutation.isPending}
                        className="flex items-center gap-1.5 bg-amber-600 hover:bg-amber-700 text-white w-full sm:w-auto"
                      >
                        <MapPin className="w-3.5 h-3.5" />
                        {markArrivedMutation.isPending ? 'Updating...' : 'Arrived On Site'}
                      </Button>
                    )}

                    {/* Stage 3: On Site -> Start Work */}
                    {statusLower === 'onsite' && (
                      <Button
                        size="sm"
                        onClick={() => handleStartWork(job)}
                        disabled={startWorkMutation.isPending}
                        className="flex items-center gap-1.5 bg-emerald-600 hover:bg-emerald-700 text-white w-full sm:w-auto"
                      >
                        <Play className="w-3.5 h-3.5" />
                        {startWorkMutation.isPending ? 'Starting...' : 'Start Work'}
                      </Button>
                    )}

                    {/* Stage 4: In Progress -> Pause, Resume, or Complete */}
                    {statusLower === 'inprogress' && (
                      <div className="flex flex-wrap items-center gap-2 w-full lg:w-auto">
                        {sessionStatusLower === 'paused' ? (
                          <Button
                            size="sm"
                            onClick={() => handleResumeWork(job)}
                            disabled={resumeWorkMutation.isPending}
                            className="flex items-center gap-1.5 bg-emerald-600 hover:bg-emerald-700 text-white"
                          >
                            <Play className="w-3.5 h-3.5" />
                            {resumeWorkMutation.isPending ? 'Resuming...' : 'Resume Work'}
                          </Button>
                        ) : (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => setPauseModalJob(job)}
                            className="flex items-center gap-1.5 border-amber-300 dark:border-amber-700 text-amber-700 dark:text-amber-400 hover:bg-amber-50 dark:hover:bg-amber-950/20"
                          >
                            <Pause className="w-3.5 h-3.5" />
                            Pause Work
                          </Button>
                        )}

                        <Button
                          size="sm"
                          onClick={() => setCompleteModalJob(job)}
                          className="flex items-center gap-1.5 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold"
                        >
                          <CheckCircle2 className="w-3.5 h-3.5" />
                          Complete Execution
                        </Button>
                      </div>
                    )}

                    {/* Stage 5: Completed */}
                    {statusLower === 'completed' && (
                      <div className="flex items-center gap-1.5 text-xs font-semibold text-emerald-700 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/30 px-3 py-1.5 rounded border border-emerald-200 dark:border-emerald-800">
                        <CheckCircle2 className="w-4 h-4" />
                        Execution Fulfilled
                      </div>
                    )}
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}

      {/* Modals */}
      {pauseModalJob && (
        <PauseWorkModal
          isOpen={true}
          onClose={() => setPauseModalJob(null)}
          assignmentId={pauseModalJob.assignmentId}
          workOrderNumber={pauseModalJob.workOrderNumber}
        />
      )}

      {completeModalJob && (
        <CompleteExecutionModal
          isOpen={true}
          onClose={() => setCompleteModalJob(null)}
          assignmentId={completeModalJob.assignmentId}
          workOrderNumber={completeModalJob.workOrderNumber}
        />
      )}
    </div>
  );
};
