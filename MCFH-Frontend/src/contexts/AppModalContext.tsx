import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import AlertModal, { type AlertModalType } from '../components/common/AlertModal';
import ConfirmModal from '../components/common/ConfirmModal';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  type?: 'danger' | 'warning';
}

export interface AlertOptions {
  title: string;
  message: string;
  confirmText?: string;
  type?: AlertModalType;
}

interface AppModalContextValue {
  confirm: (options: ConfirmOptions) => Promise<boolean>;
  alert: (options: AlertOptions) => Promise<void>;
}

const AppModalContext = createContext<AppModalContextValue | null>(null);

export function AppModalProvider({ children }: { children: ReactNode }) {
  const confirmResolver = useRef<((value: boolean) => void) | null>(null);
  const alertResolver = useRef<(() => void) | null>(null);

  const [confirmState, setConfirmState] = useState<
    (ConfirmOptions & { isOpen: boolean; isLoading: boolean }) | null
  >(null);
  const [alertState, setAlertState] = useState<
    (AlertOptions & { isOpen: boolean }) | null
  >(null);

  const closeConfirm = useCallback((result: boolean) => {
    confirmResolver.current?.(result);
    confirmResolver.current = null;
    setConfirmState(null);
  }, []);

  const closeAlert = useCallback(() => {
    alertResolver.current?.();
    alertResolver.current = null;
    setAlertState(null);
  }, []);

  const confirm = useCallback((options: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => {
      confirmResolver.current?.(false);
      confirmResolver.current = resolve;
      setConfirmState({ ...options, isOpen: true, isLoading: false });
    });
  }, []);

  const alert = useCallback((options: AlertOptions) => {
    return new Promise<void>((resolve) => {
      alertResolver.current?.();
      alertResolver.current = resolve;
      setAlertState({ ...options, isOpen: true });
    });
  }, []);

  const value = useMemo(() => ({ confirm, alert }), [confirm, alert]);

  return (
    <AppModalContext.Provider value={value}>
      {children}

      {confirmState?.isOpen && (
        <ConfirmModal
          isOpen
          title={confirmState.title}
          message={confirmState.message}
          confirmText={confirmState.confirmText}
          cancelText={confirmState.cancelText}
          type={confirmState.type}
          isLoading={confirmState.isLoading}
          onClose={() => closeConfirm(false)}
          onConfirm={() => closeConfirm(true)}
        />
      )}

      {alertState?.isOpen && (
        <AlertModal
          isOpen
          title={alertState.title}
          message={alertState.message}
          confirmText={alertState.confirmText}
          type={alertState.type}
          onClose={closeAlert}
        />
      )}
    </AppModalContext.Provider>
  );
}

export function useAppModal(): AppModalContextValue {
  const ctx = useContext(AppModalContext);
  if (!ctx) {
    throw new Error('useAppModal phải dùng bên trong AppModalProvider');
  }
  return ctx;
}
