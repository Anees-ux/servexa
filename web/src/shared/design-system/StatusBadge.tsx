import React from 'react';

interface StatusBadgeProps {
  status: string;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status }) => {
  const getBadgeStyle = (): React.CSSProperties => {
    const s = status.toLowerCase();
    if (s === 'active') {
      return {
        backgroundColor: 'var(--success-subtle)',
        color: 'var(--success-text)',
        borderColor: '#a7f3d0',
      };
    }
    if (s === 'onhold') {
      return {
        backgroundColor: 'var(--warning-subtle)',
        color: 'var(--warning-text)',
        borderColor: '#fde68a',
      };
    }
    if (s === 'prospect') {
      return {
        backgroundColor: 'var(--info-subtle)',
        color: 'var(--info-text)',
        borderColor: '#bfdbfe',
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
