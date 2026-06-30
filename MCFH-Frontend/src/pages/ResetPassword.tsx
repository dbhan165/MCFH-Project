import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Lock, ArrowLeft, AlertCircle, CheckCircle2 } from 'lucide-react';
import { authApi } from '../api/authApi';
import { extractApiError } from '../utils/authStorage';
import McfhLogo from '../components/brand/McfhLogo';

const ResetPassword = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const navigate = useNavigate();

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) {
      setErrorMessage('Link khôi phục không hợp lệ. Vui lòng yêu cầu gửi lại email.');
      return;
    }
    if (password !== confirmPassword) {
      setErrorMessage('Mật khẩu xác nhận không khớp.');
      return;
    }

    setIsLoading(true);
    setErrorMessage('');
    setSuccessMessage('');
    try {
      await authApi.resetPassword(token, password, confirmPassword);
      setSuccessMessage('Đặt lại mật khẩu thành công! Đang chuyển tới trang đăng nhập...');
      setTimeout(() => navigate('/login'), 2000);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể đặt lại mật khẩu. Link có thể đã hết hạn.'));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#FAFAFA] flex items-center justify-center p-6 font-sans">
      <div className="w-full max-w-md space-y-6">
        <McfhLogo linkTo="/" size={36} textClassName="text-[#0A101D] text-xl" />
        <Link to="/login" className="inline-flex items-center gap-2 text-sm font-semibold text-gray-500 hover:text-[#0A101D]">
          <ArrowLeft className="w-4 h-4" />
          Quay lại đăng nhập
        </Link>

        <div className="bg-white border border-gray-200 rounded-2xl p-8 shadow-sm">
          <h1 className="text-2xl font-bold text-[#0A101D] mb-2">Đặt lại mật khẩu</h1>
          <p className="text-sm text-gray-500 mb-6">Nhập mật khẩu mới cho tài khoản của bạn.</p>

          {errorMessage && (
            <div className="mb-4 bg-red-50 text-red-600 p-3 rounded-lg flex items-center gap-2 text-sm">
              <AlertCircle className="w-4 h-4 shrink-0" />
              {errorMessage}
            </div>
          )}
          {successMessage && (
            <div className="mb-4 bg-green-50 text-green-700 p-3 rounded-lg flex items-center gap-2 text-sm">
              <CheckCircle2 className="w-4 h-4 shrink-0" />
              {successMessage}
            </div>
          )}

          <form className="space-y-4" onSubmit={handleSubmit}>
            <div>
              <label className="text-sm font-semibold text-gray-700">Mật khẩu mới</label>
              <div className="relative mt-1">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full pl-10 pr-4 py-3 bg-[#0A101D] text-white rounded-xl border-0"
                  required
                  minLength={6}
                  disabled={isLoading}
                />
              </div>
            </div>
            <div>
              <label className="text-sm font-semibold text-gray-700">Xác nhận mật khẩu</label>
              <div className="relative mt-1">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                <input
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className="w-full pl-10 pr-4 py-3 bg-[#0A101D] text-white rounded-xl border-0"
                  required
                  minLength={6}
                  disabled={isLoading}
                />
              </div>
            </div>
            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-[#0A101D] hover:bg-[#1A2235] disabled:opacity-50 text-white font-bold py-3 rounded-xl"
            >
              {isLoading ? 'Đang xử lý...' : 'Cập nhật mật khẩu'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default ResetPassword;
