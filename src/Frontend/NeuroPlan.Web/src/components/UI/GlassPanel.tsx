import React, { ReactNode, CSSProperties } from 'react';

interface GlassPanelProps {
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
  onClick?: () => void;
}

export const GlassPanel: React.FC<GlassPanelProps> = ({ children, className = '', style, onClick }) => {
  return (
    <div 
      className={`glass-panel ${className}`} 
      style={style}
      onClick={onClick}
    >
      {children}
    </div>
  );
}
