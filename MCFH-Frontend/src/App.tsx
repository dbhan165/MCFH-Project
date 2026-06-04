import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';

// Import các trang
import Welcome from './pages/Welcome';
import Login from './pages/Login';
import Campaigns from './pages/Campaigns';
import CreateCampaign from './pages/CreateCampaign';  

// TODO: Import các trang đang phát triển sau này
// import Dashboard from './pages/Dashboard';
// import BespokeRequest from './pages/BespokeRequest';

function App() {
  return (
    <Router>
      <Routes>
        {/* ==========================================
            1. KHU VỰC PUBLIC (Ai cũng vào được)
            ========================================== */}
        {/* Trang chủ (Landing Page) */}
        <Route path="/" element={<Welcome />} />
        
        {/* Trang Đăng nhập */}
        <Route path="/login" element={<Login />} />
        

        {/* ==========================================
            2. KHU VỰC PRIVATE (Không gian làm việc MCFH)
            ========================================== */}
        <Route path="/campaigns" element={<Campaigns />} />
        <Route path="/create-campaign" element={<CreateCampaign />} />
        
        {/* Các màn hình đang phát triển (Mở comment khi nào làm xong) */}
        {/* <Route path="/dashboard" element={<Dashboard />} /> */}
        {/* <Route path="/bespoke-request" element={<BespokeRequest />} /> */}


        {/* ==========================================
            3. XỬ LÝ LỖI (Bắt mọi đường dẫn sai)
            ========================================== */}
        <Route path="*" element={
          <div className="flex flex-col items-center justify-center min-h-screen bg-[#050A15] text-white">
            <h1 className="text-6xl font-extrabold text-[#00B4D8] mb-4">404</h1>
            <h2 className="text-2xl font-bold mb-2">Không tìm thấy trang</h2>
            <p className="text-gray-400 mb-8">Đường dẫn bạn nhập không tồn tại trên hệ thống.</p>
            <a 
              href="/" 
              className="px-6 py-3 bg-white text-[#0A101D] font-bold rounded-lg hover:bg-gray-200 transition-colors"
            >
              Quay lại Trang chủ
            </a>
          </div>
        } />
      </Routes>
    </Router>
  );
}

export default App;