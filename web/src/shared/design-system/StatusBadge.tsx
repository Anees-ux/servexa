import React from 'react';

interface StatusBadgeProps {
  status: string;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status }) => {
  const getBadgeStyle = (): React.CSSProperties => {
    const s = status.toLowerCase();
    if (s === 'active' || s === 'completed' || s === 'operationallycomplete') {
      return {
        backgroundColor: '#ecfdf5',
        color: '#065f46',
        borderColor: '#a7f3d0',
      };
    }
    if (s === 'inprogress' || s === 'working' || s === 'onsite') {
      return {
        backgroundColor: '#eff6ff',
        color: '#1e40af',
        borderColor: '#bfdbfe',
      };
    }
    if (s === 'scheduled' || s === 'assigned') {
      return {
        backgroundColor: '#f5f3ff',
        color: '#5b21b6',
        borderColor: '#ddd6fe',
      };
    }
    if (s === 'dispatched' || s === 'traveling' || s === 'enroute') {
      return {
        backgroundColor: '#f0fdf4',
        color: '#166534',
        borderColor: '#bbf7d0',
      };
    }
    if (s === 'paused' || s === 'onhold') {
      return {
        backgroundColor: '#fffbeb',
        color: '#92400e',
        borderColor: '#fde68a',
      };
    }
    if (s === 'unassigned') {
      return {
        backgroundColor: '#fff1f2',
        color: '#9f1239',
        borderColor: '#fecdd3',
      };
    }
    if (s === 'cancelled' || s === 'inactive' || s === 'noshow') {
      return {
        backgroundColor: '#fef2f2',
        color: '#991b1b',
        borderColor: '#fecaca',
      };
    }
    return {
      backgroundColor: 'var(--bg-subtle)',
      color: 'var(--text-secondary)',
      borderColor: 'var(--border-subtle)',
    };
  };

  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        padding: '2px 8px',
        fontSize: '12px',
        fontWeight: 600,
        borderRadius: '9999px',
        border: '1px solid',
        textTransform: 'capitalize',
        letterSpacing: '0.02em',
        ...getBadgeStyle(),
      }}
    >
      {status}
    </span>
  );
};
