import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { User, Lock, Save, ShieldCheck, Mail, Phone, Image, CheckCircle2, AlertCircle, Sparkles } from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import { authApi } from '../../api/authApi';
import { extractApiError, getAccessToken, getAvatarFallback, loadProfileFromStorage, normalizeProfile, saveUserProfile, type UserProfile } from '../../utils/authStorage';
import { getPasswordValidationError, PASSWORD_REQUIREMENT_MESSAGE } from '../../utils/passwordValidation';

const AdminProfile = () => {
  const navigate = useNavigate();

  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [role, setRole] = useState('Admin');
  const [customAvatarUrl, setCustomAvatarUrl] = useState('');

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const [isLoading, setIsLoading] = useState(true);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [isSavingPassword, setIsSavingPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [passSuccessMessage, setPassSuccessMessage] = useState('');
  const [passErrorMessage, setPassErrorMessage] = useState('');

  const applyProfile = useCallback((profile: UserProfile) => {
    setFullName(profile.fullName);
    setPhone(profile.phone || '');
    setEmail(profile.email);
    setAvatarUrl(profile.avatarUrl || '');
    setRole(profile.role || 'Admin');
    setCustomAvatarUrl(profile.avatarUrl || '');
    saveUserProfile(profile);
  }, []);

  useEffect(() => {
    const token = getAccessToken();
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

        setErrorMessage(extractApiError(error, 'Không thể tải dữ liệu hồ sơ từ máy chủ. Đang hiển thị dữ liệu đã lưu.'));
      } finally {
        setIsLoading(false);
      }
    };

    loadProfile();
  }, [navigate, applyProfile]);

  const displayAvatar = customAvatarUrl.trim() || avatarUrl || getAvatarFallback(fullName || email);

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
        customAvatarUrl.trim() || undefined
      );
      applyProfile(normalizeProfile(response.data as unknown as Record<string, unknown>));
      setSuccessMessage('Đã cập nhật thông tin hồ sơ Admin thành công.');
    } catch (error: unknown) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật hồ sơ Admin.'));
    } finally {
      setIsSavingProfile(false);
    }
  };

  const handleChangePassword = async () => {
    setPassErrorMessage('');
    setPassSuccessMessage('');

    if (!currentPassword) {
      setPassErrorMessage('Vui lòng nhập mật khẩu hiện tại.');
      return;
    }

    const valErr = getPasswordValidationError(newPassword);
    if (valErr) {
      setPassErrorMessage(valErr);
      return;
    }

    if (newPassword !== confirmPassword) {
      setPassErrorMessage('Mật khẩu xác nhận không khớp.');
      return;
    }

    setIsSavingPassword(true);
    try {
      await authApi.changePassword(currentPassword, newPassword, confirmPassword);
      setPassSuccessMessage('Đổi mật khẩu thành công!');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (error: unknown) {
      setPassErrorMessage(extractApiError(error, 'Đổi mật khẩu thất bại. Vui lòng kiểm tra lại mật khẩu hiện tại.'));
    } finally {
      setIsSavingPassword(false);
    }
  };

  return (
    <AdminLayout adminName={fullName} adminRole="Quản trị viên hệ thống">
      <div className="max-w-4xl mx-auto space-y-6 pb-12">
        {/* Title Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-gray-900 flex items-center gap-2">
              <User className="w-6 h-6 text-red-500" />
              Chi Tiết & Cập Nhật Hồ Sơ Admin
            </h1>
            <p className="text-xs text-gray-500 mt-1">
              Quản lý thông tin tài khoản quản trị viên và thiết lập mật khẩu bảo mật hệ thống
            </p>
          </div>
          <span className="px-3 py-1 bg-red-50 text-[#ef4444] border border-red-200 rounded-full text-xs font-semibold flex items-center gap-1.5">
            <ShieldCheck className="w-3.5 h-3.5" />
            Super Admin
          </span>
        </div>

        {/* Profile Card Header */}
        <div className="bg-white border border-gray-200 rounded-2xl p-6 shadow-xs flex flex-col md:flex-row items-center gap-6">
          <div className="relative group">
            <div className="w-24 h-24 rounded-full overflow-hidden border-4 border-red-100 shadow-md">
              <img
                src={displayAvatar}
                alt={fullName}
                className="w-full h-full object-cover"
                onError={(e) => {
                  (e.target as HTMLImageElement).src = getAvatarFallback(fullName || email);
                }}
              />
            </div>
            <div className="absolute bottom-0 right-0 w-6 h-6 rounded-full bg-emerald-500 border-2 border-white flex items-center justify-center" title="Online / Active">
              <span className="w-2 h-2 rounded-full bg-white animate-ping" />
            </div>
          </div>

          <div className="flex-1 text-center md:text-left space-y-1">
            <div className="flex flex-col md:flex-row md:items-center gap-2">
              <h2 className="text-lg font-bold text-gray-900">{fullName || 'Admin User'}</h2>
              <span className="inline-block px-2.5 py-0.5 rounded text-[11px] font-semibold bg-gray-100 text-gray-700 w-max mx-auto md:mx-0">
                {role}
              </span>
            </div>
            <p className="text-xs text-gray-500 flex items-center justify-center md:justify-start gap-1.5">
              <Mail className="w-3.5 h-3.5 text-gray-400" />
              {email}
            </p>
            <p className="text-xs text-gray-400 pt-1">
              Quyền hạn: <span className="text-emerald-600 font-medium">Toàn quyền Quản trị & Thống kê Doanh thu (Full Access)</span>
            </p>
          </div>
        </div>

        {/* Form 1: Personal Info */}
        <div className="bg-white border border-gray-200 rounded-2xl p-6 shadow-xs">
          <h3 className="text-base font-bold text-gray-900 mb-4 pb-3 border-b border-gray-100 flex items-center gap-2">
            <Sparkles className="w-4 h-4 text-amber-500" />
            Thông Tin Cá Nhân Admin
          </h3>

          {errorMessage && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-xl flex items-center gap-2 text-xs text-red-700">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{errorMessage}</span>
            </div>
          )}

          {successMessage && (
            <div className="mb-4 p-3 bg-emerald-50 border border-emerald-200 rounded-xl flex items-center gap-2 text-xs text-emerald-700">
              <CheckCircle2 className="w-4 h-4 shrink-0" />
              <span>{successMessage}</span>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <div>
              <label className="block text-xs font-semibold text-gray-700 mb-1.5">
                Họ và Tên Admin <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <User className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="text"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  placeholder="Nhập họ và tên..."
                  className="w-full bg-gray-50 border border-gray-200 rounded-xl pl-9 pr-4 py-2.5 text-xs text-gray-900 focus:bg-white focus:border-red-500 focus:outline-none transition-all"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-700 mb-1.5">
                Email Quản Trị (Không thể sửa)
              </label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="email"
                  value={email}
                  disabled
                  className="w-full bg-gray-100 border border-gray-200 rounded-xl pl-9 pr-4 py-2.5 text-xs text-gray-500 cursor-not-allowed"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-700 mb-1.5">
                Số Điện Thoại Liên Hệ
              </label>
              <div className="relative">
                <Phone className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="text"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  placeholder="Nhập số điện thoại..."
                  className="w-full bg-gray-50 border border-gray-200 rounded-xl pl-9 pr-4 py-2.5 text-xs text-gray-900 focus:bg-white focus:border-red-500 focus:outline-none transition-all"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-700 mb-1.5">
                URL Ảnh Đại Diện (Avatar Image URL)
              </label>
              <div className="relative">
                <Image className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="text"
                  value={customAvatarUrl}
                  onChange={(e) => setCustomAvatarUrl(e.target.value)}
                  placeholder="https://example.com/avatar.jpg"
                  className="w-full bg-gray-50 border border-gray-200 rounded-xl pl-9 pr-4 py-2.5 text-xs text-gray-900 focus:bg-white focus:border-red-500 focus:outline-none transition-all"
                />
              </div>
            </div>
          </div>

          <div className="mt-6 flex justify-end">
            <button
              type="button"
              onClick={handleSaveProfile}
              disabled={isSavingProfile || isLoading}
              className="px-5 py-2.5 bg-[#ef4444] hover:bg-red-600 text-white rounded-xl text-xs font-semibold flex items-center gap-2 shadow-xs hover:shadow transition-all disabled:opacity-50 cursor-pointer"
            >
              <Save className="w-4 h-4" />
              {isSavingProfile ? 'Đang lưu...' : 'Lưu Thay Đổi Thông Tin'}
            </button>
          </div>
        </div>

        {/* Form 2: Change Password */}
        <div className="bg-white border border-gray-200 rounded-2xl p-6 shadow-xs">
          <h3 className="text-base font-bold text-gray-900 mb-4 pb-3 border-b border-gray-100 flex items-center gap-2">
            <Lock className="w-4 h-4 text-red-500" />
            Đổi Mật Khẩu Bảo Mật Tài Khoản
          </h3>

          {passErrorMessage && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-xl flex items-center gap-2 text-xs text-red-700">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{passErrorMessage}</span>
            </div>
          )}

          {passSuccessMessage && (
            <div className="mb-4 p-3 bg-emerald-50 border border-emerald-200 rounded-xl flex items-center gap-2 text-xs text-emerald-700">
              <CheckCircle2 className="w-4 h-4 shrink-0" />
              <span>{passSuccessMessage}</span>
            </div>
          )}

          <div className="space-y-4 max-w-lg">
            <div>
              <label className="block text-xs font-semibold text-gray-700 mb-1.5">
                Mật Khẩu Hiện Tại <span className="text-red-500">*</span>
              </label>
              <input
                type="password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                placeholder="Nhập mật khẩu hiện tại..."
                className="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-2.5 text-xs text-gray-900 focus:bg-white focus:border-red-500 focus:outline-none transition-all"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-700 mb-1.5">
                Mật Khẩu Mới <span className="text-red-500">*</span>
              </label>
              <input
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="Nhập mật khẩu mới..."
                className="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-2.5 text-xs text-gray-900 focus:bg-white focus:border-red-500 focus:outline-none transition-all"
              />
              <p className="text-[11px] text-gray-400 mt-1">{PASSWORD_REQUIREMENT_MESSAGE}</p>
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-700 mb-1.5">
                Xác Nhận Mật Khẩu Mới <span className="text-red-500">*</span>
              </label>
              <input
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Nhập lại mật khẩu mới..."
                className="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-2.5 text-xs text-gray-900 focus:bg-white focus:border-red-500 focus:outline-none transition-all"
              />
            </div>
          </div>

          <div className="mt-6 flex justify-end">
            <button
              type="button"
              onClick={handleChangePassword}
              disabled={isSavingPassword}
              className="px-5 py-2.5 bg-gray-900 hover:bg-black text-white rounded-xl text-xs font-semibold flex items-center gap-2 shadow-xs hover:shadow transition-all disabled:opacity-50 cursor-pointer"
            >
              <Lock className="w-4 h-4" />
              {isSavingPassword ? 'Đang cập nhật...' : 'Đổi Mật Khẩu'}
            </button>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
};

export default AdminProfile;
