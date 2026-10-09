import React from 'react';
import { Card } from '../../shared/design-system/Card';

export const DispatchView: React.FC = () => (
  <Card title="Dispatch & Scheduling Grid">
    <div style={{ padding: '20px 0', color: 'var(--text-secondary)' }}>
      <p style={{ marginBottom: '12px' }}>
        Resource scheduling concurrency guard, booking windows, and technician assignments.
      </p>
      <div
        style={{
          padding: '16px',
          backgroundColor: 'var(--bg-subtle)',
          borderRadius: 'var(--radius-md)',
          border: '1px solid var(--border-subtle)',
        }}
      >
        <strong>Next Batch:</strong> Resource scheduling, territory filtering, and shift commitment.
      </div>
    </div>
  </Card>
);
