import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Mail, Lock, CheckCircle2, AlertCircle, User, Phone, ArrowLeft, ShieldCheck, Sparkles } from 'lucide-react';
import { GoogleLogin } from '@react-oauth/google';
import { authApi } from '../api/authApi';
import loginImage from '../assets/login.png';

type AuthMode = 'login' | 'register' | 'forgot' | 'verify-otp';

const Login = () => {
  const [mode, setMode] = useState<AuthMode>('login');

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [otp, setOtp] = useState('');
  
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState(''); 
  
  const navigate = useNavigate();

  // ==========================================
  // XỬ LÝ SUBMIT (LOGIN / REGISTER / FORGOT / VERIFY)
  // ==========================================
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setErrorMessage('');
    setSuccessMessage('');

    try {
      if (mode === 'login') {
        const response = await authApi.login(email, password);
        const userData = response.data;
        
        localStorage.setItem('accessToken', userData.token);
        localStorage.setItem('userRole', userData.role);
        localStorage.setItem('fullName', userData.fullName);
        
        navigate('/workspaces'); 

      } else if (mode === 'register') {
        const response = await authApi.register(fullName, email, phone, password);
        setSuccessMessage(response.data.message || "Đăng ký thành công! Vui lòng kiểm tra email để lấy mã xác thực.");
        setMode('verify-otp'); 

      } else if (mode === 'verify-otp') {
        const response = await authApi.verifyEmail(email, otp); 
        setSuccessMessage(response.data.message || "Xác thực thành công! Vui lòng đăng nhập.");
        setMode('login');
        setPassword('');
        setOtp('');

      } else if (mode === 'forgot') {
        const response = await authApi.forgotPassword(email);
        setSuccessMessage(response.data.message || "Hệ thống đã gửi link khôi phục. Vui lòng kiểm tra hộp thư!");
      }
    } catch (error: any) {
      if (error.response && error.response.data) {
        setErrorMessage(error.response.data.message);
      } else {
        setErrorMessage('Không thể kết nối đến máy chủ. Vui lòng kiểm tra mạng!');
      }
    } finally {
      setIsLoading(false);
    }
  };

  // ==========================================
  // XỬ LÝ GỬI LẠI MÃ OTP
  // ==========================================
  const handleResendOtp = async () => {
    setIsLoading(true);
    setErrorMessage('');
    setSuccessMessage('');
    try {
      const response = await authApi.resendOtp(email);
      setSuccessMessage(response.data.message || "Đã gửi lại mã OTP mới. Vui lòng kiểm tra hộp thư.");
    } catch (error: any) {
      if (error.response && error.response.data) {
        setErrorMessage(error.response.data.message);
      } else {
        setErrorMessage('Lỗi khi gửi lại mã OTP.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex w-full font-sans selection:bg-[#00B4D8] selection:text-white bg-white">
      
      {/* ==========================================
          CỘT TRÁI: KHÔNG GIAN DARK MODE & HIỆU ỨNG
          ========================================== */}
      <div className="hidden lg:flex lg:w-5/12 bg-[#050A15] relative flex-col justify-between p-12 overflow-hidden border-r border-[#151B2B]">
        
        {/* Lưới Grid và Gradient Ambient Orbs siêu mượt */}
        <div 
          className="absolute inset-0 z-0 opacity-10 pointer-events-none"
          style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
        ></div>
        <div className="absolute top-[-10%] left-[-20%] w-[500px] h-[500px] bg-[#00B4D8] rounded-full mix-blend-screen filter blur-[120px] opacity-20 animate-pulse-slow"></div>
        <div className="absolute bottom-[-10%] right-[-10%] w-[400px] h-[400px] bg-[#3B82F6] rounded-full mix-blend-screen filter blur-[100px] opacity-20"></div>

        <div className="relative z-10 grow flex items-center justify-center mb-12">
          <div className="w-full max-w-md aspect-4/5 bg-linear-to-b from-[#151B2B]/80 to-[#0A101D]/90 backdrop-blur-xl border border-white/10 rounded-3xl p-6 shadow-[0_0_40px_rgba(0,180,216,0.1)] flex items-center justify-center overflow-hidden transform hover:scale-[1.02] transition-transform duration-700 ease-out">
            <img src={loginImage} alt="AI Funnel" className="w-full h-full object-cover rounded-xl filter contrast-125" />
          </div>
        </div>

        {/* Card tính năng (Glassmorphism) */}
        <div className="relative z-10 bg-[#0A101D]/60 backdrop-blur-xl border border-white/10 p-8 rounded-3xl shadow-2xl">
          <div className="flex items-center gap-3 mb-6">
            <Sparkles className="text-[#00B4D8] w-6 h-6" />
            <h3 className="text-white text-2xl font-bold tracking-tight">MCFH Update v2.0</h3>
          </div>
          <ul className="space-y-4">
            <li className="flex items-center gap-4 text-gray-300 text-sm font-medium hover:text-white transition-colors">
              <div className="bg-[#00B4D8]/10 p-1.5 rounded-full"><CheckCircle2 className="text-[#00B4D8] w-4 h-4" /></div>
              Tích hợp SignalR Real-time Connection
            </li>
            <li className="flex items-center gap-4 text-gray-300 text-sm font-medium hover:text-white transition-colors">
              <div className="bg-[#00B4D8]/10 p-1.5 rounded-full"><CheckCircle2 className="text-[#00B4D8] w-4 h-4" /></div>
              AI Aspect Extraction & Sentiment
            </li>
            <li className="flex items-center gap-4 text-gray-300 text-sm font-medium hover:text-white transition-colors">
              <div className="bg-[#00B4D8]/10 p-1.5 rounded-full"><CheckCircle2 className="text-[#00B4D8] w-4 h-4" /></div>
              VNPay Checkout Gateway Security
            </li>
          </ul>
        </div>
      </div>

      {/* ==========================================
          CỘT PHẢI: FORM ĐIỀN THÔNG TIN (LIGHT MODE)
          ========================================== */}
      <div className="w-full lg:w-7/12 bg-[#FAFAFA] flex items-center justify-center p-8 sm:p-12 overflow-y-auto relative">
        <div className="w-full max-w-[440px] space-y-8 my-auto relative z-10">
          
          {/* Header kèm Animation */}
          <div key={`header-${mode}`} className="animate-in fade-in slide-in-from-bottom-4 duration-500 ease-out">
            <h1 className="text-[#0A101D] text-4xl font-extrabold tracking-tight mb-4">MCFH.</h1>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">
              {mode === 'login' && "Đăng nhập không gian làm việc"}
              {mode === 'register' && "Tạo tài khoản mới"}
              {mode === 'forgot' && "Khôi phục mật khẩu"}
              {mode === 'verify-otp' && "Xác thực tài khoản"}
            </h2>
            <p className="text-gray-500 text-sm leading-relaxed">
              {mode === 'login' && "Chào mừng trở lại! Vui lòng điền thông tin để tiếp tục."}
              {mode === 'register' && "Điền thông tin bên dưới để trải nghiệm hệ thống ngay hôm nay."}
              {mode === 'forgot' && "Nhập email của bạn, chúng tôi sẽ gửi hướng dẫn khôi phục qua hòm thư."}
              {mode === 'verify-otp' && `Vui lòng nhập mã OTP gồm 6 số vừa được gửi tới ${email}.`}
            </p>
          </div>

          {/* Thông báo lỗi/thành công */}
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

          {/* Form Content - Bao bọc bởi thẻ div có key để kích hoạt animation khi đổi mode */}
          <div key={`form-${mode}`} className="animate-in fade-in slide-in-from-bottom-4 duration-700 ease-out">
            <form className="space-y-5" onSubmit={handleSubmit}>
              
              {mode === 'verify-otp' ? (
                <div className="space-y-2">
                  <label className="text-[13px] font-bold text-gray-700 tracking-wider">Mã xác thực (OTP)</label>
                  <div className="relative">
                    <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><ShieldCheck className="h-5 w-5 text-gray-400" /></div>
                    <input 
                      type="text" 
                      value={otp} 
                      onChange={(e) => setOtp(e.target.value)} 
                      placeholder="Nhập 6 số OTP..." 
                      maxLength={6}
                      className="w-full pl-12 pr-4 py-4 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-xl focus:outline-none focus:ring-0 transition-all shadow-sm" 
                      required 
                      disabled={isLoading} 
                    />
                  </div>
                  <div className="flex justify-end pt-2">
                    <button type="button" onClick={handleResendOtp} disabled={isLoading} className="text-sm font-bold text-[#00B4D8] hover:text-[#0693B0] transition-colors">
                      Chưa nhận được? Gửi lại mã
                    </button>
                  </div>
                </div>
              ) : (
                <>
                  {mode === 'register' && (
                    <>
                      <div className="space-y-2">
                        <label className="text-[13px] font-bold text-gray-700 tracking-wider">Họ và Tên</label>
                        <div className="relative">
                          <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><User className="h-5 w-5 text-gray-400" /></div>
                          <input type="text" value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Nguyễn Văn A" className="w-full pl-12 pr-4 py-4 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-xl focus:outline-none focus:ring-0 transition-all shadow-sm" required disabled={isLoading} />
                        </div>
                      </div>
                      <div className="space-y-2">
                        <label className="text-[13px] font-bold text-gray-700 tracking-wider">Số điện thoại</label>
                        <div className="relative">
                          <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Phone className="h-5 w-5 text-gray-400" /></div>
                          <input type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="0987654321" className="w-full pl-12 pr-4 py-4 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-xl focus:outline-none focus:ring-0 transition-all shadow-sm" required disabled={isLoading} />
                        </div>
                      </div>
                    </>
                  )}

                  <div className="space-y-2">
                    <label className="text-[13px] font-bold text-gray-700 tracking-wider">Email doanh nghiệp</label>
                    <div className="relative">
                      <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Mail className="h-5 w-5 text-gray-400" /></div>
                      <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="name@company.com" className="w-full pl-12 pr-4 py-4 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-xl focus:outline-none focus:ring-0 transition-all shadow-sm" required disabled={isLoading} />
                    </div>
                  </div>

                  {mode !== 'forgot' && (
                    <div className="space-y-2">
                      <label className="text-[13px] font-bold text-gray-700 tracking-wider">Mật khẩu</label>
                      <div className="relative">
                        <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Lock className="h-5 w-5 text-gray-400" /></div>
                        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" className="w-full pl-12 pr-4 py-4 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-xl focus:outline-none focus:ring-0 transition-all shadow-sm" required disabled={isLoading} />
                      </div>
                    </div>
                  )}
                </>
              )}

              {mode === 'login' && (
                <div className="flex items-center justify-between pt-1">
                  <label className="flex items-center gap-2 cursor-pointer group">
                    <input type="checkbox" className="w-4 h-4 rounded-md border-gray-300 text-[#0A101D] focus:ring-[#0A101D] cursor-pointer transition-all" />
                    <span className="text-sm font-medium text-gray-600 group-hover:text-gray-900 transition-colors">Duy trì đăng nhập</span>
                  </label>
                  <button type="button" onClick={() => { setMode('forgot'); setErrorMessage(''); setSuccessMessage(''); }} className="text-sm font-bold text-[#0A101D] hover:text-[#00B4D8] transition-colors">
                    Quên mật khẩu?
                  </button>
                </div>
              )}

              <button type="submit" disabled={isLoading} className="w-full bg-[#0A101D] hover:bg-[#1A2235] disabled:bg-gray-400 disabled:cursor-not-allowed text-white font-bold py-4 rounded-xl transition-all duration-300 hover:shadow-[0_8px_30px_rgb(10,16,29,0.2)] active:scale-[0.98] flex justify-center items-center mt-4">
                {isLoading ? (
                  <span className="flex items-center gap-2">
                    <svg className="animate-spin h-5 w-5 text-white/70" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Đang xử lý...
                  </span>
                ) : (
                  mode === 'login' ? "Đăng nhập hệ thống" 
                  : mode === 'register' ? "Đăng ký tài khoản" 
                  : mode === 'verify-otp' ? "Xác nhận mã OTP" 
                  : "Gửi yêu cầu khôi phục"
                )}
              </button>
            </form>

            <div className="text-center mt-8">
              {mode === 'forgot' || mode === 'verify-otp' ? (
                <button type="button" onClick={() => { setMode('login'); setErrorMessage(''); setSuccessMessage(''); }} className="flex items-center justify-center gap-2 text-sm font-bold text-gray-500 hover:text-[#0A101D] transition-colors mx-auto">
                  <ArrowLeft className="w-4 h-4" /> Quay lại đăng nhập
                </button>
              ) : (
                <div className="bg-gray-100/50 py-3 rounded-xl border border-gray-200/60 inline-block px-6">
                  <span className="text-sm font-medium text-gray-600">
                    {mode === 'login' ? "Chưa có tài khoản? " : "Đã có tài khoản? "}
                  </span>
                  <button type="button" onClick={() => { setMode(mode === 'login' ? 'register' : 'login'); setErrorMessage(''); setSuccessMessage(''); }} className="text-sm font-bold text-[#00B4D8] hover:text-[#0693B0] transition-colors ml-1">
                    {mode === 'login' ? "Đăng ký ngay" : "Đăng nhập tại đây"}
                  </button>
                </div>
              )}
            </div>

            <div className="relative flex items-center py-6">
              <div className="grow border-t border-gray-200"></div>
              <span className="shrink-0 mx-4 text-[11px] font-bold text-gray-400 tracking-widest uppercase">Hoặc tiếp tục với</span>
              <div className="grow border-t border-gray-200"></div>
            </div>

            <div className="w-full flex justify-center mt-2 opacity-90 hover:opacity-100 transition-opacity duration-300">
              <GoogleLogin
                onSuccess={async (credentialResponse) => {
                  try {
                    setIsLoading(true);
                    setErrorMessage('');
                    const response = await authApi.googleLogin(credentialResponse.credential!);
                    const userData = response.data;
                    localStorage.setItem('accessToken', userData.token);
                    localStorage.setItem('userRole', userData.role);
                    localStorage.setItem('fullName', userData.fullName);
                    navigate('/workspaces');
                  } catch (error: any) {
                    if (error.response && error.response.data) {
                      setErrorMessage(error.response.data.message);
                    } else {
                      setErrorMessage("Xác thực với hệ thống Google thất bại.");
                    }
                  } finally {
                    setIsLoading(false);
                  }
                }}
                onError={() => {
                  setErrorMessage("Cửa sổ đăng nhập Google bị đóng hoặc xảy ra lỗi mạng.");
                }}
                useOneTap
                theme="outline"
                size="large"
                width="100%"
                text="continue_with"
                shape="pill" 
              />
            </div>
          </div>

        </div>
      </div>
    </div>
  );
};

export default Login;