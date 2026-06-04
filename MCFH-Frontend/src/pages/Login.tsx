import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Mail, Lock, CheckCircle2, AlertCircle, User, Phone, ArrowLeft } from 'lucide-react';
import { GoogleLogin } from '@react-oauth/google'; // Import thư viện Google
import { authApi } from '../api/authApi';
import loginImage from '../assets/login.png';

type AuthMode = 'login' | 'register' | 'forgot';

const Login = () => {
  const [mode, setMode] = useState<AuthMode>('login');

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState(''); 
  
  const navigate = useNavigate();

  // ==========================================
  // XỬ LÝ SUBMIT (LOGIN / REGISTER / FORGOT)
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
        
        navigate('/create-campaign'); 

      } else if (mode === 'register') {
        const response = await authApi.register(fullName, email, phone, password);
        alert(response.data.message || "Đăng ký thành công! Vui lòng kiểm tra email để lấy mã xác thực.");
        setMode('login');
        setPassword(''); 

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

  return (
    <div className="min-h-screen flex w-full font-sans selection:bg-[#0A101D] selection:text-white">
      
      {/* CỘT TRÁI: Dark Mode */}
      <div className="hidden lg:flex lg:w-5/12 bg-[#050A15] relative flex-col justify-between p-12 border-r border-white/5 overflow-hidden">
        <div 
          className="absolute inset-0 z-0 opacity-10 pointer-events-none"
          style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '24px 24px' }}
        ></div>
        <div className="relative z-10 grow flex items-center justify-center mb-12">
          <div className="w-full max-w-md aspect-3/4 bg-linear-to-b from-[#151B2B] to-[#0A101D] border border-white/10 rounded-2xl p-4 shadow-2xl flex items-center justify-center overflow-hidden">
            <img src={loginImage} alt="AI Funnel" className="w-full h-full object-cover" />
          </div>
        </div>
        <div className="relative z-10 bg-[#0A101D]/80 backdrop-blur-md border border-white/10 p-8 rounded-2xl">
          <h3 className="text-white text-2xl font-bold mb-6 tracking-tight">MCFH System Update v2.0</h3>
          <ul className="space-y-4">
            <li className="flex items-center gap-3 text-gray-300 text-sm"><CheckCircle2 className="text-[#00B4D8] w-5 h-5" /> Tích hợp SignalR Real-time</li>
            <li className="flex items-center gap-3 text-gray-300 text-sm"><CheckCircle2 className="text-[#00B4D8] w-5 h-5" /> AI Aspect Extraction</li>
            <li className="flex items-center gap-3 text-gray-300 text-sm"><CheckCircle2 className="text-[#00B4D8] w-5 h-5" /> VNPay Checkout</li>
          </ul>
        </div>
      </div>

      {/* CỘT PHẢI: Light Mode - Form */}
      <div className="w-full lg:w-7/12 bg-white flex items-center justify-center p-8 sm:p-12 overflow-y-auto">
        <div className="w-full max-w-md space-y-8 my-auto">
          
          <div>
            <h1 className="text-[#0A101D] text-4xl font-extrabold tracking-tight mb-4">MCFH</h1>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">
              {mode === 'login' && "Đăng nhập Không gian làm việc"}
              {mode === 'register' && "Tạo tài khoản mới"}
              {mode === 'forgot' && "Khôi phục mật khẩu"}
            </h2>
            <p className="text-gray-500 text-sm">
              {mode === 'login' && "Vui lòng điền thông tin để tiếp tục."}
              {mode === 'register' && "Điền thông tin bên dưới để trải nghiệm hệ thống."}
              {mode === 'forgot' && "Nhập email của bạn, chúng tôi sẽ gửi hướng dẫn khôi phục."}
            </p>
          </div>

          {errorMessage && (
            <div className="bg-red-50 text-red-600 p-4 rounded-lg flex items-center gap-3 text-sm font-medium border border-red-100 animate-pulse">
              <AlertCircle className="w-5 h-5 shrink-0" />
              {errorMessage}
            </div>
          )}

          {successMessage && (
            <div className="bg-green-50 text-green-700 p-4 rounded-lg flex items-center gap-3 text-sm font-medium border border-green-100 animate-pulse">
              <CheckCircle2 className="w-5 h-5 shrink-0" />
              {successMessage}
            </div>
          )}

          <form className="space-y-5" onSubmit={handleSubmit}>
            
            {mode === 'register' && (
              <>
                <div className="space-y-2 animate-fade-in-down">
                  <label className="text-xs font-bold text-gray-700 tracking-wider uppercase">Họ và Tên</label>
                  <div className="relative">
                    <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><User className="h-5 w-5 text-gray-400" /></div>
                    <input type="text" value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Nguyễn Văn A" className="w-full pl-11 pr-4 py-3.5 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#00B4D8] transition-all" required disabled={isLoading} />
                  </div>
                </div>
                <div className="space-y-2 animate-fade-in-down">
                  <label className="text-xs font-bold text-gray-700 tracking-wider uppercase">Số điện thoại</label>
                  <div className="relative">
                    <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Phone className="h-5 w-5 text-gray-400" /></div>
                    <input type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="0987654321" className="w-full pl-11 pr-4 py-3.5 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#00B4D8] transition-all" required disabled={isLoading} />
                  </div>
                </div>
              </>
            )}

            <div className="space-y-2">
              <label className="text-xs font-bold text-gray-700 tracking-wider uppercase">Email Doanh Nghiệp</label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Mail className="h-5 w-5 text-gray-400" /></div>
                <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="name@company.com" className="w-full pl-11 pr-4 py-3.5 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#00B4D8] transition-all" required disabled={isLoading} />
              </div>
            </div>

            {mode !== 'forgot' && (
              <div className="space-y-2 animate-fade-in-down">
                <label className="text-xs font-bold text-gray-700 tracking-wider uppercase">Mật Khẩu</label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Lock className="h-5 w-5 text-gray-400" /></div>
                  <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" className="w-full pl-11 pr-4 py-3.5 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#00B4D8] transition-all" required disabled={isLoading} />
                </div>
              </div>
            )}

            {mode === 'login' && (
              <div className="flex items-center justify-between">
                <label className="flex items-center gap-2 cursor-pointer group">
                  <input type="checkbox" className="w-4 h-4 rounded border-gray-300 text-[#0A101D] focus:ring-[#0A101D] cursor-pointer" />
                  <span className="text-sm text-gray-700 group-hover:text-black transition-colors">Duy trì đăng nhập</span>
                </label>
                <button type="button" onClick={() => { setMode('forgot'); setErrorMessage(''); setSuccessMessage(''); }} className="text-sm font-bold text-[#0A101D] hover:text-[#00B4D8] transition-colors">
                  Quên mật khẩu?
                </button>
              </div>
            )}

            <button type="submit" disabled={isLoading} className="w-full bg-[#0A101D] hover:bg-[#151B2B] disabled:bg-gray-400 disabled:cursor-not-allowed text-white font-bold py-4 rounded-lg transition-colors shadow-lg shadow-gray-900/20 flex justify-center items-center mt-2">
              {isLoading ? (
                <span className="flex items-center gap-2">
                  <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  ĐANG XỬ LÝ...
                </span>
              ) : (
                mode === 'login' ? "ĐĂNG NHẬP HỆ THỐNG" : mode === 'register' ? "ĐĂNG KÝ TÀI KHOẢN" : "GỬI YÊU CẦU KHÔI PHỤC"
              )}
            </button>
          </form>

          <div className="text-center mt-6">
            {mode === 'forgot' ? (
              <button type="button" onClick={() => { setMode('login'); setErrorMessage(''); setSuccessMessage(''); }} className="flex items-center justify-center gap-2 text-sm font-bold text-gray-600 hover:text-[#0A101D] transition-colors mx-auto">
                <ArrowLeft className="w-4 h-4" /> Quay lại Đăng nhập
              </button>
            ) : (
              <>
                <span className="text-sm text-gray-600">
                  {mode === 'login' ? "Chưa có tài khoản? " : "Đã có tài khoản? "}
                </span>
                <button type="button" onClick={() => { setMode(mode === 'login' ? 'register' : 'login'); setErrorMessage(''); setSuccessMessage(''); }} className="text-sm font-bold text-[#00B4D8] hover:text-[#0A101D] transition-colors">
                  {mode === 'login' ? "Đăng ký ngay" : "Đăng nhập tại đây"}
                </button>
              </>
            )}
          </div>

          <div className="relative flex items-center py-4">
            <div className="grow border-t border-gray-200"></div>
            <span className="shrink-0 mx-4 text-xs font-semibold text-gray-400 tracking-widest uppercase">Hoặc tiếp tục với</span>
            <div className="grow border-t border-gray-200"></div>
          </div>

          {/* ==========================================
              NÚT ĐĂNG NHẬP GOOGLE CHÍNH THỨC
              ========================================== */}
          <div className="w-full flex justify-center mt-2">
            <GoogleLogin
              onSuccess={async (credentialResponse) => {
                try {
                  setIsLoading(true);
                  setErrorMessage('');
                  
                  // credentialResponse.credential chính là chuỗi idToken từ Google
                  const response = await authApi.googleLogin(credentialResponse.credential!);
                  const userData = response.data;
                  
                  // Lưu token và thông tin người dùng
                  localStorage.setItem('accessToken', userData.token);
                  localStorage.setItem('userRole', userData.role);
                  localStorage.setItem('fullName', userData.fullName);
                  
                  // Chuyển vào trang bên trong
                  navigate('/create-campaign');
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
            />
          </div>

        </div>
      </div>
    </div>
  );
};

export default Login;