import React, { useState } from 'react';
import { Send, UserPlus, UserMinus, RefreshCw, AlertCircle, Building, MapPin, Clock } from 'lucide-react';
import { Card } from '../../shared/design-system/Card';
import { Button } from '../../shared/design-system/Button';
import { StatusBadge } from '../../shared/design-system/StatusBadge';
import {
  useBookingsQuery,
  useDispatchBookingMutation,
  useUnassignResourceMutation,
} from '../../shared/api/queries';
import { AssignTechnicianModal } from './AssignTechnicianModal';
import { ApiError } from '../../shared/api/apiClient';
import type { BookingDto } from '../../shared/api/types';

export const DispatchView: React.FC = () => {
  const [selectedBookingForAssign, setSelectedBookingForAssign] = useState<BookingDto | null>(null);
  const [filterTab, setFilterTab] = useState<'all' | 'unassigned' | 'assigned' | 'dispatched'>('all');
  const [actionError, setActionError] = useState<string | null>(null);

  const { data: bookingsData, isLoading, isError, error, refetch } = useBookingsQuery(
    undefined,
    undefined,
    undefined,
    undefined,
    undefined,
    undefined,
    1,
    50
  );

  const dispatchMutation = useDispatchBookingMutation();
  const unassignMutation = useUnassignResourceMutation();

  const bookings = bookingsData?.items || [];

  const unassignedCount = bookings.filter(
    (b) => b.dispatchStatus.toLowerCase() === 'unassigned' && b.status.toLowerCase() !== 'cancelled'
  ).length;

  const assignedCount = bookings.filter(
    (b) => b.dispatchStatus.toLowerCase() === 'assigned' && b.status.toLowerCase() !== 'cancelled'
  ).length;

  const dispatchedCount = bookings.filter(
    (b) => b.dispatchStatus.toLowerCase() === 'dispatched' || b.dispatchStatus.toLowerCase() === 'enroute'
  ).length;

  const filteredBookings = bookings.filter((b) => {
    if (b.status.toLowerCase() === 'cancelled') return filterTab === 'all';
    if (filterTab === 'unassigned') return b.dispatchStatus.toLowerCase() === 'unassigned';
    if (filterTab === 'assigned') return b.dispatchStatus.toLowerCase() === 'assigned';
    if (filterTab === 'dispatched') return b.dispatchStatus.toLowerCase() === 'dispatched' || b.dispatchStatus.toLowerCase() === 'enroute';
    return true;
  });

  const handleDispatch = async (booking: BookingDto) => {
    setActionError(null);
    try {
      await dispatchMutation.mutateAsync(booking.id);
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setActionError(err.problem?.detail || err.message);
      } else {
        setActionError((err as Error).message || 'Failed to dispatch booking.');
      }
    }
  };

  const handleUnassign = async (bookingId: string, assignmentId: string) => {
    const reason = window.prompt('Please provide a reason for unassigning this technician:');
    if (!reason || !reason.trim()) return;

    setActionError(null);
    try {
      await unassignMutation.mutateAsync({
        id: bookingId,
        assignmentId,
        reason: reason.trim(),
      });
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setActionError(err.problem?.detail || err.message);
      } else {
        setActionError((err as Error).message || 'Failed to unassign technician.');
      }
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Workspace Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: '24px', fontWeight: 700, margin: '0 0 6px 0', color: 'var(--text-primary)' }}>
            Dispatch & Resource Assignment Board
          </h1>
          <p style={{ margin: 0, color: 'var(--text-secondary)', fontSize: '14px' }}>
            Operational dispatch queue, technician capacity allocation, and field dispatching.
          </p>
        </div>
        <Button variant="outline" onClick={() => refetch()} disabled={isLoading}>
          <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
          Refresh Queue
        </Button>
      </div>

      {actionError && (
        <div
          style={{
            padding: '12px 16px',
            backgroundColor: '#fef2f2',
            color: '#991b1b',
            borderRadius: '6px',
            border: '1px solid #fecaca',
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

      {/* Metrics Bar */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '16px' }}>
        <Card>
          <div style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 500 }}>Total Queue Visits</div>
          <div style={{ fontSize: '28px', fontWeight: 700, marginTop: '4px', color: 'var(--text-primary)' }}>
            {bookings.length}
          </div>
        </Card>
        <Card>
          <div style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 500 }}>Unassigned (Action Req.)</div>
          <div style={{ fontSize: '28px', fontWeight: 700, marginTop: '4px', color: '#9f1239' }}>
            {unassignedCount}
          </div>
        </Card>
        <Card>
          <div style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 500 }}>Assigned (Ready to Dispatch)</div>
          <div style={{ fontSize: '28px', fontWeight: 700, marginTop: '4px', color: '#5b21b6' }}>
            {assignedCount}
          </div>
        </Card>
        <Card>
          <div style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 500 }}>Dispatched to Field</div>
          <div style={{ fontSize: '28px', fontWeight: 700, marginTop: '4px', color: '#166534' }}>
            {dispatchedCount}
          </div>
        </Card>
      </div>

      {/* Filter Tabs */}
      <div style={{ display: 'flex', gap: '8px', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '12px' }}>
        {[
          { key: 'all', label: `All Visits (${bookings.length})` },
          { key: 'unassigned', label: `Unassigned (${unassignedCount})` },
          { key: 'assigned', label: `Ready for Dispatch (${assignedCount})` },
          { key: 'dispatched', label: `Dispatched (${dispatchedCount})` },
        ].map((tab) => (
          <button
            key={tab.key}
            onClick={() => setFilterTab(tab.key as typeof filterTab)}
            style={{
              padding: '6px 14px',
              borderRadius: '6px',
              border: 'none',
              backgroundColor: filterTab === tab.key ? 'var(--primary-subtle)' : 'transparent',
              color: filterTab === tab.key ? 'var(--primary-text)' : 'var(--text-secondary)',
              fontWeight: filterTab === tab.key ? 600 : 500,
              fontSize: '13px',
              cursor: 'pointer',
            }}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Queue Table */}
      <Card>
        {isLoading && (
          <div style={{ padding: '40px', textAlign: 'center', color: 'var(--text-secondary)' }}>
            <RefreshCw size={24} className="animate-spin" style={{ margin: '0 auto 12px' }} />
            Loading operational dispatch queue...
          </div>
        )}

        {isError && (
          <div style={{ padding: '30px', textAlign: 'center', color: '#dc2626' }}>
            Failed to load queue: {error?.message}
          </div>
        )}

        {!isLoading && !isError && filteredBookings.length === 0 && (
          <div style={{ padding: '50px 20px', textAlign: 'center', color: 'var(--text-secondary)' }}>
            <h3 style={{ fontSize: '15px', fontWeight: 600, margin: '0 0 6px 0' }}>No visits in this queue tab</h3>
            <p style={{ margin: 0, fontSize: '13px' }}>
              All visits for this filter category have been dispatched or completed.
            </p>
          </div>
        )}

        {!isLoading && !isError && filteredBookings.length > 0 && (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)' }}>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Booking / Work Order</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Customer & Facility</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Scheduled Time (UTC)</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Dispatch State</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Assigned Field Personnel</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600, textAlign: 'right' }}>Dispatch Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredBookings.map((b) => {
                  const activeAssignments = (b.assignments || []).filter(
                    (a) => a.status.toLowerCase() !== 'cancelled'
                  );
                  const isUnassigned = activeAssignments.length === 0;
                  const isReadyToDispatch =
                    activeAssignments.length > 0 &&
                    b.dispatchStatus.toLowerCase() === 'assigned';

                  return (
                    <tr
                      key={b.id}
                      style={{
                        borderBottom: '1px solid var(--border-subtle)',
                        backgroundColor: isUnassigned ? '#fff1f215' : 'transparent',
                      }}
                    >
                      <td style={{ padding: '12px' }}>
                        <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>
                          {b.bookingNumber}
                        </div>
                        <div style={{ fontSize: '12px', color: 'var(--primary)', fontWeight: 500 }}>
                          {b.workOrderNumber} — {b.workOrderSummary}
                        </div>
                      </td>

                      <td style={{ padding: '12px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                          <Building size={13} color="var(--text-muted)" />
                          <span>{b.accountName || '—'}</span>
                        </div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                          <MapPin size={13} color="var(--text-muted)" />
                          <span>{b.siteName || '—'}</span>
                        </div>
                      </td>

                      <td style={{ padding: '12px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                          <Clock size={13} color="var(--text-muted)" />
                          <span>{new Date(b.plannedStartUtc).toLocaleDateString()}</span>
                        </div>
                        <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                          {new Date(b.plannedStartUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} –{' '}
                          {new Date(b.plannedEndUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </div>
                      </td>

                      <td style={{ padding: '12px' }}>
                        <StatusBadge status={b.dispatchStatus} />
                      </td>

                      <td style={{ padding: '12px' }}>
                        {activeAssignments.length > 0 ? (
                          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                            {activeAssignments.map((a) => (
                              <div
                                key={a.id}
                                style={{
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'space-between',
                                  backgroundColor: 'var(--bg-subtle)',
                                  padding: '4px 8px',
                                  borderRadius: '4px',
                                  fontSize: '12px',
                                }}
                              >
                                <span>
                                  <strong>{a.resourceDisplayName || a.resourceCode}</strong> ({a.assignmentRole})
                                </span>
                                {b.dispatchStatus.toLowerCase() !== 'completed' && (
                                  <button
                                    onClick={() => handleUnassign(b.id, a.id)}
                                    title="Unassign technician"
                                    style={{
                                      background: 'none',
                                      border: 'none',
                                      color: '#991b1b',
                                      cursor: 'pointer',
                                      display: 'flex',
                                      alignItems: 'center',
                                    }}
                                  >
                                    <UserMinus size={13} />
                                  </button>
                                )}
                              </div>
                            ))}
                          </div>
                        ) : (
                          <span style={{ color: '#9f1239', fontSize: '12px', fontWeight: 600 }}>
                            No Technician Assigned
                          </span>
                        )}
                      </td>

                      <td style={{ padding: '12px', textAlign: 'right' }}>
                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setSelectedBookingForAssign(b)}
                          >
                            <UserPlus size={13} />
                            {activeAssignments.length > 0 ? 'Add / Reassign' : 'Assign'}
                          </Button>

                          {isReadyToDispatch && (
                            <Button
                              size="sm"
                              onClick={() => handleDispatch(b)}
                              isLoading={dispatchMutation.isPending}
                            >
                              <Send size={13} />
                              Dispatch
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Assign Technician Modal */}
      <AssignTechnicianModal
        booking={selectedBookingForAssign}
        isOpen={Boolean(selectedBookingForAssign)}
        onClose={() => setSelectedBookingForAssign(null)}
      />
    </div>
  );
};
