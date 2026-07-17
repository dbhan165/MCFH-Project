import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Bell, Check, Loader2, Mail, AlertTriangle } from 'lucide-react';
import { meApi, type AppNotification } from '../../api/meApi';
import { formatWorkspaceDateTime } from '../../utils/workspaceHelpers';

interface NotificationBellProps {
  theme?: 'light' | 'dark';
}

const NotificationBell = ({ theme = 'dark' }: NotificationBellProps) => {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);

  const refresh = useCallback(async () => {
    try {
      const [count, list] = await Promise.all([
        meApi.getUnreadCount(),
        meApi.getNotifications(15),
      ]);
      setUnreadCount(count);
      setNotifications(list);
    } catch {
      setUnreadCount(0);
      setNotifications([]);
    }
  }, []);

  useEffect(() => {
    refresh();
    const timer = window.setInterval(refresh, 60_000);
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
        <Bell className={theme === 'dark' ? "w-5 h-5" : "w-5 h-5"} />
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 rounded-full bg-[#FF7575] text-white text-[10px] font-bold flex items-center justify-center">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-full mt-2 w-80 sm:w-96 bg-[#151B2B] border border-white/10 rounded-2xl shadow-2xl z-50 overflow-hidden">
          <div className="px-4 py-3 border-b border-white/5 flex items-center justify-between">
            <span className="font-bold text-sm">Thông báo</span>
            {inviteCount > 0 && (
              <Link
                to="/invitations"
                onClick={() => setOpen(false)}
                className="text-xs text-[#FF7575] hover:underline"
              >
                {inviteCount} lời mời
              </Link>
            )}
          </div>

          <div className="max-h-80 overflow-y-auto">
            {isLoading ? (
              <div className="py-10 flex justify-center">
                <Loader2 className="w-6 h-6 animate-spin text-[#FF7575]" />
              </div>
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

          <div className="p-3 border-t border-white/5">
            <Link
              to="/invitations"
              onClick={() => setOpen(false)}
              className="block text-center text-xs font-semibold text-[#00B4D8] hover:underline"
            >
              Xem tất cả lời mời
            </Link>
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationBell;
