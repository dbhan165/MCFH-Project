import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertCircle, CheckCircle2, Lock, Loader2, Save, User } from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';
import { authApi } from '../../api/authApi';
import {
  extractApiError,
  getAccessToken,
  getAvatarFallback,
  loadProfileFromStorage,
  normalizeProfile,
  saveUserProfile,
  type UserProfile,
} from '../../utils/authStorage';
import { getPasswordValidationError, PASSWORD_REQUIREMENT_MESSAGE } from '../../utils/passwordValidation';

export default function ReporterProfile() {
  const navigate = useNavigate();

  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [authProvider, setAuthProvider] = useState('local');
  const [customAvatarUrl, setCustomAvatarUrl] = useState('');
  const [role, setRole] = useState('Reporter');

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const [isLoading, setIsLoading] = useState(true);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [isSavingPassword, setIsSavingPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const isGoogleAccount = authProvider === 'google';
  const previewAvatar = (customAvatarUrl.trim() || avatarUrl) || getAvatarFallback(fullName || email);
  const displayAvatar = previewAvatar;

  const applyProfile = useCallback((profile: UserProfile) => {
    setFullName(profile.fullName);
    setPhone(profile.phone || '');
    setEmail(profile.email);
    setAvatarUrl(profile.avatarUrl || '');
    setAuthProvider(profile.authProvider);
    setCustomAvatarUrl(profile.avatarUrl || '');
    setRole(profile.role || 'Reporter');
    saveUserProfile(profile);
  }, []);

  useEffect(() => {
    if (!getAccessToken()) {
      navigate('/login');
      return;
    }

    const load = async () => {
      try {
        const response = await authApi.getProfile();
        applyProfile(normalizeProfile(response.data as unknown as Record<string, unknown>));
      } catch (error: unknown) {
        const cached = loadProfileFromStorage();
        if (cached) applyProfile(cached);
        setErrorMessage(extractApiError(error, 'Không thể tải hồ sơ. Đang hiển thị dữ liệu đã lưu.'));
      } finally {
        setIsLoading(false);
      }
    };
    load();
  }, [navigate, applyProfile]);

  const handleSaveProfile = async () => {
    if (!fullName.trim()) {
      setErrorMessage('Họ và tên không được để trống.');
      return;
    }
    setIsSavingProfile(true);
    setErrorMessage('');
    setSuccessMessage('');
    try {
      const response = await authApi.updateProfile(
        fullName.trim(),
        phone.trim(),
        isGoogleAccount ? undefined : customAvatarUrl.trim() || undefined
      );
      applyProfile(normalizeProfile(response.data as unknown as Record<string, unknown>));
      setSuccessMessage('Đã cập nhật hồ sơ thành công.');
    } catch (error: unknown) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật hồ sơ.'));
    } finally {
      setIsSavingProfile(false);
    }
  };

  const handleChangePassword = async () => {
    if (!currentPassword || !newPassword || !confirmPassword) {
      setErrorMessage('Vui lòng điền đầy đủ thông tin mật khẩu.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setErrorMessage('Mật khẩu xác nhận không khớp.');
      return;
    }
    const passwordError = getPasswordValidationError(newPassword);
    if (passwordError) {
      setErrorMessage(passwordError);
      return;
    }

    setIsSavingPassword(true);
    setErrorMessage('');
    setSuccessMessage('');
    try {
      const response = await authApi.changePassword(currentPassword, newPassword, confirmPassword);
      setSuccessMessage(response.data.message || 'Đã đổi mật khẩu thành công.');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (error: unknown) {
      setErrorMessage(extractApiError(error, 'Không thể đổi mật khẩu.'));
    } finally {
      setIsSavingPassword(false);
    }
  };

  if (isLoading) {
    return (
      <ReporterLayout>
        <div className="flex justify-center py-24 text-stone-500 gap-3">
          <Loader2 className="w-8 h-8 animate-spin text-[#e11d48]" />
          Đang tải hồ sơ...
        </div>
      </ReporterLayout>
    );
  }

  return (
    <ReporterLayout>
      <div className="mb-8">
        <h2 className="text-xl lg:text-2xl font-bold text-[#111827]">Hồ sơ cá nhân</h2>
        <p className="text-sm text-[#78716c] mt-1">Thông tin tài khoản Reporter</p>
      </div>

      {errorMessage && (
        <div className="mb-6 flex items-center gap-2 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-3">
          <AlertCircle className="w-4 h-4 shrink-0" />
          {errorMessage}
        </div>
      )}
      {successMessage && (
        <div className="mb-6 flex items-center gap-2 text-sm text-emerald-700 bg-emerald-50 border border-emerald-100 rounded-lg px-4 py-3">
          <CheckCircle2 className="w-4 h-4 shrink-0" />
          {successMessage}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="bg-white border border-stone-200 rounded-xl p-6 shadow-sm flex flex-col items-center text-center">
          <div className="w-28 h-28 rounded-full border-2 border-stone-200 overflow-hidden mb-4 bg-stone-100">
            <img src={displayAvatar} alt="Avatar" className="w-full h-full object-cover" />
          </div>
          <h3 className="font-bold text-lg text-[#111827]">{fullName || '—'}</h3>
          <p className="text-sm text-stone-500 mt-1">{email}</p>
          <span className="mt-3 text-[10px] font-bold uppercase tracking-wider px-2.5 py-1 rounded-full bg-rose-50 text-[#e11d48]">
            {role}
          </span>
          {isGoogleAccount && (
            <p className="text-xs text-stone-500 mt-4 leading-relaxed">
              Ảnh đại diện đồng bộ từ Google khi đăng nhập.
            </p>
          )}
        </div>

        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white border border-stone-200 rounded-xl p-6 shadow-sm">
            <h3 className="text-lg font-bold text-[#111827] mb-5 flex items-center gap-2">
              <User className="w-5 h-5" /> Thông tin cơ bản
            </h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-bold text-stone-500 mb-1.5">Họ và tên</label>
                <input
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  className="w-full bg-stone-50 border border-stone-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-rose-400"
                />
              </div>
              <div>
                <label className="block text-xs font-bold text-stone-500 mb-1.5">Số điện thoại</label>
                <input
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  className="w-full bg-stone-50 border border-stone-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-rose-400"
                />
              </div>
              <div className="sm:col-span-2">
                <label className="block text-xs font-bold text-stone-500 mb-1.5">Email</label>
                <input
                  value={email}
                  disabled
                  className="w-full bg-stone-100 border border-stone-200 rounded-lg px-3 py-2.5 text-sm text-stone-500"
                />
              </div>
              {!isGoogleAccount && (
                <div className="sm:col-span-2">
                  <label className="block text-xs font-bold text-stone-500 mb-1.5">URL ảnh đại diện</label>
                  <input
                    type="url"
                    value={customAvatarUrl}
                    onChange={(e) => setCustomAvatarUrl(e.target.value)}
                    placeholder="https://example.com/avatar.jpg"
                    className="w-full bg-stone-50 border border-stone-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-rose-400"
                  />
                  <p className="text-xs text-stone-400 mt-1.5">
                    Dán link ảnh rồi bấm «Lưu hồ sơ» bên dưới để áp dụng.
                  </p>
                </div>
              )}
            </div>
            <button
              type="button"
              onClick={handleSaveProfile}
              disabled={isSavingProfile}
              className="mt-5 inline-flex items-center gap-2 px-5 py-2.5 bg-[#e11d48] hover:bg-[#be123c] disabled:opacity-50 text-white rounded-lg text-sm font-semibold"
            >
              {isSavingProfile ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
              Lưu hồ sơ
            </button>
          </div>

          {!isGoogleAccount && (
            <div className="bg-white border border-stone-200 rounded-xl p-6 shadow-sm">
              <h3 className="text-lg font-bold text-[#111827] mb-2 flex items-center gap-2">
                <Lock className="w-5 h-5" /> Đổi mật khẩu
              </h3>
              <p className="text-xs text-stone-500 mb-5">{PASSWORD_REQUIREMENT_MESSAGE}</p>
              <div className="space-y-4">
                <input
                  type="password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  placeholder="Mật khẩu hiện tại"
                  className="w-full bg-stone-50 border border-stone-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-rose-400"
                />
                <input
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  placeholder="Mật khẩu mới"
                  className="w-full bg-stone-50 border border-stone-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-rose-400"
                />
                <input
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  placeholder="Xác nhận mật khẩu mới"
                  className="w-full bg-stone-50 border border-stone-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-rose-400"
                />
              </div>
              <button
                type="button"
                onClick={handleChangePassword}
                disabled={isSavingPassword}
                className="mt-5 inline-flex items-center gap-2 px-5 py-2.5 border-2 border-[#e11d48] text-[#e11d48] hover:bg-rose-50 disabled:opacity-50 rounded-lg text-sm font-semibold"
              >
                {isSavingPassword ? <Loader2 className="w-4 h-4 animate-spin" /> : <Lock className="w-4 h-4" />}
                Đổi mật khẩu
              </button>
            </div>
          )}
        </div>
      </div>
    </ReporterLayout>
  );
}
