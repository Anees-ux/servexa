import React, { useState } from 'react';
import { AlertCircle, ClipboardList, Plus, RefreshCw, Search } from 'lucide-react';
import { useWorkOrdersQuery } from '../../shared/api/queries';
import type { WorkOrderDto } from '../../shared/api/types';
import { Button } from '../../shared/design-system/Button';
import { Card } from '../../shared/design-system/Card';
import { Input } from '../../shared/design-system/Input';
import { Select } from '../../shared/design-system/Select';
import { StatusBadge } from '../../shared/design-system/StatusBadge';
import { CreateWorkOrderModal } from './CreateWorkOrderModal';
import { WorkOrderDetailModal } from './WorkOrderDetailModal';

export const WorkOrdersView: React.FC = () => {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedWorkOrder, setSelectedWorkOrder] = useState<WorkOrderDto | null>(null);

  const { data, isLoading, error, refetch, isFetching } = useWorkOrdersQuery(
    undefined,
    undefined,
    statusFilter ? Number(statusFilter) : undefined,
    priorityFilter ? Number(priorityFilter) : undefined,
    search || undefined
  );

  const workOrders = data?.items ?? [];

  const getPriorityStyle = (priority: string): React.CSSProperties => {
    const p = priority.toLowerCase();
    if (p === 'critical') return { backgroundColor: '#fee2e2', color: '#991b1b', borderColor: '#fca5a5' };
    if (p === 'high') return { backgroundColor: '#ffedd5', color: '#9a3412', borderColor: '#fdba74' };
    if (p === 'standard') return { backgroundColor: '#e0f2fe', color: '#0369a1', borderColor: '#bae6fd' };
    return { backgroundColor: '#f1f5f9', color: '#475569', borderColor: '#cbd5e1' };
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Header Bar */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: '22px', fontWeight: 700, color: 'var(--text-primary)', margin: 0 }}>
            Work Orders
          </h1>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            Operational service commitments, operational lifecycles, and field dispatch execution.
          </p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
            aria-label="Refresh work orders"
          >
            <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
            Refresh
          </Button>
          <Button variant="primary" size="md" onClick={() => setIsCreateModalOpen(true)}>
            <Plus size={16} />
            New Work Order
          </Button>
        </div>
      </div>

      {/* Filter / Search Bar */}
      <Card>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 200px 180px', gap: '12px', alignItems: 'center' }}>
          <div style={{ position: 'relative' }}>
            <Search
              size={16}
              style={{
                position: 'absolute',
                left: '12px',
                top: '50%',
                transform: 'translateY(-50%)',
                color: 'var(--text-muted)',
              }}
            />
            <Input
              placeholder="Search by work order number or summary..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ paddingLeft: '36px' }}
            />
          </div>

          <Select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            options={[
              { value: '', label: 'All Operational Statuses' },
              { value: '1', label: 'Draft' },
              { value: '2', label: 'Approved' },
              { value: '3', label: 'Scheduled' },
              { value: '4', label: 'In Progress' },
              { value: '5', label: 'Paused' },
              { value: '6', label: 'Operationally Complete' },
              { value: '7', label: 'Cancelled' },
            ]}
          />

          <Select
            value={priorityFilter}
            onChange={(e) => setPriorityFilter(e.target.value)}
            options={[
              { value: '', label: 'All Priorities' },
              { value: '1', label: 'Low' },
              { value: '2', label: 'Standard' },
              { value: '3', label: 'High' },
              { value: '4', label: 'Critical' },
            ]}
          />
        </div>
      </Card>

      {/* Error State */}
      {error && (
        <div
          role="alert"
          style={{
            padding: '16px',
            backgroundColor: 'var(--danger-subtle)',
            border: '1px solid var(--danger)',
            borderRadius: 'var(--radius-lg)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: '12px',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <AlertCircle size={20} color="var(--danger)" />
            <div>
              <strong style={{ color: 'var(--danger-text)', fontSize: '14px' }}>Unable to load work orders</strong>
              <p style={{ color: 'var(--danger-text)', fontSize: '13px', margin: '2px 0 0' }}>{error.message}</p>
            </div>
          </div>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Retry
          </Button>
        </div>
      )}

      {/* Data Table */}
      <Card noPadding>
        {isLoading ? (
          <div style={{ padding: '60px', textAlign: 'center', color: 'var(--text-secondary)' }}>
            <RefreshCw size={24} className="animate-spin" style={{ margin: '0 auto 12px' }} />
            <p style={{ fontSize: '14px' }}>Loading work orders...</p>
          </div>
        ) : workOrders.length === 0 ? (
          <div style={{ padding: '60px 20px', textAlign: 'center' }}>
            <div
              style={{
                width: '48px',
                height: '48px',
                borderRadius: '50%',
                backgroundColor: 'var(--bg-subtle)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                margin: '0 auto 16px',
                color: 'var(--text-muted)',
              }}
            >
              <ClipboardList size={24} />
            </div>
            <h3 style={{ fontSize: '16px', fontWeight: 600, color: 'var(--text-primary)', margin: '0 0 6px' }}>
              No work orders found
            </h3>
            <p style={{ fontSize: '13px', color: 'var(--text-secondary)', maxWidth: '400px', margin: '0 auto 16px' }}>
              {search || statusFilter || priorityFilter
                ? 'Try adjusting your search criteria or filters.'
                : 'Create your first work order to start managing operational service commitments.'}
            </p>
            <Button variant="primary" size="sm" onClick={() => setIsCreateModalOpen(true)}>
              <Plus size={14} />
              Create Work Order
            </Button>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
              <thead>
                <tr style={{ backgroundColor: 'var(--bg-subtle)', borderBottom: '1px solid var(--border-subtle)' }}>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>WO Number</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Priority</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Summary</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Customer Account</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Operational Site</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Status</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)', textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {workOrders.map((wo) => (
                  <tr
                    key={wo.id}
                    style={{
                      borderBottom: '1px solid var(--border-subtle)',
                      transition: 'background-color 0.15s ease',
                    }}
                    onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'var(--bg-subtle)')}
                    onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                  >
                    <td style={{ padding: '14px 16px' }}>
                      <span
                        style={{
                          fontWeight: 700,
                          fontFamily: 'monospace',
                          fontSize: '13px',
                          color: 'var(--primary-text)',
                        }}
                      >
                        {wo.workOrderNumber}
                      </span>
                    </td>
                    <td style={{ padding: '14px 16px' }}>
                      <span
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          padding: '2px 8px',
                          fontSize: '12px',
                          fontWeight: 600,
                          borderRadius: '9999px',
                          border: '1px solid',
                          ...getPriorityStyle(wo.priority),
                        }}
                      >
                        {wo.priority}
                      </span>
                    </td>
                    <td style={{ padding: '14px 16px' }}>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{wo.summary}</div>
                      <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{wo.workTypeCode}</div>
                    </td>
                    <td style={{ padding: '14px 16px', color: 'var(--text-primary)' }}>
                      {wo.serviceAccountName || '—'}
                    </td>
                    <td style={{ padding: '14px 16px', color: 'var(--text-secondary)' }}>
                      {wo.primarySiteName || '—'}
                    </td>
                    <td style={{ padding: '14px 16px' }}>
                      <StatusBadge status={wo.operationalStatus} />
                    </td>
                    <td style={{ padding: '14px 16px', textAlign: 'right' }}>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setSelectedWorkOrder(wo)}
                      >
                        View & Manage
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Modals */}
      <CreateWorkOrderModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onCreated={() => refetch()}
      />

      <WorkOrderDetailModal
        workOrder={selectedWorkOrder}
        isOpen={Boolean(selectedWorkOrder)}
        onClose={() => setSelectedWorkOrder(null)}
        onUpdated={(updated) => {
          setSelectedWorkOrder(updated);
          refetch();
        }}
      />
    </div>
  );
};
