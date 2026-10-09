import React, { useState } from 'react';
import { AlertCircle, AlertTriangle, Key, MapPin, Plus, RefreshCw, Search } from 'lucide-react';
import { useSitesQuery } from '../../shared/api/queries';
import { Button } from '../../shared/design-system/Button';
import { Card } from '../../shared/design-system/Card';
import { Input } from '../../shared/design-system/Input';
import { StatusBadge } from '../../shared/design-system/StatusBadge';
import { CreateSiteModal } from './CreateSiteModal';

export const SitesView: React.FC = () => {
  const [search, setSearch] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);

  const { data, isLoading, error, refetch, isFetching } = useSitesQuery(
    undefined,
    undefined,
    search || undefined
  );

  const sites = data?.items ?? [];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: '22px', fontWeight: 700, color: 'var(--text-primary)', margin: 0 }}>
            Operational Sites
          </h1>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            Physical service locations, dispatch coordinates, hazards and access profiles.
          </p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
            aria-label="Refresh operational sites"
          >
            <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
            Refresh
          </Button>
          <Button variant="primary" size="md" onClick={() => setIsModalOpen(true)}>
            <Plus size={16} />
            New Site
          </Button>
        </div>
      </div>

      {/* Filter / Search Bar */}
      <Card>
        <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
          <div style={{ position: 'relative', flex: 1 }}>
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
              placeholder="Search by site number, facility name, address, or city..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ paddingLeft: '36px' }}
            />
          </div>
        </div>
      </Card>

      {/* Error Banner */}
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
              <strong style={{ color: 'var(--danger-text)', fontSize: '14px' }}>Unable to load operational sites</strong>
              <p style={{ color: 'var(--danger-text)', fontSize: '13px', margin: '2px 0 0' }}>{error.message}</p>
            </div>
          </div>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Retry
          </Button>
        </div>
      )}

      {/* Data Table / Empty State / Loading State */}
      <Card style={{ padding: 0, overflow: 'hidden' }}>
        {isLoading ? (
          <div style={{ padding: '48px', textAlign: 'center', color: 'var(--text-secondary)' }}>
            <div style={{ display: 'inline-block', width: '28px', height: '28px', border: '3px solid var(--border-strong)', borderTopColor: 'var(--primary)', borderRadius: '50%', animation: 'spin 0.8s linear infinite' }} />
            <p style={{ marginTop: '12px', fontSize: '14px' }}>Loading operational sites from server...</p>
          </div>
        ) : sites.length === 0 ? (
          <div style={{ padding: '64px 24px', textAlign: 'center' }}>
            <MapPin size={40} style={{ margin: '0 auto 12px', color: 'var(--text-muted)' }} />
            <h3 style={{ fontSize: '16px', fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>
              No operational sites found
            </h3>
            <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '4px', maxWidth: '400px', marginInline: 'auto' }}>
              {search ? 'No sites matched your search criteria.' : 'No site locations currently exist in this tenant. Click "New Site" to register an operational facility.'}
            </p>
            {!search && (
              <div style={{ marginTop: '16px' }}>
                <Button variant="primary" size="sm" onClick={() => setIsModalOpen(true)}>
                  <Plus size={14} />
                  Create First Site
                </Button>
              </div>
            )}
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
              <thead>
                <tr style={{ backgroundColor: 'var(--bg-subtle)', borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-secondary)' }}>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Site #</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Facility Name</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Address & Location</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Time Zone</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Status</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Safety & Access</th>
                </tr>
              </thead>
              <tbody>
                {sites.map((site) => (
                  <tr
                    key={site.id}
                    style={{
                      borderBottom: '1px solid var(--border-subtle)',
                      transition: 'background 0.1s ease',
                    }}
                  >
                    <td style={{ padding: '12px 16px', fontWeight: 600, fontFamily: 'var(--font-mono)' }}>
                      {site.siteNumber}
                    </td>
                    <td style={{ padding: '12px 16px' }}>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{site.name}</div>
                    </td>
                    <td style={{ padding: '12px 16px', color: 'var(--text-secondary)' }}>
                      <div>{site.addressLine1}</div>
                      <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                        {site.city}, {site.stateProvince} {site.postalCode}
                      </div>
                    </td>
                    <td style={{ padding: '12px 16px', color: 'var(--text-secondary)', fontFamily: 'var(--font-mono)', fontSize: '12px' }}>
                      {site.timeZoneId}
                    </td>
                    <td style={{ padding: '12px 16px' }}>
                      <StatusBadge status={site.status} />
                    </td>
                    <td style={{ padding: '12px 16px' }}>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                        {site.hazardNotes && (
                          <div style={{ display: 'flex', alignItems: 'center', gap: '4px', color: 'var(--warning)', fontSize: '11px', fontWeight: 500 }}>
                            <AlertTriangle size={12} />
                            <span>{site.hazardNotes}</span>
                          </div>
                        )}
                        {site.accessNotes && (
                          <div style={{ display: 'flex', alignItems: 'center', gap: '4px', color: 'var(--text-muted)', fontSize: '11px' }}>
                            <Key size={12} />
                            <span>{site.accessNotes}</span>
                          </div>
                        )}
                        {!site.hazardNotes && !site.accessNotes && (
                          <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>Standard</span>
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

      <CreateSiteModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
      />
    </div>
  );
};
