import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import AlertModal, { type AlertModalType } from '../components/common/AlertModal';
import ConfirmModal from '../components/common/ConfirmModal';

type ConfirmOptions = {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  type?: 'danger' | 'warning';
};

type AlertOptions = {
  title: string;
  message: string;
  confirmText?: string;
  type?: AlertModalType;
};

type ConfirmState = ConfirmOptions & {
  kind: 'confirm';
  isLoading: boolean;
  resolve: (value: boolean) => void;
};

type AlertState = AlertOptions & {
  kind: 'alert';
  resolve: () => void;
};

type ModalState = ConfirmState | AlertState | null;

type AppModalContextValue = {
  confirm: (options: ConfirmOptions) => Promise<boolean>;
  alert: (options: AlertOptions) => Promise<void>;
  setConfirmLoading: (loading: boolean) => void;
};

const AppModalContext = createContext<AppModalContextValue | null>(null);

export function AppModalProvider({ children }: { children: ReactNode }) {
  const [modal, setModal] = useState<ModalState>(null);

  const closeModal = useCallback(() => setModal(null), []);

  const confirm = useCallback((options: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => {
      setModal({
        kind: 'confirm',
        ...options,
        isLoading: false,
        resolve: (value) => {
          resolve(value);
          closeModal();
        },
      });
    });
  }, [closeModal]);

  const alert = useCallback((options: AlertOptions) => {
    return new Promise<void>((resolve) => {
      setModal({
        kind: 'alert',
        ...options,
        resolve: () => {
          resolve();
          closeModal();
        },
      });
    });
  }, [closeModal]);

  const setConfirmLoading = useCallback((loading: boolean) => {
    setModal((current) => {
      if (!current || current.kind !== 'confirm') return current;
      return { ...current, isLoading: loading };
    });
  }, []);

  const value = useMemo(
    () => ({ confirm, alert, setConfirmLoading }),
    [confirm, alert, setConfirmLoading]
  );

  return (
    <AppModalContext.Provider value={value}>
      {children}

      {modal?.kind === 'confirm' && (
        <ConfirmModal
          isOpen
          title={modal.title}
          message={modal.message}
          confirmText={modal.confirmText}
          cancelText={modal.cancelText}
          type={modal.type}
          isLoading={modal.isLoading}
          onClose={() => modal.resolve(false)}
          onConfirm={() => modal.resolve(true)}
        />
      )}

      {modal?.kind === 'alert' && (
        <AlertModal
          isOpen
          title={modal.title}
          message={modal.message}
          confirmText={modal.confirmText}
          type={modal.type}
          onClose={() => modal.resolve()}
        />
      )}
    </AppModalContext.Provider>
  );
}

export function useAppModal() {
  const ctx = useContext(AppModalContext);
  if (!ctx) {
    throw new Error('useAppModal must be used within AppModalProvider');
  }
  return ctx;
}
