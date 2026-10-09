import React, { useState } from 'react';
import { AlertCircle, Boxes, Plus, RefreshCw, Search, SlidersHorizontal } from 'lucide-react';
import { useAssetsQuery, useEquipmentModelsQuery, useSitesQuery } from '../../shared/api/queries';
import type { AssetDto } from '../../shared/api/types';
import { Button } from '../../shared/design-system/Button';
import { Card } from '../../shared/design-system/Card';
import { Input } from '../../shared/design-system/Input';
import { Select } from '../../shared/design-system/Select';
import { StatusBadge } from '../../shared/design-system/StatusBadge';
import { AssetDetailModal } from './AssetDetailModal';
import { CreateAssetModal } from './CreateAssetModal';
import { CreateEquipmentModelModal } from './CreateEquipmentModelModal';

export const AssetsView: React.FC = () => {
  const [search, setSearch] = useState('');
  const [siteFilter, setSiteFilter] = useState('');
  const [modelFilter, setModelFilter] = useState('');
  const [isAssetModalOpen, setIsAssetModalOpen] = useState(false);
  const [isModelModalOpen, setIsModelModalOpen] = useState(false);
  const [selectedAsset, setSelectedAsset] = useState<AssetDto | null>(null);

  const { data, isLoading, error, refetch, isFetching } = useAssetsQuery(
    siteFilter || undefined,
    undefined,
    modelFilter || undefined,
    search || undefined
  );

  const { data: sitesData } = useSitesQuery(undefined, undefined, '', 1, 100);
  const { data: modelsData } = useEquipmentModelsQuery('', 1, 100);

  const assets = data?.items ?? [];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Header Bar */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: '22px', fontWeight: 700, color: 'var(--text-primary)', margin: 0 }}>
            Asset Registry & Equipment Models
          </h1>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            Mission-critical physical equipment, serial tracking, site installations, and operational lifecycles.
          </p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
            aria-label="Refresh assets"
          >
            <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
            Refresh
          </Button>
          <Button variant="outline" size="md" onClick={() => setIsModelModalOpen(true)}>
            <Plus size={16} />
            New Model
          </Button>
          <Button variant="primary" size="md" onClick={() => setIsAssetModalOpen(true)}>
            <Plus size={16} />
            Register Asset
          </Button>
        </div>
      </div>

      {/* Filter / Search Bar */}
      <Card>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 220px 220px', gap: '12px', alignItems: 'center' }}>
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
              placeholder="Search by asset tag, serial number..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ paddingLeft: '36px' }}
            />
          </div>

          <Select
            value={siteFilter}
            onChange={(e) => setSiteFilter(e.target.value)}
            options={[
              { value: '', label: 'All Operational Sites' },
              ...(sitesData?.items.map((s) => ({ value: s.id, label: s.name })) || []),
            ]}
          />

          <Select
            value={modelFilter}
            onChange={(e) => setModelFilter(e.target.value)}
            options={[
              { value: '', label: 'All Equipment Models' },
              ...(modelsData?.items.map((m) => ({
                value: m.id,
                label: `${m.manufacturerName} ${m.modelCode}`,
              })) || []),
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
              <strong style={{ color: 'var(--danger-text)', fontSize: '14px' }}>Unable to load asset registry</strong>
              <p style={{ color: 'var(--danger-text)', fontSize: '13px', margin: '2px 0 0' }}>{error.message}</p>
            </div>
          </div>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Retry
          </Button>
        </div>
      )}

      {/* Assets Table */}
      <Card noPadding>
        {isLoading ? (
          <div style={{ padding: '60px', textAlign: 'center', color: 'var(--text-secondary)' }}>
            <RefreshCw size={24} className="animate-spin" style={{ margin: '0 auto 12px' }} />
            <p style={{ fontSize: '14px' }}>Loading registered assets...</p>
          </div>
        ) : assets.length === 0 ? (
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
              <Boxes size={24} />
            </div>
            <h3 style={{ fontSize: '16px', fontWeight: 600, color: 'var(--text-primary)', margin: '0 0 6px' }}>
              No registered assets found
            </h3>
            <p style={{ fontSize: '13px', color: 'var(--text-secondary)', maxWidth: '400px', margin: '0 auto 16px' }}>
              {search || siteFilter || modelFilter
                ? 'Try adjusting your search criteria or filters.'
                : 'Get started by creating an equipment model and registering your first customer asset.'}
            </p>
            <div style={{ display: 'flex', justifyContent: 'center', gap: '10px' }}>
              <Button variant="outline" size="sm" onClick={() => setIsModelModalOpen(true)}>
                New Model
              </Button>
              <Button variant="primary" size="sm" onClick={() => setIsAssetModalOpen(true)}>
                <Plus size={14} />
                Register Asset
              </Button>
            </div>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
              <thead>
                <tr style={{ backgroundColor: 'var(--bg-subtle)', borderBottom: '1px solid var(--border-subtle)' }}>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Asset Tag</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Model / Specification</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Serial Number</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Installed Site</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Customer Account</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Status</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)', textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {assets.map((asset) => (
                  <tr
                    key={asset.id}
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
                        {asset.assetNumber}
                      </span>
                    </td>
                    <td style={{ padding: '14px 16px' }}>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>
                        {asset.manufacturerName} {asset.modelCode}
                      </div>
                      <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                        {asset.equipmentModelName}
                      </div>
                    </td>
                    <td style={{ padding: '14px 16px', fontFamily: 'monospace', color: 'var(--text-secondary)' }}>
                      {asset.serialNumber || '—'}
                    </td>
                    <td style={{ padding: '14px 16px', color: 'var(--text-primary)' }}>
                      {asset.currentSiteName || <span style={{ color: 'var(--text-muted)' }}>Depot / Unassigned</span>}
                    </td>
                    <td style={{ padding: '14px 16px', color: 'var(--text-secondary)' }}>
                      {asset.currentOwnerAccountName || '—'}
                    </td>
                    <td style={{ padding: '14px 16px' }}>
                      <StatusBadge status={asset.status} />
                    </td>
                    <td style={{ padding: '14px 16px', textAlign: 'right' }}>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setSelectedAsset(asset)}
                      >
                        <SlidersHorizontal size={13} />
                        Lifecycle
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
      <CreateAssetModal
        isOpen={isAssetModalOpen}
        onClose={() => setIsAssetModalOpen(false)}
        onCreated={() => refetch()}
      />

      <CreateEquipmentModelModal
        isOpen={isModelModalOpen}
        onClose={() => setIsModelModalOpen(false)}
      />

      <AssetDetailModal
        asset={selectedAsset}
        isOpen={Boolean(selectedAsset)}
        onClose={() => setSelectedAsset(null)}
        onUpdated={(updated) => {
          setSelectedAsset(updated);
          refetch();
        }}
      />
    </div>
  );
};
