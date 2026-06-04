import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Mail, Lock, CheckCircle2, AlertCircle, User, Phone, ArrowLeft } from 'lucide-react'; // Thêm icon ArrowLeft
import { authApi } from '../api/authApi';
import loginImage from '../assets/login.png';

// Định nghĩa 3 trạng thái màn hình
type AuthMode = 'login' | 'register' | 'forgot';

const Login = () => {
  // 1. State quản lý chế độ hiển thị form
  const [mode, setMode] = useState<AuthMode>('login');

  // 2. State dữ liệu
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState(''); // Để hiển thị báo thành công khi quên pass
  
  const navigate = useNavigate();

  // 3. Hàm xử lý Submit chung cho cả 3 form
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
        alert(response.data.message || "Đăng ký thành công! Vui lòng kiểm tra email.");
        setMode('login');
        setPassword(''); 

      } else if (mode === 'forgot') {
        // LUỒNG QUÊN MẬT KHẨU (Tạm thời mock up, chờ BE có API thì gọi vào đây)
        /* const response = await authApi.forgotPassword(email);
        setSuccessMessage("Hệ thống đã gửi link khôi phục. Vui lòng kiểm tra hộp thư!");
        */
        
        // Code chạy tạm khi chưa có Backend
        setTimeout(() => {
          setSuccessMessage(`Đã gửi hướng dẫn khôi phục mật khẩu tới email: ${email}`);
          setIsLoading(false);
        }, 1500);
        return; // Dừng lại ở đây vì đây là code giả lập
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
          
          {/* Header động thay đổi theo Mode */}
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

          {/* Cảnh báo lỗi */}
          {errorMessage && (
            <div className="bg-red-50 text-red-600 p-4 rounded-lg flex items-center gap-3 text-sm font-medium border border-red-100 animate-pulse">
              <AlertCircle className="w-5 h-5 shrink-0" />
              {errorMessage}
            </div>
          )}

          {/* Cảnh báo thành công (Dành riêng cho Quên mật khẩu) */}
          {successMessage && (
            <div className="bg-green-50 text-green-700 p-4 rounded-lg flex items-center gap-3 text-sm font-medium border border-green-100 animate-pulse">
              <CheckCircle2 className="w-5 h-5 shrink-0" />
              {successMessage}
            </div>
          )}

          <form className="space-y-5" onSubmit={handleSubmit}>
            
            {/* TRƯỜNG ĐĂNG KÝ: Họ tên & SĐT */}
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

            {/* TRƯỜNG CHUNG: Email (Luôn xuất hiện ở cả 3 chế độ) */}
            <div className="space-y-2">
              <label className="text-xs font-bold text-gray-700 tracking-wider uppercase">Email Doanh Nghiệp</label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Mail className="h-5 w-5 text-gray-400" /></div>
                <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="name@company.com" className="w-full pl-11 pr-4 py-3.5 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#00B4D8] transition-all" required disabled={isLoading} />
              </div>
            </div>

            {/* TRƯỜNG MẬT KHẨU: Ẩn đi khi ở chế độ 'forgot' */}
            {mode !== 'forgot' && (
              <div className="space-y-2 animate-fade-in-down">
                <label className="text-xs font-bold text-gray-700 tracking-wider uppercase">Mật Khẩu</label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Lock className="h-5 w-5 text-gray-400" /></div>
                  <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" className="w-full pl-11 pr-4 py-3.5 bg-[#0A101D] border border-transparent text-white placeholder-gray-500 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#00B4D8] transition-all" required disabled={isLoading} />
                </div>
              </div>
            )}

            {/* Các nút bấm phụ dưới form đăng nhập */}
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

            {/* Nút Submit chính */}
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

          {/* Nút Chuyển Mode (Điều hướng phụ) */}
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

          <button type="button" className="w-full bg-white border border-gray-200 hover:bg-gray-50 text-gray-700 font-semibold py-3.5 rounded-lg flex items-center justify-center gap-3 transition-colors shadow-sm">
            <svg className="w-5 h-5" viewBox="0 0 24 24">
              <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
              <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
              <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
              <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
            </svg>
            Google SSO
          </button>
        </div>
      </div>
    </div>
  );
};

export default Login;