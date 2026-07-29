import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Bell, Check, Loader2, Mail, AlertTriangle, ArrowDownLeft, Landmark, CheckCircle2 } from 'lucide-react';
import { meApi, type AppNotification } from '../../api/meApi';
import { adminApi, type AdminRecentRevenueTransaction } from '../../api/portalApi';
import { formatWorkspaceDateTime } from '../../utils/workspaceHelpers';

interface NotificationBellProps {
  theme?: 'light' | 'dark';
  isAdmin?: boolean;
}

const formatVND = (amount: number) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
};

const NotificationBell = ({ theme = 'dark', isAdmin = false }: NotificationBellProps) => {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [revenueTransactions, setRevenueTransactions] = useState<AdminRecentRevenueTransaction[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);

  const refresh = useCallback(async () => {
    try {
      if (isAdmin) {
        const dashboard = await adminApi.getDashboard();
        const txs = dashboard.recentRevenueTransactions || [];
        setRevenueTransactions(txs);
        setUnreadCount(txs.length);
      } else {
        const [count, list] = await Promise.all([
          meApi.getUnreadCount(),
          meApi.getNotifications(15),
        ]);
        setUnreadCount(count);
        setNotifications(list);
      }
    } catch {
      setUnreadCount(0);
      setNotifications([]);
      setRevenueTransactions([]);
    }
  }, [isAdmin]);

  useEffect(() => {
    refresh();
    const timer = window.setInterval(refresh, 30_000);
    return () => window.clearInterval(timer);
  }, [refresh]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (panelRef.current && !panelRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleOpen = async () => {
    const next = !open;
    setOpen(next);
    if (next) {
      setIsLoading(true);
      await refresh();
      setIsLoading(false);
    }
  };

  const handleNotificationClick = async (notification: AppNotification) => {
    if (!notification.isRead) {
      await meApi.markNotificationRead(notification.notificationId);
      await refresh();
    }

    if (notification.type === 'workspace_invite') {
      setOpen(false);
      navigate('/invitations');
      return;
    }

    if (notification.type === 'crisis_alert' && notification.workspaceId && notification.projectId) {
      setOpen(false);
      navigate(`/workspace/${notification.workspaceId}/project/${notification.projectId}/sentiment`);
      return;
    }

    if ((notification.type === 'scrape_completed' || notification.type === 'scrape_failed') && notification.workspaceId && notification.projectId) {
      setOpen(false);
      navigate(`/workspace/${notification.workspaceId}/project/${notification.projectId}`);
      return;
    }

    if (notification.type === 'success' && notification.relatedType === 'scrape_order' && notification.workspaceId && notification.relatedId) {
      setOpen(false);
      navigate(`/workspace/${notification.workspaceId}/orders/${notification.relatedId}`);
      return;
    }

    if (
      (notification.type === 'bespoke_revision_request' ||
        notification.type === 'bespoke_assigned' ||
        notification.type === 'bespoke_ready' ||
        notification.type === 'bespoke_delivered') &&
      notification.relatedId
    ) {
      setOpen(false);
      if (notification.type === 'bespoke_ready' || notification.type === 'bespoke_delivered') {
        if (notification.workspaceId) {
          navigate(`/workspace/${notification.workspaceId}/project/bespoke-reports`);
        }
      } else {
        navigate(`/reporter/requests/${notification.relatedId}`);
      }
      return;
    }
  };

  const inviteCount = notifications.filter(
    (n) => n.type === 'workspace_invite' && !n.isRead
  ).length;

  return (
    <div className="relative" ref={panelRef}>
      <button
        type="button"
        onClick={handleOpen}
        className={
          theme === 'dark'
            ? "relative p-2.5 rounded-xl text-gray-400 hover:text-white hover:bg-white/5 border border-transparent hover:border-white/10 transition-colors"
            : "relative p-2 rounded-lg text-gray-500 hover:text-[#111827] hover:bg-gray-50 transition-colors"
        }
        aria-label="Thông báo"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 rounded-full bg-[#ef4444] text-white text-[10px] font-bold flex items-center justify-center animate-pulse">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div className={`absolute right-0 top-full mt-2 w-84 sm:w-96 rounded-2xl shadow-2xl z-50 overflow-hidden border ${
          theme === 'dark' ? 'bg-[#151B2B] border-white/10 text-white' : 'bg-white border-gray-200 text-gray-900'
        }`}>
          <div className={`px-4 py-3 border-b flex items-center justify-between ${
            theme === 'dark' ? 'border-white/5' : 'border-gray-100'
          }`}>
            <div className="flex items-center gap-2">
              {isAdmin ? (
                <Landmark className="w-4 h-4 text-emerald-500" />
              ) : (
                <Bell className="w-4 h-4 text-blue-500" />
              )}
              <span className="font-bold text-sm">
                {isAdmin ? 'Thông Báo Doanh Thu (App Ngân Hàng)' : 'Thông báo'}
              </span>
            </div>
            {inviteCount > 0 && !isAdmin && (
              <Link
                to="/invitations"
                onClick={() => setOpen(false)}
                className="text-xs text-[#FF7575] hover:underline"
              >
                {inviteCount} lời mời
              </Link>
            )}
          </div>

          <div className="max-h-96 overflow-y-auto p-2 space-y-2">
            {isLoading ? (
              <div className="py-10 flex justify-center">
                <Loader2 className="w-6 h-6 animate-spin text-emerald-500" />
              </div>
            ) : isAdmin ? (
              revenueTransactions.length === 0 ? (
                <p className="py-10 text-center text-xs text-gray-400">Chưa có thông báo biến động số dư nào</p>
              ) : (
                revenueTransactions.map((tx) => (
                  <div
                    key={tx.paymentId}
                    className="p-3 bg-gradient-to-br from-emerald-50/60 to-teal-50/40 border border-emerald-200/70 rounded-xl hover:shadow-sm transition-all"
                  >
                    <div className="flex items-start justify-between gap-2 mb-1.5">
                      <div className="flex items-center gap-2">
                        <div className="w-7 h-7 rounded-full bg-emerald-500/15 text-emerald-600 flex items-center justify-center shrink-0">
                          <ArrowDownLeft className="w-4 h-4 stroke-[2.5]" />
                        </div>
                        <div>
                          <p className="text-[11px] font-bold text-emerald-800 uppercase tracking-wide">
                            THÔNG BÁO BIẾN ĐỘNG SỐ DƯ (+)
                          </p>
                          <p className="text-xs font-semibold text-gray-900">{tx.userName}</p>
                        </div>
                      </div>
                      <span className="text-sm font-extrabold text-emerald-600 shrink-0">
                        + {formatVND(tx.amount)}
                      </span>
                    </div>

                    <div className="text-xs space-y-1 bg-white/70 backdrop-blur-xs p-2.5 rounded-lg border border-emerald-100/60 text-gray-700">
                      <div className="flex items-center justify-between">
                        <span className="text-gray-500">Tính năng:</span>
                        <span className="font-semibold text-gray-900">{tx.featureName}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-gray-500">Email User:</span>
                        <span className="text-gray-700">{tx.userEmail}</span>
                      </div>
                      <div className="flex items-center justify-between pt-1 border-t border-gray-100 text-[11px]">
                        <span className="font-mono text-gray-400">Mã GD: {tx.transactionRef}</span>
                        <span className="text-emerald-700 font-medium flex items-center gap-1">
                          <CheckCircle2 className="w-3 h-3" /> Thành công
                        </span>
                      </div>
                    </div>

                    <div className="mt-1.5 text-right text-[10px] text-gray-400 font-medium">
                      {formatWorkspaceDateTime(tx.paidAt)}
                    </div>
                  </div>
                ))
              )
            ) : notifications.length === 0 ? (
              <p className="py-10 text-center text-sm text-gray-500">Không có thông báo</p>
            ) : (
              notifications.map((n) => (
                <button
                  key={n.notificationId}
                  type="button"
                  onClick={() => handleNotificationClick(n)}
                  className={`w-full text-left px-4 py-3 border-b border-white/5 hover:bg-white/5 transition-colors ${
                    !n.isRead ? 'bg-[#FF7575]/5' : ''
                  }`}
                >
                  <div className="flex items-start gap-3">
                    <div className="mt-0.5 w-8 h-8 rounded-lg bg-white/5 flex items-center justify-center shrink-0">
                      {n.type === 'workspace_invite' ? (
                        <Mail className="w-4 h-4 text-[#FF7575]" />
                      ) : n.type === 'crisis_alert' ? (
                        <AlertTriangle className="w-4 h-4 text-amber-400" />
                      ) : n.type === 'scrape_failed' ? (
                        <AlertTriangle className="w-4 h-4 text-[#FF7575]" />
                      ) : (
                        <Check className="w-4 h-4 text-emerald-400" />
                      )}
                    </div>
                    <div className="min-w-0">
                      <p className="text-sm font-semibold text-white truncate">{n.title}</p>
                      {n.message && (
                        <p className="text-xs text-gray-400 mt-1 line-clamp-2">{n.message}</p>
                      )}
                      <p className="text-[10px] text-gray-500 mt-1">
                        {formatWorkspaceDateTime(n.createdAt)}
                      </p>
                    </div>
                  </div>
                </button>
              ))
            )}
          </div>

          {!isAdmin && (
            <div className="p-3 border-t border-white/5">
              <Link
                to="/invitations"
                onClick={() => setOpen(false)}
                className="block text-center text-xs font-semibold text-[#00B4D8] hover:underline"
              >
                Xem tất cả lời mời
              </Link>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default NotificationBell;
