import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Bell, Building2, Check, Loader2, Mail, X } from 'lucide-react';
import { meApi } from '../api/meApi';
import type { ReceivedInvitation } from '../types/workspace';
import { extractApiError } from '../utils/authStorage';
import { formatWorkspaceDateTime } from '../utils/workspaceHelpers';

const Invitations = () => {
  const navigate = useNavigate();
  const [invitations, setInvitations] = useState<ReceivedInvitation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [actingId, setActingId] = useState<number | null>(null);

  const loadInvitations = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await meApi.getInvitations();
      setInvitations(data);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải lời mời.'));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadInvitations();
  }, [loadInvitations]);

  const handleAccept = async (invitation: ReceivedInvitation) => {
    setActingId(invitation.invitationId);
    try {
      const message = await meApi.acceptInvitation(invitation.invitationId);
      setSuccessMessage(message);
      await loadInvitations();
      setTimeout(() => {
        navigate(`/workspace/${invitation.workspaceId}/projects`);
      }, 1200);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể chấp nhận lời mời.'));
    } finally {
      setActingId(null);
    }
  };

  const handleDecline = async (invitation: ReceivedInvitation) => {
    setActingId(invitation.invitationId);
    try {
      const message = await meApi.declineInvitation(invitation.invitationId);
      setSuccessMessage(message);
      await loadInvitations();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể từ chối lời mời.'));
    } finally {
      setActingId(null);
    }
  };

  return (
    <div className="min-h-full text-white p-8 md:p-10">
      <div className="max-w-3xl mx-auto">
        <div className="flex items-center gap-3 mb-2">
          <div className="w-12 h-12 rounded-xl bg-[#FF7575]/10 flex items-center justify-center">
            <Bell className="w-6 h-6 text-[#FF7575]" />
          </div>
          <div>
            <h1 className="text-3xl font-bold">Lời mời tham gia tổ chức</h1>
            <p className="text-gray-400 text-sm mt-1">
              Chấp nhận hoặc từ chối lời mời vào workspace
            </p>
          </div>
        </div>

        {errorMessage && (
          <div className="mt-6 p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-300 text-sm">
            {errorMessage}
          </div>
        )}
        {successMessage && (
          <div className="mt-6 p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 text-sm">
            {successMessage}
          </div>
        )}

        {isLoading ? (
          <div className="flex flex-col items-center justify-center py-24 text-gray-400 gap-4">
            <Loader2 className="w-10 h-10 animate-spin text-[#FF7575]" />
            <p>Đang tải lời mời...</p>
          </div>
        ) : invitations.length === 0 ? (
          <div className="mt-10 p-10 rounded-2xl border border-white/5 bg-[#0A101D] text-center">
            <Mail className="w-12 h-12 mx-auto text-gray-600 mb-4" />
            <p className="text-gray-400">Bạn chưa có lời mời nào đang chờ.</p>
          </div>
        ) : (
          <div className="mt-8 space-y-4">
            {invitations.map((inv) => (
              <div
                key={inv.invitationId}
                className="p-6 rounded-2xl border border-white/10 bg-[#0A101D] flex flex-col sm:flex-row sm:items-center justify-between gap-4"
              >
                <div className="flex items-start gap-4">
                  <div className="w-12 h-12 rounded-xl bg-[#151B2B] border border-white/10 flex items-center justify-center shrink-0">
                    <Building2 className="w-6 h-6 text-[#00B4D8]" />
                  </div>
                  <div>
                    <h3 className="font-bold text-lg">{inv.workspaceName}</h3>
                    <p className="text-sm text-gray-400 mt-1">
                      <strong className="text-gray-300">{inv.invitedByName}</strong> mời bạn tham gia
                    </p>
                    <p className="text-xs text-gray-500 mt-2">
                      {formatWorkspaceDateTime(inv.createdAt)}
                    </p>
                  </div>
                </div>

                <div className="flex gap-3 shrink-0">
                  <button
                    type="button"
                    disabled={actingId === inv.invitationId}
                    onClick={() => handleDecline(inv)}
                    className="px-4 py-2.5 rounded-lg border border-white/10 text-gray-300 hover:bg-white/5 text-sm font-semibold flex items-center gap-2 disabled:opacity-50"
                  >
                    <X className="w-4 h-4" /> Từ chối
                  </button>
                  <button
                    type="button"
                    disabled={actingId === inv.invitationId}
                    onClick={() => handleAccept(inv)}
                    className="px-4 py-2.5 rounded-lg bg-[#FF7575] hover:bg-[#ff6262] text-white text-sm font-semibold flex items-center gap-2 disabled:opacity-50"
                  >
                    {actingId === inv.invitationId ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Check className="w-4 h-4" />
                    )}
                    Chấp nhận
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default Invitations;
