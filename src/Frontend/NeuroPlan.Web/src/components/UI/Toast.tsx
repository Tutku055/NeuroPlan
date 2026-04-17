import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { X, CheckCircle, AlertTriangle, Info, XCircle } from 'lucide-react';

/* ─── Types ─── */
export type ToastType = 'success' | 'error' | 'warning' | 'info';

interface ToastItem {
  id: string;
  type: ToastType;
  title: string;
  message?: string;
  duration?: number;
}

interface ToastContextValue {
  toast: (type: ToastType, title: string, message?: string, duration?: number) => void;
  confirm: (title: string, message?: string) => Promise<boolean>;
}

/* ─── Context ─── */
const ToastContext = createContext<ToastContextValue>({
  toast: () => {},
  confirm: async () => false,
});

export const useToast = () => useContext(ToastContext);

/* ─── Colours ─── */
const TOAST_STYLES: Record<ToastType, { bg: string; border: string; icon: string; IconComp: React.FC<{ size: number }> }> = {
  success: {
    bg: 'rgba(34,197,94,0.1)',
    border: 'rgba(34,197,94,0.3)',
    icon: '#22c55e',
    IconComp: ({ size }) => <CheckCircle size={size} color="#22c55e" />,
  },
  error: {
    bg: 'rgba(239,68,68,0.1)',
    border: 'rgba(239,68,68,0.3)',
    icon: '#ef4444',
    IconComp: ({ size }) => <XCircle size={size} color="#ef4444" />,
  },
  warning: {
    bg: 'rgba(245,158,11,0.1)',
    border: 'rgba(245,158,11,0.3)',
    icon: '#f59e0b',
    IconComp: ({ size }) => <AlertTriangle size={size} color="#f59e0b" />,
  },
  info: {
    bg: 'rgba(14,165,233,0.1)',
    border: 'rgba(14,165,233,0.3)',
    icon: '#0ea5e9',
    IconComp: ({ size }) => <Info size={size} color="#0ea5e9" />,
  },
};

/* ─── Confirm Dialog ─── */
interface ConfirmDialogProps {
  title: string;
  message?: string;
  onConfirm: () => void;
  onCancel: () => void;
}

