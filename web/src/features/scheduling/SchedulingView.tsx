import React, { useState } from 'react';
import { Calendar, Plus, RefreshCw, Clock, MapPin, Building, UserCheck } from 'lucide-react';
import { Card } from '../../shared/design-system/Card';
import { Button } from '../../shared/design-system/Button';
import { StatusBadge } from '../../shared/design-system/StatusBadge';
import { useBookingsQuery } from '../../shared/api/queries';
import { CreateBookingModal } from './CreateBookingModal';
import { RescheduleBookingModal } from './RescheduleBookingModal';
import { CancelBookingModal } from './CancelBookingModal';
import type { BookingDto } from '../../shared/api/types';
import { formatSiteDateTime } from '../../shared/utils/timezone';

export const SchedulingView: React.FC = () => {
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [selectedBookingForReschedule, setSelectedBookingForReschedule] = useState<BookingDto | null>(null);
  const [selectedBookingForCancel, setSelectedBookingForCancel] = useState<BookingDto | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const { data: bookingsData, isLoading, isError, error, refetch } = useBookingsQuery(
    undefined,
    undefined,
    statusFilter === 'all' ? undefined : undefined,
    undefined,
    undefined,
    undefined,
    1,
    50
  );

  const bookings = bookingsData?.items || [];

  const filteredBookings = statusFilter === 'all'
    ? bookings
    : bookings.filter((b) => b.status.toLowerCase() === statusFilter.toLowerCase());

  const totalCount = bookings.length;
  const scheduledCount = bookings.filter((b) => b.status.toLowerCase() === 'scheduled').length;
  const inProgressCount = bookings.filter((b) => b.status.toLowerCase() === 'inprogress').length;
  const completedCount = bookings.filter((b) => b.status.toLowerCase() === 'completed').length;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: '24px', fontWeight: 700, margin: '0 0 6px 0', color: 'var(--text-primary)' }}>
            Scheduling & Customer Visits
          </h1>
          <p style={{ margin: 0, color: 'var(--text-secondary)', fontSize: '14px' }}>
            Multi-visit appointment envelopes, time windows, and operational dispatch scheduling.
          </p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <Button variant="outline" onClick={() => refetch()} disabled={isLoading}>
            <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
            Refresh
          </Button>
          <Button onClick={() => setIsCreateOpen(true)}>
            <Plus size={16} />
            Schedule Booking
          </Button>
        </div>
      </div>

      {/* Metrics Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '16px' }}>
        <Card>
          <div style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 500 }}>Total Bookings</div>
          <div style={{ fontSize: '28px', fontWeight: 700, marginTop: '4px', color: 'var(--text-primary)' }}>
            {totalCount}
          </div>
        </Card>
        <Card>
          <div style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 500 }}>Scheduled / Confirmed</div>
          <div style={{ fontSize: '28px', fontWeight: 700, marginTop: '4px', color: '#5b21b6' }}>
            {scheduledCount}
          </div>
        </Card>
        <Card>
          <div style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 500 }}>In Progress (Field)</div>
          <div style={{ fontSize: '28px', fontWeight: 700, marginTop: '4px', color: '#1e40af' }}>
            {inProgressCount}
          </div>
        </Card>
        <Card>
          <div style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 500 }}>Completed Visits</div>
          <div style={{ fontSize: '28px', fontWeight: 700, marginTop: '4px', color: '#065f46' }}>
            {completedCount}
          </div>
        </Card>
      </div>

      {/* Filter Tabs */}
      <div style={{ display: 'flex', gap: '8px', borderBottom: '1px solid var(--border-subtle)', paddingBottom: '12px' }}>
        {[
          { key: 'all', label: 'All Bookings' },
          { key: 'scheduled', label: 'Scheduled' },
          { key: 'dispatched', label: 'Dispatched' },
          { key: 'inprogress', label: 'In Progress' },
          { key: 'completed', label: 'Completed' },
          { key: 'cancelled', label: 'Cancelled' },
        ].map((tab) => (
          <button
            key={tab.key}
            onClick={() => setStatusFilter(tab.key)}
            style={{
              padding: '6px 14px',
              borderRadius: '6px',
              border: 'none',
              backgroundColor: statusFilter === tab.key ? 'var(--primary-subtle)' : 'transparent',
              color: statusFilter === tab.key ? 'var(--primary-text)' : 'var(--text-secondary)',
              fontWeight: statusFilter === tab.key ? 600 : 500,
              fontSize: '13px',
              cursor: 'pointer',
            }}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Bookings Table Card */}
      <Card>
        {isLoading && (
          <div style={{ padding: '40px', textAlign: 'center', color: 'var(--text-secondary)' }}>
            <RefreshCw size={24} className="animate-spin" style={{ margin: '0 auto 12px' }} />
            Loading bookings from tenant ledger...
          </div>
        )}

        {isError && (
          <div style={{ padding: '30px', textAlign: 'center', color: '#dc2626' }}>
            Failed to load bookings: {error?.message}
          </div>
        )}

        {!isLoading && !isError && filteredBookings.length === 0 && (
          <div style={{ padding: '60px 20px', textAlign: 'center', color: 'var(--text-secondary)' }}>
            <Calendar size={36} style={{ margin: '0 auto 12px', opacity: 0.4 }} />
            <h3 style={{ fontSize: '16px', fontWeight: 600, margin: '0 0 6px 0' }}>No Bookings Found</h3>
            <p style={{ margin: '0 0 16px 0', fontSize: '13px' }}>
              Create a booking from an approved work order to initiate technician scheduling.
            </p>
            <Button onClick={() => setIsCreateOpen(true)}>
              <Plus size={15} />
              Schedule First Booking
            </Button>
          </div>
        )}

        {!isLoading && !isError && filteredBookings.length > 0 && (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)' }}>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Booking #</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Work Order</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Customer / Site</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Scheduled Window (UTC)</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Status</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600 }}>Assigned Resources</th>
                  <th style={{ padding: '10px 12px', fontWeight: 600, textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredBookings.map((b) => (
                  <tr
                    key={b.id}
                    style={{
                      borderBottom: '1px solid var(--border-subtle)',
                      transition: 'background-color 0.1s ease',
                    }}
                  >
                    <td style={{ padding: '12px', fontWeight: 600, color: 'var(--text-primary)' }}>
                      {b.bookingNumber}
                    </td>
                    <td style={{ padding: '12px' }}>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>
                        {b.workOrderNumber || '—'}
                      </div>
                      <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                        {b.workOrderSummary || '—'}
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
                        <span style={{ fontWeight: 500 }}>{formatSiteDateTime(b.plannedStartUtc, b.siteTimeZoneId)}</span>
                      </div>
                      <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                        End: {formatSiteDateTime(b.plannedEndUtc, b.siteTimeZoneId)}
                      </div>
                    </td>
                    <td style={{ padding: '12px' }}>
                      <StatusBadge status={b.status} />
                    </td>
                    <td style={{ padding: '12px' }}>
                      {b.assignments && b.assignments.length > 0 ? (
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                          {b.assignments
                            .filter((a) => a.status.toLowerCase() !== 'cancelled')
                            .map((a) => (
                              <div
                                key={a.id}
                                style={{
                                  display: 'flex',
                                  alignItems: 'center',
                                  gap: '6px',
                                  fontSize: '12px',
                                }}
                              >
                                <UserCheck size={12} color="#166534" />
                                <span>{a.resourceDisplayName || a.resourceCode || 'Assigned'}</span>
                                <span style={{ fontSize: '10px', color: 'var(--text-muted)' }}>
                                  ({a.assignmentRole})
                                </span>
                              </div>
                            ))}
                        </div>
                      ) : (
                        <span style={{ color: '#dc2626', fontSize: '12px', fontStyle: 'italic' }}>
                          Unassigned
                        </span>
                      )}
                    </td>
                    <td style={{ padding: '12px', textAlign: 'right' }}>
                      <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
                        {b.status.toLowerCase() !== 'completed' && b.status.toLowerCase() !== 'cancelled' && (
                          <>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => setSelectedBookingForReschedule(b)}
                            >
                              Reschedule
                            </Button>
                            <Button
                              variant="danger"
                              size="sm"
                              onClick={() => setSelectedBookingForCancel(b)}
                            >
                              Cancel
                            </Button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Modals */}
      <CreateBookingModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
      />

      <RescheduleBookingModal
        booking={selectedBookingForReschedule}
        isOpen={Boolean(selectedBookingForReschedule)}
        onClose={() => setSelectedBookingForReschedule(null)}
      />

      <CancelBookingModal
        booking={selectedBookingForCancel}
        isOpen={Boolean(selectedBookingForCancel)}
        onClose={() => setSelectedBookingForCancel(null)}
      />
    </div>
  );
};
