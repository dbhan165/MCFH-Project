import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom';

// Import các trang
import Welcome from './pages/Welcome';
import Login from './pages/Login';
import Campaigns from './pages/Campaigns';
import CreateCampaign from './pages/CreateCampaign';
import AdminDashboard from './pages/admin/AdminDashboard';
import UserManagement from './pages/admin/UserManagement';
import SubscriptionPlans from './pages/admin/SubscriptionPlans';
import ProxyManagement from './pages/admin/ProxyManagement';
import ScrapingMonitor from './pages/admin/ScrapingMonitor';
import SystemSettings from './pages/admin/SystemSettings';
import BespokeRequests from './pages/reporter/BespokeRequests';
import ReporterPlaceholder from './pages/reporter/ReporterPlaceholder';
import RequestDetail from './pages/reporter/RequestDetail';
import MyPerformance from './pages/reporter/MyPerformance';
import PipelineConfig from './pages/reporter/PipelineConfig';
import RequestDelivery from './pages/reporter/RequestDelivery';
import AnalystWorkspace from './pages/reporter/AnalystWorkspace';

function AppRoutes() {
  const location = useLocation();
  const normalizedPath = location.pathname.replace(/\/{2,}/g, '/');

  if (normalizedPath !== location.pathname) {
    return <Navigate to={normalizedPath + location.search + location.hash} replace />;
  }

  return (
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

        {/* Admin Portal */}
        <Route path="/admin" element={<Navigate to="/admin/dashboard" replace />} />
        <Route path="/admin/dashboard" element={<AdminDashboard />} />
        <Route path="/admin/users" element={<UserManagement />} />
        <Route path="/admin/subscriptions" element={<SubscriptionPlans />} />
        <Route path="/admin/proxies" element={<ProxyManagement />} />
        <Route path="/admin/scraping" element={<ScrapingMonitor />} />
        <Route path="/admin/settings" element={<SystemSettings />} />

        {/* Reporter / Analyst Workspace */}
        <Route path="/reporter" element={<Navigate to="/reporter/tasks" replace />} />
        <Route path="/reporter/tasks" element={<BespokeRequests />} />
        
        {/* Route chi tiết yêu cầu/báo giá */}
        <Route path="/reporter/requests/:id" element={<RequestDetail />} />

        <Route
          path="/reporter/dashboard"
          element={
            <ReporterPlaceholder
              title="Dashboard"
              description="Tổng quan hiệu suất và đơn yêu cầu của bạn."
              activeTopNav="dashboard"
            />
          }
        />
        
        <Route path="/reporter/performance" element={<MyPerformance />} />
        
        <Route
          path="/reporter/settings"
          element={
            <ReporterPlaceholder
              title="Settings"
              description="Cấu hình tài khoản và tùy chọn làm việc."
              activeTopNav="settings"
            />
          }
        />
        <Route
          path="/reporter/archive"
          element={
            <ReporterPlaceholder
              title="Archive"
              description="Lưu trữ các báo cáo và đơn yêu cầu đã hoàn thành."
              activeTopNav="archive"
            />
          }
        />
        
        {/* 🌟 ĐÃ CHUẨN HÓA CÁC ĐƯỜNG DẪN ROUTE (KHÔNG TRÙNG NHAU) */}
        <Route path="/reporter/pipeline/:id" element={<PipelineConfig />} />
        <Route path="/reporter/delivery/:id" element={<RequestDelivery />} />
        <Route path="/reporter/workspace/:id" element={<AnalystWorkspace />} />     


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
  );
}

function App() {
  return (
    <Router>
      <AppRoutes />
    </Router>
  );
}

export default App;