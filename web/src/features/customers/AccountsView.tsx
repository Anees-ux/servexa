import React, { useState } from 'react';
import { Building2, Plus, Search, AlertCircle, RefreshCw } from 'lucide-react';
import { useAccountsQuery } from '../../shared/api/queries';
import { Button } from '../../shared/design-system/Button';
import { Card } from '../../shared/design-system/Card';
import { Input } from '../../shared/design-system/Input';
import { StatusBadge } from '../../shared/design-system/StatusBadge';
import { CreateAccountModal } from './CreateAccountModal';

export const AccountsView: React.FC = () => {
  const [search, setSearch] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);

  const { data, isLoading, error, refetch, isFetching } = useAccountsQuery(search || undefined);

  const accounts = data?.items ?? [];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Header bar */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: '22px', fontWeight: 700, color: 'var(--text-primary)', margin: 0 }}>
            Customer Accounts
          </h1>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            Enterprise customer billing and relationship master records.
          </p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
            aria-label="Refresh customer accounts"
          >
            <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
            Refresh
          </Button>
          <Button variant="primary" size="md" onClick={() => setIsModalOpen(true)}>
            <Plus size={16} />
            New Account
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
              placeholder="Search by account number, legal name, or display name..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ paddingLeft: '36px' }}
            />
          </div>
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
              <strong style={{ color: 'var(--danger-text)', fontSize: '14px' }}>Unable to load accounts</strong>
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
            <p style={{ marginTop: '12px', fontSize: '14px' }}>Loading customer accounts from server...</p>
          </div>
        ) : accounts.length === 0 ? (
          <div style={{ padding: '64px 24px', textAlign: 'center' }}>
            <Building2 size={40} style={{ margin: '0 auto 12px', color: 'var(--text-muted)' }} />
            <h3 style={{ fontSize: '16px', fontWeight: 600, color: 'var(--text-primary)', margin: 0 }}>
              No customer accounts found
            </h3>
            <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '4px', maxWidth: '400px', marginInline: 'auto' }}>
              {search ? 'No accounts matched your search criteria.' : 'No customer records currently exist in this tenant. Click "New Account" to create your first customer.'}
            </p>
            {!search && (
              <div style={{ marginTop: '16px' }}>
                <Button variant="primary" size="sm" onClick={() => setIsModalOpen(true)}>
                  <Plus size={14} />
                  Create First Account
                </Button>
              </div>
            )}
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
              <thead>
                <tr style={{ backgroundColor: 'var(--bg-subtle)', borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-secondary)' }}>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Account #</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Legal & Display Name</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Type</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Status</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Credit Status</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Terms</th>
                  <th style={{ padding: '12px 16px', fontWeight: 600 }}>Currency</th>
                </tr>
              </thead>
              <tbody>
                {accounts.map((acc) => (
                  <tr
                    key={acc.id}
                    style={{
                      borderBottom: '1px solid var(--border-subtle)',
                      transition: 'background 0.1s ease',
                    }}
                  >
                    <td style={{ padding: '12px 16px', fontWeight: 600, fontFamily: 'var(--font-mono)' }}>
                      {acc.accountNumber}
                    </td>
                    <td style={{ padding: '12px 16px' }}>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{acc.displayName}</div>
                      <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{acc.legalName}</div>
                    </td>
                    <td style={{ padding: '12px 16px', color: 'var(--text-secondary)' }}>
                      {acc.accountType}
                    </td>
                    <td style={{ padding: '12px 16px' }}>
                      <StatusBadge status={acc.status} />
                    </td>
                    <td style={{ padding: '12px 16px' }}>
                      {acc.isCreditHold ? (
                        <span style={{ color: 'var(--danger)', fontWeight: 600, fontSize: '12px' }}>
                          Credit Hold
                        </span>
                      ) : (
                        <span style={{ color: 'var(--success)', fontSize: '12px' }}>Good Standing</span>
                      )}
                    </td>
                    <td style={{ padding: '12px 16px', color: 'var(--text-secondary)' }}>
                      {acc.paymentTermsDays ?? 30} Days
                    </td>
                    <td style={{ padding: '12px 16px', fontFamily: 'var(--font-mono)' }}>
                      {acc.currencyCode}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <CreateAccountModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
      />
    </div>
  );
};
