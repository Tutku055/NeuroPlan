import React, { InputHTMLAttributes, ReactNode } from 'react';

interface InputFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  icon?: ReactNode;
}

export const InputField: React.FC<InputFieldProps> = ({ icon, className = '', style, ...props }) => {
  return (
    <div style={{ position: 'relative', width: '100%' }}>
      {icon && (
        <div style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }}>
          {icon}
        </div>
      )}
      <input 
        className={`input-field ${className}`} 
        style={{ paddingLeft: icon ? '3rem' : '1rem', ...style }}
        {...props}
      />
    </div>
  );
}
