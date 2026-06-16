import { useState } from 'react';
import { User, Mail, Phone, Lock, Upload, Save } from 'lucide-react';

const Profile = () => {
  const [fullName, setFullName] = useState('Nguyễn Văn A');
  const [phone, setPhone] = useState('0987654321');

  return (
    <div className="flex flex-col h-full text-white bg-[#050A15] overflow-y-auto">
      <header className="h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 flex items-center px-8 sticky top-0 z-20 shrink-0">
        <div className="text-gray-300 font-medium tracking-wide">Quản lý Hồ sơ Cài đặt</div>
      </header>

      <div className="p-8 md:p-10 max-w-4xl mx-auto w-full">
        <h1 className="text-4xl font-bold tracking-tight mb-8">Hồ sơ cá nhân</h1>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {/* Cột Trái: Avatar */}
          <div className="col-span-1 bg-[#0A101D] border border-white/5 rounded-2xl p-8 flex flex-col items-center justify-center text-center">
            <div className="w-32 h-32 rounded-full border-4 border-[#151B2B] bg-gray-700 mb-4 overflow-hidden relative group">
              <img src="https://i.pravatar.cc/150?img=11" alt="Avatar" className="w-full h-full object-cover" />
              <div className="absolute inset-0 bg-black/50 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer">
                <Upload className="w-6 h-6 text-white" />
              </div>
            </div>
            <h3 className="font-bold text-lg mb-1">{fullName}</h3>
            <p className="text-sm text-gray-500 mb-6">admin@acma.vn</p>
            <button className="text-sm font-semibold text-[#FF7575] bg-[#FF7575]/10 px-4 py-2 rounded-lg hover:bg-[#FF7575] hover:text-white transition-colors w-full">
              Thay đổi Ảnh đại diện
            </button>
          </div>

          {/* Cột Phải: Form thông tin */}
          <div className="col-span-1 md:col-span-2 space-y-6">
            
            {/* Form Thông tin cơ bản */}
            <div className="bg-[#0A101D] border border-white/5 rounded-2xl p-8">
              <h3 className="text-xl font-bold mb-6 flex items-center gap-2"><User className="text-[#FF7575]"/> Thông tin liên hệ</h3>
              <div className="space-y-5">
                <div className="space-y-2">
                  <label className="text-xs font-bold text-gray-400 uppercase">Họ và Tên</label>
                  <input type="text" value={fullName} onChange={(e) => setFullName(e.target.value)} className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none" />
                </div>
                <div className="space-y-2">
                  <label className="text-xs font-bold text-gray-400 uppercase">Số điện thoại</label>
                  <input type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none" />
                </div>
                <div className="space-y-2">
                  <label className="text-xs font-bold text-gray-400 uppercase">Email (Không thể thay đổi)</label>
                  <div className="w-full bg-[#151B2B]/50 border border-white/5 rounded-lg px-4 py-3 text-gray-500 cursor-not-allowed">
                    admin@acma.vn
                  </div>
                </div>
              </div>
              <button className="mt-6 bg-[#FF7575] hover:bg-[#ff6262] text-white px-6 py-3 rounded-lg font-bold flex items-center gap-2 transition-colors">
                <Save size={18} /> Lưu thông tin
              </button>
            </div>

            {/* Form Đổi mật khẩu */}
            <div className="bg-[#0A101D] border border-white/5 rounded-2xl p-8">
              <h3 className="text-xl font-bold mb-6 flex items-center gap-2"><Lock className="text-[#FF7575]"/> Đổi mật khẩu</h3>
              <div className="space-y-5">
                <input type="password" placeholder="Mật khẩu hiện tại" className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none" />
                <input type="password" placeholder="Mật khẩu mới" className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none" />
                <input type="password" placeholder="Xác nhận mật khẩu mới" className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:border-[#FF7575] focus:outline-none" />
              </div>
              <button className="mt-6 bg-[#151B2B] hover:bg-white/10 border border-white/10 text-white px-6 py-3 rounded-lg font-bold transition-colors">
                Cập nhật mật khẩu
              </button>
            </div>

          </div>
        </div>
      </div>
    </div>
  );
};

export default Profile;