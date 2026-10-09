import React from 'react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  disabled,
  style,
  ...props
}) => {
  const getVariantStyles = (): React.CSSProperties => {
    switch (variant) {
      case 'primary':
        return {
          backgroundColor: 'var(--primary)',
          color: 'var(--text-inverse)',
          border: '1px solid var(--primary)',
        };
      case 'secondary':
        return {
          backgroundColor: 'var(--bg-subtle)',
          color: 'var(--text-primary)',
          border: '1px solid var(--border-subtle)',
        };
      case 'outline':
        return {
          backgroundColor: 'transparent',
          color: 'var(--text-primary)',
          border: '1px solid var(--border-strong)',
        };
      case 'danger':
        return {
          backgroundColor: 'var(--danger)',
          color: 'var(--text-inverse)',
          border: '1px solid var(--danger)',
        };
    }
  };

  const getSizeStyles = (): React.CSSProperties => {
    switch (size) {
      case 'sm':
        return { padding: '4px 10px', fontSize: '13px' };
      case 'lg':
        return { padding: '10px 20px', fontSize: '15px' };
      case 'md':
      default:
        return { padding: '7px 14px', fontSize: '14px' };
    }
  };

  return (
    <button
      disabled={disabled || isLoading}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: '6px',
        fontWeight: 500,
        borderRadius: 'var(--radius-md)',
        cursor: disabled || isLoading ? 'not-allowed' : 'pointer',
        opacity: disabled || isLoading ? 0.6 : 1,
        transition: 'all 0.15s ease',
        ...getSizeStyles(),
        ...getVariantStyles(),
        ...style,
      }}
      {...props}
    >
      {isLoading ? 'Processing...' : children}
    </button>
  );
};
