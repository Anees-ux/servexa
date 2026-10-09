import React from 'react';
import {
  Activity,
  AlertCircle,
  Boxes,
  Building2,
  Calendar,
  ClipboardList,
  Layers,
  MapPin,
  RefreshCw,
  ShieldAlert,
  ShieldCheck,
  User,
} from 'lucide-react';
import { Link, Outlet, useRouterState } from '@tanstack/react-router';
import { useAuth } from '../app/auth/AuthContext';
import { Button } from '../shared/design-system/Button';

export const Shell: React.FC = () => {
  const { user, error, isLoading, retryAuth } = useAuth();
  const routerState = useRouterState();
  const currentPath = routerState.location.pathname;

  const isNavActive = (path: string) => currentPath.startsWith(path);

  const getNavLinkStyle = (active: boolean): React.CSSProperties => ({
    display: 'flex',
    alignItems: 'center',
    gap: '10px',
    padding: '9px 12px',
    borderRadius: 'var(--radius-md)',
    textDecoration: 'none',
    backgroundColor: active ? 'var(--primary-subtle)' : 'transparent',
    color: active ? 'var(--primary-text)' : 'var(--text-secondary)',
    fontWeight: active ? 600 : 500,
    fontSize: '14px',
    transition: 'background-color 0.1s ease',
  });

  return (
    <div style={{ display: 'flex', flexDirection: 'column', minHeight: '100vh', backgroundColor: 'var(--bg-app)' }}>
      {/* Top Navigation Bar */}
      <header
        style={{
          height: '56px',
          backgroundColor: '#0f172a',
          color: '#ffffff',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '0 24px',
          borderBottom: '1px solid #1e293b',
          zIndex: 50,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <div
              style={{
                width: '28px',
                height: '28px',
                borderRadius: '6px',
                backgroundColor: 'var(--primary)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontWeight: 800,
                fontSize: '14px',
                letterSpacing: '-0.05em',
                color: '#ffffff',
              }}
            >
              SX
            </div>
            <span style={{ fontWeight: 700, fontSize: '17px', letterSpacing: '0.02em' }}>SERVEXA</span>
            <span
              style={{
                fontSize: '11px',
                textTransform: 'uppercase',
                backgroundColor: '#334155',
                padding: '2px 6px',
                borderRadius: '4px',
                letterSpacing: '0.08em',
                fontWeight: 600,
              }}
            >
              MVP R1
            </span>
          </div>

          <div style={{ height: '20px', width: '1px', backgroundColor: '#334155' }} />

          {/* Tenant Indicator */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '13px', color: '#94a3b8' }}>
            <Layers size={14} />
            <span>Tenant:</span>
            <strong style={{ color: user ? '#f8fafc' : '#ef4444', fontWeight: 600 }}>
              {user ? 'Acme Industrial Operations (ACME)' : 'Disconnected (No Active Tenant)'}
            </strong>
          </div>
        </div>

        {/* User & Security Context */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          {user ? (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '6px',
                fontSize: '12px',
                padding: '4px 10px',
                backgroundColor: '#1e293b',
                borderRadius: '9999px',
                color: '#38bdf8',
                border: '1px solid #334155',
              }}
            >
              <ShieldCheck size={14} />
              <span>JWT Tenant Isolation Active</span>
            </div>
          ) : (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '6px',
                fontSize: '12px',
                padding: '4px 10px',
                backgroundColor: '#450a0a',
                borderRadius: '9999px',
                color: '#f87171',
                border: '1px solid #7f1d1d',
              }}
            >
              <ShieldAlert size={14} />
              <span>Backend Disconnected</span>
            </div>
          )}

          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <div
              style={{
                width: '32px',
                height: '32px',
                borderRadius: '50%',
                backgroundColor: user ? '#334155' : '#7f1d1d',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#e2e8f0',
              }}
            >
              <User size={16} />
            </div>
            <div style={{ display: 'flex', flexDirection: 'column' }}>
              <span style={{ fontSize: '13px', fontWeight: 600, color: '#f8fafc' }}>
                {user?.displayName || 'Unauthenticated'}
              </span>
              <span style={{ fontSize: '11px', color: '#94a3b8' }}>
                {user?.roles?.[0] || 'No Permissions'}
              </span>
            </div>
          </div>
        </div>
      </header>

      {/* Disconnection / Auth Error Banner */}
      {error && !user && (
        <div
          role="alert"
          style={{
            backgroundColor: '#fef2f2',
            borderBottom: '1px solid #f87171',
            padding: '12px 24px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <AlertCircle size={18} color="#dc2626" />
            <div>
              <strong style={{ color: '#991b1b', fontSize: '13px' }}>Backend API Connection Required: </strong>
              <span style={{ color: '#b91c1c', fontSize: '13px' }}>
                {error}
              </span>
            </div>
          </div>
          <Button variant="outline" size="sm" onClick={retryAuth} disabled={isLoading}>
            <RefreshCw size={13} className={isLoading ? 'animate-spin' : ''} />
            Retry Connection
          </Button>
        </div>
      )}

      {/* Main Container */}
      <div style={{ display: 'flex', flex: 1 }}>
        {/* Sidebar */}
        <aside
          style={{
            width: '230px',
            backgroundColor: 'var(--bg-surface)',
            borderRight: '1px solid var(--border-subtle)',
            padding: '20px 12px',
            display: 'flex',
            flexDirection: 'column',
            gap: '6px',
          }}
        >
          <div
            style={{
              padding: '0 8px 8px',
              fontSize: '11px',
              fontWeight: 700,
              textTransform: 'uppercase',
              color: 'var(--text-muted)',
              letterSpacing: '0.08em',
            }}
          >
            Operations Spine
          </div>

          <Link to="/customers" style={getNavLinkStyle(isNavActive('/customers'))}>
            <Building2 size={17} />
            <span>Customers</span>
          </Link>

          <Link to="/sites" style={getNavLinkStyle(isNavActive('/sites'))}>
            <MapPin size={17} />
            <span>Sites & Facilities</span>
          </Link>

          <Link to="/assets" style={getNavLinkStyle(isNavActive('/assets'))}>
            <Boxes size={17} />
            <span>Asset Registry</span>
          </Link>

          <Link to="/work-orders" style={getNavLinkStyle(isNavActive('/work-orders'))}>
            <ClipboardList size={17} />
            <span>Work Orders</span>
          </Link>

          <Link to="/dispatch" style={getNavLinkStyle(isNavActive('/dispatch'))}>
            <Calendar size={17} />
            <span>Dispatch & Scheduling</span>
          </Link>

          <div
            style={{
              marginTop: '20px',
              padding: '0 8px 8px',
              fontSize: '11px',
              fontWeight: 700,
              textTransform: 'uppercase',
              color: 'var(--text-muted)',
              letterSpacing: '0.08em',
            }}
          >
            Next Verticals
          </div>

          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '10px',
              padding: '8px 12px',
              fontSize: '13px',
              color: 'var(--text-muted)',
            }}
          >
            <Activity size={16} />
            <span>Field Execution</span>
          </div>
        </aside>

        {/* Content View routed by TanStack Router */}
        <main style={{ flex: 1, padding: '28px 36px', maxWidth: '1400px' }}>
          <Outlet />
        </main>
      </div>
    </div>
  );
};
