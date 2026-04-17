import React, { ButtonHTMLAttributes, ReactNode } from 'react';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  children: ReactNode;
  icon?: ReactNode;
  variant?: 'primary' | 'secondary' | 'danger';
}

export const Button: React.FC<ButtonProps> = ({ children, icon, variant = 'primary', className = '', style, ...props }) => {
  let bgColor = 'var(--accent-primary)';
  let color = '#fff';
  let boxSh = '0 4px 15px var(--accent-glow)';

  if (variant === 'secondary') {
    bgColor = 'var(--glass-bg)';
    color = 'var(--text-main)';
    boxSh = 'none';
  } else if (variant === 'danger') {
    bgColor = 'rgba(239, 68, 68, 0.1)';
    color = '#fca5a5';
    boxSh = 'none';
  }

  return (
    <button 
      className={`btn-primary ${className}`} 
      style={{ background: bgColor, color, boxShadow: boxSh, ...style }}
      {...props}
    >
      {icon}
      {children}
    </button>
  );
}
