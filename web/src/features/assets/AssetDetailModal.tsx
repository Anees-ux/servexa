import React, { useState } from 'react';
import { History } from 'lucide-react';
import { useChangeAssetStatusMutation, useSitesQuery } from '../../shared/api/queries';
import type { AssetDto } from '../../shared/api/types';
import { Button } from '../../shared/design-system/Button';
import { Input } from '../../shared/design-system/Input';
import { Modal } from '../../shared/design-system/Modal';
import { Select } from '../../shared/design-system/Select';
import { StatusBadge } from '../../shared/design-system/StatusBadge';

interface AssetDetailModalProps {
  asset: AssetDto | null;
  isOpen: boolean;
  onClose: () => void;
  onUpdated?: (updated: AssetDto) => void;
}

export const AssetDetailModal: React.FC<AssetDetailModalProps> = ({
  asset,
  isOpen,
  onClose,
  onUpdated,
}) => {
  const [reason, setReason] = useState('');
  const [siteId, setSiteId] = useState('');
  const [error, setError] = useState<string | null>(null);

  const changeStatusMutation = useChangeAssetStatusMutation();
  const { data: sitesData } = useSitesQuery(undefined, undefined, '', 1, 100);

  if (!asset) return null;

  const handleTransition = async (targetStatus: number) => {
    setError(null);
    try {
      const updated = await changeStatusMutation.mutateAsync({
        id: asset.id,
        request: {
          targetStatus,
          reason: reason ? reason.trim() : null,
          siteId: siteId || null,
        },
      });
      setReason('');
      onUpdated?.(updated);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Lifecycle transition failed';
      setError(message);
    }
  };

  const siteOptions = [
    { value: '', label: '-- Select Commissioning Site --' },
    ...(sitesData?.items.map((s) => ({
      value: s.id,
      label: `${s.siteNumber} - ${s.name}`,
    })) || []),
  ];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={`Asset: ${asset.assetNumber}`}
      subtitle={`${asset.manufacturerName || ''} ${asset.modelCode || ''} — ${asset.equipmentModelName || 'Registered Equipment'}`}
      size="lg"
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
        {error && (
          <div
            role="alert"
            style={{
              padding: '12px',
              backgroundColor: 'var(--danger-subtle)',
              border: '1px solid var(--danger)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--danger-text)',
              fontSize: '13px',
            }}
          >
            {error}
          </div>
        )}

        {/* Technical Attributes Grid */}
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr',
            gap: '16px',
            padding: '16px',
            backgroundColor: 'var(--bg-subtle)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <div>
            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Status</span>
            <div style={{ marginTop: '4px' }}>
              <StatusBadge status={asset.status} />
            </div>
          </div>

          <div>
            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Serial Number</span>
            <div style={{ fontWeight: 600, marginTop: '4px', color: 'var(--text-primary)' }}>
              {asset.serialNumber || '— (Non-Serialized)'}
            </div>
          </div>

          <div>
            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Installed Site</span>
            <div style={{ fontWeight: 500, marginTop: '4px', color: 'var(--text-primary)' }}>
              {asset.currentSiteName || '— (In Depot/Staging)'}
            </div>
          </div>

          <div>
            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Customer Account</span>
            <div style={{ fontWeight: 500, marginTop: '4px', color: 'var(--text-primary)' }}>
              {asset.currentOwnerAccountName || '— (Tenant Inventory)'}
            </div>
          </div>

          <div>
            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Commissioned Date</span>
            <div style={{ fontSize: '13px', marginTop: '4px', color: 'var(--text-secondary)' }}>
              {asset.installedAtUtc ? new Date(asset.installedAtUtc).toLocaleDateString() : 'Pending Commission'}
            </div>
          </div>

          <div>
            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Decommissioned Date</span>
            <div style={{ fontSize: '13px', marginTop: '4px', color: 'var(--text-secondary)' }}>
              {asset.decommissionedAtUtc ? new Date(asset.decommissionedAtUtc).toLocaleDateString() : 'N/A (In Service)'}
            </div>
          </div>
        </div>

        {/* Operational Lifecycle Actions */}
        <div style={{ borderTop: '1px solid var(--border-subtle)', paddingTop: '16px' }}>
          <h4 style={{ fontSize: '14px', fontWeight: 600, color: 'var(--text-primary)', marginBottom: '12px' }}>
            Operational Lifecycle Guardrails
          </h4>

          {asset.status === 'PreInstallation' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              {!asset.currentSiteId && (
                <Select
                  label="Select Commissioning Site"
                  value={siteId}
                  onChange={(e) => setSiteId(e.target.value)}
                  options={siteOptions}
                />
              )}
              <Button
                variant="primary"
                onClick={() => handleTransition(2)} // 2 = Active
                disabled={changeStatusMutation.isPending}
              >
                Commission Asset into Service
              </Button>
            </div>
          )}

          {asset.status === 'Active' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <Input
                label="Action Reason / Note (Optional)"
                placeholder="Reason for degrading or decommissioning..."
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
              <div style={{ display: 'flex', gap: '10px' }}>
                <Button
                  variant="outline"
                  onClick={() => handleTransition(3)} // 3 = Degraded
                  disabled={changeStatusMutation.isPending}
                >
                  Mark Degraded (Requires Service)
                </Button>
                <Button
                  variant="danger"
                  onClick={() => handleTransition(4)} // 4 = Decommissioned
                  disabled={changeStatusMutation.isPending}
                >
                  Decommission Asset
                </Button>
              </div>
            </div>
          )}

          {asset.status === 'Degraded' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <Input
                label="Action Reason / Service Note"
                placeholder="Details of service performed or decommissioning..."
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
              <div style={{ display: 'flex', gap: '10px' }}>
                <Button
                  variant="primary"
                  onClick={() => handleTransition(2)} // 2 = Active
                  disabled={changeStatusMutation.isPending}
                >
                  Restore to Full Operational Active
                </Button>
                <Button
                  variant="danger"
                  onClick={() => handleTransition(4)} // 4 = Decommissioned
                  disabled={changeStatusMutation.isPending}
                >
                  Decommission Asset
                </Button>
              </div>
            </div>
          )}

          {asset.status === 'Decommissioned' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <Input
                label="Reactivation / Replacement Reason"
                placeholder="Audit reason for reactivation or replacement..."
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
              <div style={{ display: 'flex', gap: '10px' }}>
                <Button
                  variant="primary"
                  onClick={() => handleTransition(2)} // 2 = Active
                  disabled={changeStatusMutation.isPending}
                >
                  Reactivate Asset
                </Button>
                <Button
                  variant="outline"
                  onClick={() => handleTransition(5)} // 5 = Replaced
                  disabled={changeStatusMutation.isPending}
                >
                  Mark Permanently Replaced
                </Button>
              </div>
            </div>
          )}

          {asset.status === 'Replaced' && (
            <div style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
              This asset has been permanently retired and replaced. Terminal state is immutable.
            </div>
          )}
        </div>

        {/* Lifecycle Audit History Ledger */}
        <div style={{ borderTop: '1px solid var(--border-subtle)', paddingTop: '16px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '10px' }}>
            <History size={15} color="var(--text-secondary)" />
            <h4 style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>
              Lifecycle Audit History ({asset.lifecycleEvents?.length || 0})
            </h4>
          </div>
          <div style={{ overflowX: 'auto', maxHeight: '180px', overflowY: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '12px' }}>
              <thead>
                <tr style={{ backgroundColor: 'var(--bg-subtle)', textAlign: 'left' }}>
                  <th style={{ padding: '6px 10px', color: 'var(--text-secondary)' }}>Event</th>
                  <th style={{ padding: '6px 10px', color: 'var(--text-secondary)' }}>Status Transition</th>
                  <th style={{ padding: '6px 10px', color: 'var(--text-secondary)' }}>Audit Reason</th>
                  <th style={{ padding: '6px 10px', color: 'var(--text-secondary)' }}>Timestamp</th>
                </tr>
              </thead>
              <tbody>
                {(asset.lifecycleEvents || []).map((ev) => (
                  <tr key={ev.id} style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                    <td style={{ padding: '6px 10px', fontWeight: 600, color: 'var(--text-primary)' }}>
                      {ev.eventType}
                    </td>
                    <td style={{ padding: '6px 10px' }}>
                      {ev.previousStatus || ev.newStatus ? (
                        <span>
                          {ev.previousStatus || 'None'} → {ev.newStatus || 'None'}
                        </span>
                      ) : (
                        <span style={{ color: 'var(--text-muted)' }}>—</span>
                      )}
                    </td>
                    <td style={{ padding: '6px 10px', color: 'var(--text-primary)' }}>
                      {ev.reason || '—'}
                    </td>
                    <td style={{ padding: '6px 10px', color: 'var(--text-muted)' }}>
                      {new Date(ev.occurredAtUtc).toLocaleString()}
                    </td>
                  </tr>
                ))}
                {(!asset.lifecycleEvents || asset.lifecycleEvents.length === 0) && (
                  <tr>
                    <td colSpan={4} style={{ padding: '12px', textAlign: 'center', color: 'var(--text-muted)' }}>
                      No lifecycle events recorded.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '12px' }}>
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>
    </Modal>
  );
};
