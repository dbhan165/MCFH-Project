import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Lock, AlertCircle, CheckCircle2, ShieldCheck, Sparkles, ArrowLeft } from 'lucide-react';
import { authApi } from '../api/authApi';
import { extractApiError } from '../utils/authStorage';
import { getPasswordValidationError, PASSWORD_REQUIREMENT_MESSAGE } from '../utils/passwordValidation';
import McfhLogo from '../components/brand/McfhLogo';
import loginImage from '../assets/login.png';

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

    const passwordError = getPasswordValidationError(password);
    if (passwordError) {
      setErrorMessage(passwordError);
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
    <div className="min-h-screen flex w-full font-sans selection:bg-[#00B4D8] selection:text-white bg-white">

      <div className="hidden lg:flex lg:w-5/12 bg-[#050A15] relative flex-col justify-between p-12 overflow-hidden border-r border-[#151B2B]">
        <div
          className="absolute inset-0 z-0 opacity-10 pointer-events-none"
          style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
        ></div>
        <div className="absolute top-[-10%] left-[-20%] w-[500px] h-[500px] bg-[#00B4D8] rounded-full mix-blend-screen filter blur-[120px] opacity-20 animate-pulse-slow"></div>
        <div className="absolute bottom-[-10%] right-[-10%] w-[400px] h-[400px] bg-[#3B82F6] rounded-full mix-blend-screen filter blur-[100px] opacity-20"></div>

        <div className="relative z-50 mb-8">
          <McfhLogo linkTo="/" size={36} textClassName="text-white text-xl" />
        </div>

        <div className="relative z-10 grow flex items-center justify-center mb-12">
          <div className="w-full max-w-md aspect-4/5 bg-linear-to-b from-[#151B2B]/80 to-[#0A101D]/90 backdrop-blur-xl border border-white/10 rounded-3xl p-6 shadow-[0_0_40px_rgba(0,180,216,0.1)] flex items-center justify-center overflow-hidden transform hover:scale-[1.02] transition-transform duration-700 ease-out">
            <img src={loginImage} alt="AI Funnel" className="w-full h-full object-cover rounded-xl filter contrast-125" />
          </div>
        </div>

        <div className="relative z-10 bg-[#0A101D]/60 backdrop-blur-xl border border-white/10 p-8 rounded-3xl shadow-2xl">
          <div className="flex items-center gap-3 mb-6">
            <Sparkles className="text-[#00B4D8] w-6 h-6" />
            <h3 className="text-white text-2xl font-bold tracking-tight">Bảo mật tài khoản</h3>
          </div>
          <ul className="space-y-4">
            <li className="flex items-center gap-4 text-gray-300 text-sm font-medium hover:text-white transition-colors">
              <div className="bg-[#00B4D8]/10 p-1.5 rounded-full"><CheckCircle2 className="text-[#00B4D8] w-4 h-4" /></div>
              Mã hoá mật khẩu theo chuẩn BCrypt
            </li>
            <li className="flex items-center gap-4 text-gray-300 text-sm font-medium hover:text-white transition-colors">
              <div className="bg-[#00B4D8]/10 p-1.5 rounded-full"><CheckCircle2 className="text-[#00B4D8] w-4 h-4" /></div>
              Token khôi phục có thời hạn giới hạn
            </li>
            <li className="flex items-center gap-4 text-gray-300 text-sm font-medium hover:text-white transition-colors">
              <div className="bg-[#00B4D8]/10 p-1.5 rounded-full"><CheckCircle2 className="text-[#00B4D8] w-4 h-4" /></div>
              Tự động vô hiệu hoá token cũ khi đặt lại
            </li>
          </ul>
        </div>
      </div>

      <div className="w-full lg:w-7/12 bg-[#FAFAFA] flex items-center justify-center p-8 sm:p-12 overflow-y-auto relative">
        <div className="w-full max-w-[440px] space-y-8 my-auto relative z-10">

          <Link
            to="/"
            className="inline-flex items-center gap-2 text-sm font-semibold text-gray-500 hover:text-[#0A101D] transition-colors"
          >
            <ArrowLeft className="w-4 h-4" />
            Quay lại trang chủ
          </Link>

          <div className="animate-in fade-in slide-in-from-bottom-4 duration-500 ease-out">
            <McfhLogo size={44} textClassName="text-[#0A101D] text-3xl" subtitle="Social Listening" subtitleClassName="text-xs text-[#00B4D8] font-bold tracking-[0.2em] uppercase" className="mb-6" />
            <h2 className="text-3xl font-extrabold text-gray-900 mb-3 tracking-tight">
              Đặt lại mật khẩu
            </h2>
            <p className="text-gray-500 text-sm leading-relaxed font-medium">
              Vui lòng nhập mật khẩu mới cho tài khoản của bạn. Mật khẩu phải đáp ứng các yêu cầu bảo mật bên dưới.
            </p>
          </div>

          {errorMessage && (
            <div className="bg-red-50 text-red-600 p-4 rounded-xl flex items-center gap-3 text-sm font-semibold border border-red-100 animate-in fade-in zoom-in-95 duration-300">
              <AlertCircle className="w-5 h-5 shrink-0" />
              {errorMessage}
            </div>
          )}

          {successMessage && (
            <div className="bg-green-50 text-green-700 p-4 rounded-xl flex items-center gap-3 text-sm font-semibold border border-green-100 animate-in fade-in zoom-in-95 duration-300">
              <CheckCircle2 className="w-5 h-5 shrink-0" />
              {successMessage}
            </div>
          )}

          {!token && (
            <div className="bg-amber-50 text-amber-700 p-4 rounded-xl flex items-center gap-3 text-sm font-semibold border border-amber-100 animate-in fade-in zoom-in-95 duration-300">
              <ShieldCheck className="w-5 h-5 shrink-0" />
              Không tìm thấy token khôi phục. Vui lòng mở lại email và bấm vào nút "Đặt Lại Mật Khẩu".
            </div>
          )}

          <div className="animate-in fade-in slide-in-from-bottom-4 duration-700 ease-out">
            <form className="space-y-5" onSubmit={handleSubmit}>

              <div className="space-y-2">
                <label className="text-[13px] font-bold text-gray-700 tracking-wider">Mật khẩu mới</label>
                <div className="relative group">
                  <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Lock className="h-5 w-5 text-gray-400 group-focus-within:text-[#00B4D8] transition-colors" /></div>
                  <input
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Ví dụ: MatKhau@1"
                    className="w-full pl-12 pr-4 py-3.5 bg-white border border-gray-200 text-gray-900 placeholder-gray-400 rounded-xl focus:outline-none focus:ring-4 focus:ring-[#00B4D8]/15 focus:border-[#00B4D8] transition-all shadow-sm hover:border-gray-300"
                    required
                    disabled={isLoading || !token}
                  />
                </div>
                <p className="text-xs text-gray-500 leading-relaxed">
                  {PASSWORD_REQUIREMENT_MESSAGE}
                </p>
              </div>

              <div className="space-y-2">
                <label className="text-[13px] font-bold text-gray-700 tracking-wider">Xác nhận mật khẩu</label>
                <div className="relative group">
                  <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Lock className="h-5 w-5 text-gray-400 group-focus-within:text-[#00B4D8] transition-colors" /></div>
                  <input
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    placeholder="Nhập lại mật khẩu mới"
                    className="w-full pl-12 pr-4 py-3.5 bg-white border border-gray-200 text-gray-900 placeholder-gray-400 rounded-xl focus:outline-none focus:ring-4 focus:ring-[#00B4D8]/15 focus:border-[#00B4D8] transition-all shadow-sm hover:border-gray-300"
                    required
                    disabled={isLoading || !token}
                  />
                </div>
              </div>

              <button
                type="submit"
                disabled={isLoading || !token}
                className="w-full bg-gradient-to-r from-[#00B4D8] to-[#3B82F6] hover:from-[#0693B0] hover:to-[#2563EB] disabled:from-gray-400 disabled:to-gray-500 disabled:cursor-not-allowed text-white font-bold py-4 rounded-xl transition-all duration-300 shadow-[0_8px_20px_rgba(0,180,216,0.3)] hover:shadow-[0_12px_25px_rgba(0,180,216,0.4)] active:scale-[0.98] flex justify-center items-center mt-4"
              >
                {isLoading ? (
                  <span className="flex items-center gap-2">
                    <svg className="animate-spin h-5 w-5 text-white/70" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Đang xử lý...
                  </span>
                ) : (
                  "Cập nhật mật khẩu"
                )}
              </button>
            </form>

            <div className="text-center mt-8">
              <div className="bg-gray-100/50 py-3 rounded-xl border border-gray-200/60 inline-block px-6">
                <span className="text-sm font-medium text-gray-600">Đã nhớ mật khẩu? </span>
                <Link
                  to="/login"
                  className="text-sm font-bold text-[#00B4D8] hover:text-[#0693B0] transition-colors ml-1"
                >
                  Đăng nhập tại đây
                </Link>
              </div>
            </div>
          </div>

        </div>
      </div>
    </div>
  );
};

export default ResetPassword;