const ConfirmDialog: React.FC<ConfirmDialogProps> = ({ title, message, onConfirm, onCancel }) => {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onCancel();
      if (e.key === 'Enter') onConfirm();
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [onConfirm, onCancel]);

  return (
    <div
      style={{
        position: 'fixed', inset: 0, zIndex: 2000,
        background: 'rgba(0,0,0,0.65)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        padding: '1rem',
        backdropFilter: 'blur(8px)',
        animation: 'fadeIn 0.15s ease',
      }}
    >
      <div
        className="glass-panel"
        style={{
          width: '100%',
          maxWidth: 420,
          padding: '1.75rem',
          display: 'flex',
          flexDirection: 'column',
          gap: '1rem',
          border: '1px solid rgba(239,68,68,0.2)',
          boxShadow: '0 24px 80px rgba(0,0,0,0.7)',
          animation: 'scaleIn 0.15s ease',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <div style={{
            width: 40, height: 40, borderRadius: '50%',
            background: 'rgba(239,68,68,0.12)',
            border: '1px solid rgba(239,68,68,0.25)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            flexShrink: 0,
          }}>
            <AlertTriangle size={18} color="#ef4444" />
          </div>
          <div>
            <h3 style={{ margin: 0, fontSize: '1rem', fontWeight: 600 }}>{title}</h3>
            {message && (
              <p style={{ margin: '0.2rem 0 0', fontSize: '0.84rem', color: 'var(--text-muted)', lineHeight: 1.5 }}>
                {message}
              </p>
            )}
          </div>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end', marginTop: '0.5rem' }}>
          <button
            onClick={onCancel}
            style={{
              padding: '0.55rem 1.2rem',
              background: 'rgba(255,255,255,0.06)',
              border: '1px solid var(--glass-border)',
              color: 'var(--text-main)',
              borderRadius: 8,
              cursor: 'pointer',
              fontSize: '0.875rem',
              fontFamily: 'var(--font-family)',
              fontWeight: 500,
              transition: 'all 0.15s',
            }}
            onMouseEnter={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.1)')}
            onMouseLeave={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.06)')}
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            autoFocus
            style={{
              padding: '0.55rem 1.2rem',
              background: 'rgba(239,68,68,0.15)',
              border: '1px solid rgba(239,68,68,0.3)',
              color: '#fca5a5',
              borderRadius: 8,
              cursor: 'pointer',
              fontSize: '0.875rem',
              fontFamily: 'var(--font-family)',
              fontWeight: 600,
              transition: 'all 0.15s',
            }}
            onMouseEnter={e => (e.currentTarget.style.background = 'rgba(239,68,68,0.25)')}
            onMouseLeave={e => (e.currentTarget.style.background = 'rgba(239,68,68,0.15)')}
          >
            Confirm
          </button>
        </div>
      </div>
    </div>
  );
};

/* ─── Single Toast Card ─── */
const ToastCard: React.FC<{ item: ToastItem; onRemove: (id: string) => void }> = ({ item, onRemove }) => {
  const [visible, setVisible] = useState(false);
  const style = TOAST_STYLES[item.type];

  useEffect(() => {
    // Trigger enter animation
    const t1 = setTimeout(() => setVisible(true), 10);
    const dur = item.duration ?? 4000;
    const t2 = setTimeout(() => {
      setVisible(false);
      setTimeout(() => onRemove(item.id), 350);
    }, dur);
    return () => { clearTimeout(t1); clearTimeout(t2); };
  }, [item.id, item.duration, onRemove]);

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: '0.75rem',
        padding: '0.9rem 1rem',
        background: style.bg,
        border: `1px solid ${style.border}`,
        borderRadius: 12,
        backdropFilter: 'blur(16px)',
        boxShadow: '0 8px 30px rgba(0,0,0,0.4)',
        minWidth: 280,
        maxWidth: 380,
        transform: visible ? 'translateX(0) scale(1)' : 'translateX(100%) scale(0.95)',
        opacity: visible ? 1 : 0,
        transition: 'transform 0.3s cubic-bezier(0.34,1.56,0.64,1), opacity 0.3s ease',
        pointerEvents: 'all',
      }}
    >
      <div style={{ flexShrink: 0, marginTop: 2 }}>
        <style.IconComp size={17} />
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        <p style={{ margin: 0, fontWeight: 600, fontSize: '0.875rem', color: 'var(--text-main)' }}>
          {item.title}
        </p>
        {item.message && (
          <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-muted)', lineHeight: 1.4 }}>
            {item.message}
          </p>
        )}
      </div>
      <button
        onClick={() => { setVisible(false); setTimeout(() => onRemove(item.id), 350); }}
        style={{
          background: 'none', border: 'none', cursor: 'pointer',
          color: 'var(--text-muted)', padding: '0.1rem', borderRadius: 4,
          display: 'flex', flexShrink: 0, transition: 'color 0.15s',
        }}
        onMouseEnter={e => (e.currentTarget.style.color = 'var(--text-main)')}
        onMouseLeave={e => (e.currentTarget.style.color = 'var(--text-muted)')}
      >
        <X size={13} />
      </button>
    </div>
  );
};

/* ─── Provider ─── */
export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const [confirmState, setConfirmState] = useState<{
    title: string;
    message?: string;
    resolve: (val: boolean) => void;
  } | null>(null);

  const removeToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id));
  }, []);

  const toast = useCallback((type: ToastType, title: string, message?: string, duration?: number) => {
    const id = Math.random().toString(36).slice(2);
    setToasts(prev => [...prev, { id, type, title, message, duration }]);
  }, []);

  const confirm = useCallback((title: string, message?: string): Promise<boolean> => {
    return new Promise(resolve => {
      setConfirmState({ title, message, resolve });
    });
  }, []);

  const handleConfirm = () => {
    confirmState?.resolve(true);
    setConfirmState(null);
  };

  const handleCancel = () => {
    confirmState?.resolve(false);
    setConfirmState(null);
  };

  return (
    <ToastContext.Provider value={{ toast, confirm }}>
      {children}

      {/* Toast container */}
      <div
        style={{
          position: 'fixed',
          bottom: '1.5rem',
          right: '1.5rem',
          display: 'flex',
          flexDirection: 'column',
          gap: '0.65rem',
          zIndex: 3000,
          pointerEvents: 'none',
          alignItems: 'flex-end',
        }}
      >
        {toasts.map(item => (
          <ToastCard key={item.id} item={item} onRemove={removeToast} />
        ))}
      </div>

      {/* Confirm dialog */}
      {confirmState && (
        <ConfirmDialog
          title={confirmState.title}
          message={confirmState.message}
          onConfirm={handleConfirm}
          onCancel={handleCancel}
        />
      )}
    </ToastContext.Provider>
  );
};
