import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { User, Lock, Save, AlertCircle, CheckCircle2 } from 'lucide-react';
import { authApi } from '../api/authApi';
import { extractApiError, getAvatarFallback, loadProfileFromStorage, normalizeProfile, saveUserProfile, type UserProfile } from '../utils/authStorage';

const Profile = () => {
  const navigate = useNavigate();

  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [authProvider, setAuthProvider] = useState('local');
  const [customAvatarUrl, setCustomAvatarUrl] = useState('');

  const applyProfile = useCallback((profile: UserProfile) => {
    setFullName(profile.fullName);
    setPhone(profile.phone || '');
    setEmail(profile.email);
    setAvatarUrl(profile.avatarUrl || '');
    setAuthProvider(profile.authProvider);
    setCustomAvatarUrl(profile.avatarUrl || '');
    saveUserProfile(profile);
  }, []);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const [isLoading, setIsLoading] = useState(true);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [isSavingPassword, setIsSavingPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const isGoogleAccount = authProvider === 'google';
  const displayAvatar = avatarUrl || getAvatarFallback(fullName || email);

  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    if (!token) {
      navigate('/login');
      return;
    }

    const loadProfile = async () => {
      try {
        const response = await authApi.getProfile();
        applyProfile(normalizeProfile(response.data as unknown as Record<string, unknown>));
      } catch (error: any) {
        if (error.response?.status === 401) {
          navigate('/login');
          return;
        }

        const cached = loadProfileFromStorage();
        if (cached) applyProfile(cached);

        const status = error.response?.status;
        if (status === 404) {
          setErrorMessage('API hồ sơ chưa sẵn sàng. Hãy restart backend (dotnet run) rồi đăng nhập lại.');
        } else {
          setErrorMessage(extractApiError(error, 'Không thể tải hồ sơ từ máy chủ. Đang hiển thị dữ liệu đã lưu.'));
        }
      } finally {
        setIsLoading(false);
      }
    };

    loadProfile();
  }, [navigate, applyProfile]);

  const handleSaveProfile = async () => {
    if (!fullName.trim()) {
      setErrorMessage('Họ và tên không được để trống.');
      return;
    }

    if (phone.trim() && phone.includes('@')) {
      setErrorMessage('Số điện thoại không hợp lệ. Vui lòng xóa email khỏi ô SĐT (do trình duyệt tự điền).');
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

    setIsSavingPassword(true);
    setErrorMessage('');
    setSuccessMessage('');

    try {
      const response = await authApi.changePassword(currentPassword, newPassword, confirmPassword);
      setSuccessMessage(response.data.message || 'Đã đổi mật khẩu thành công.');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (error: any) {
      setErrorMessage(error.response?.data?.message || 'Không thể đổi mật khẩu.');
    } finally {
      setIsSavingPassword(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center bg-[#050A15] text-gray-400">
        Đang tải hồ sơ...
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full text-white bg-[#050A15] overflow-y-auto">
      <header className="h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 flex items-center px-8 sticky top-0 z-20 shrink-0">
        <div className="text-gray-300 font-medium tracking-wide">Quản lý Hồ sơ Cài đặt</div>
      </header>

      <div className="p-8 md:p-10 max-w-4xl mx-auto w-full">
        <h1 className="text-4xl font-bold tracking-tight mb-8">Hồ sơ cá nhân</h1>

        {errorMessage && (
          <div className="mb-6 bg-red-500/10 border border-red-500/20 text-red-400 p-4 rounded-xl flex items-center gap-3 text-sm">
            <AlertCircle size={18} className="shrink-0" />
            {errorMessage}
          </div>
        )}

        {successMessage && (
          <div className="mb-6 bg-green-500/10 border border-green-500/20 text-green-400 p-4 rounded-xl flex items-center gap-3 text-sm">
            <CheckCircle2 size={18} className="shrink-0" />
            {successMessage}
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div className="col-span-1 bg-[#0A101D] border border-white/5 rounded-2xl p-8 flex flex-col items-center justify-center text-center">
            <div className="w-32 h-32 rounded-full border-4 border-[#151B2B] bg-gray-700 mb-4 overflow-hidden">
              <img src={displayAvatar} alt="Avatar" className="w-full h-full object-cover" />
            </div>
            <h3 className="font-bold text-lg mb-1">{fullName}</h3>
            <p className="text-sm text-gray-500 mb-2">{email}</p>
            <p className="text-xs text-gray-600 mb-6 uppercase tracking-wider">
              {isGoogleAccount ? 'Đăng nhập qua Google' : 'Tài khoản Email'}
            </p>
            {isGoogleAccount ? (
              <p className="text-xs text-gray-500 leading-relaxed">
                Ảnh đại diện được đồng bộ tự động từ tài khoản Google mỗi lần đăng nhập.
              </p>
            ) : (
              <div className="w-full space-y-2">
                <label className="text-xs font-bold text-gray-400 uppercase text-left block">URL ảnh đại diện</label>
                <input
                  type="url"
                  value={customAvatarUrl}
                  onChange={(e) => setCustomAvatarUrl(e.target.value)}
                  placeholder="https://..."
                  className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-3 py-2 text-sm text-white focus:border-[#FF7575] focus:outline-none"
                />
              </div>
            )}
          </div>

          <div className="col-span-1 md:col-span-2 space-y-6">
            <div className="bg-[#0A101D] border border-white/5 rounded-2xl p-8">
              <h3 className="text-xl font-bold mb-6 flex items-center gap-2">
                <User className="text-[#FF7575]" /> Thông tin liên hệ
              </h3>
              <div className="space-y-5">
                <div className="space-y-2">
                  <label className="text-xs font-bold text-gray-400 uppercase">Họ và Tên</label>
                  <input
                    type="text"
                    name="fullName"
                    autoComplete="name"
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                    className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none"
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-xs font-bold text-gray-400 uppercase">Số điện thoại</label>
                  <input
                    type="tel"
                    name="phone"
                    autoComplete="tel"
                    inputMode="tel"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none"
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-xs font-bold text-gray-400 uppercase">Email (Không thể thay đổi)</label>
                  <div className="w-full bg-[#151B2B]/50 border border-white/5 rounded-lg px-4 py-3 text-gray-500 cursor-not-allowed">
                    {email}
                  </div>
                </div>
              </div>
              <button
                onClick={handleSaveProfile}
                disabled={isSavingProfile || !fullName.trim()}
                className="mt-6 bg-[#FF7575] hover:bg-[#ff6262] disabled:bg-gray-600 disabled:cursor-not-allowed text-white px-6 py-3 rounded-lg font-bold flex items-center gap-2 transition-colors"
              >
                <Save size={18} />
                {isSavingProfile ? 'Đang lưu...' : 'Lưu thông tin'}
              </button>
            </div>

            {!isGoogleAccount && (
              <div className="bg-[#0A101D] border border-white/5 rounded-2xl p-8">
                <h3 className="text-xl font-bold mb-6 flex items-center gap-2">
                  <Lock className="text-[#FF7575]" /> Đổi mật khẩu
                </h3>
                <div className="space-y-5">
                  <input
                    type="password"
                    name="currentPassword"
                    autoComplete="current-password"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    placeholder="Mật khẩu hiện tại"
                    className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none"
                  />
                  <input
                    type="password"
                    name="newPassword"
                    autoComplete="new-password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="Mật khẩu mới"
                    className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none"
                  />
                  <input
                    type="password"
                    name="confirmPassword"
                    autoComplete="new-password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    placeholder="Xác nhận mật khẩu mới"
                    className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none"
                  />
                </div>
                <button
                  onClick={handleChangePassword}
                  disabled={isSavingPassword}
                  className="mt-6 bg-[#151B2B] hover:bg-white/10 disabled:opacity-50 border border-white/10 text-white px-6 py-3 rounded-lg font-bold transition-colors"
                >
                  {isSavingPassword ? 'Đang cập nhật...' : 'Cập nhật mật khẩu'}
                </button>
              </div>
            )}

            {isGoogleAccount && (
              <div className="bg-[#0A101D] border border-white/5 rounded-2xl p-8 text-sm text-gray-400">
                Tài khoản đăng nhập bằng Google không sử dụng mật khẩu cục bộ trên hệ thống MCFH.
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default Profile;
