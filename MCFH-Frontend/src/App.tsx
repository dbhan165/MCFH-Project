import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';

// Import Layouts quản lý khung giao diện
import DashboardLayout from './layouts/DashboardLayout';
import ProjectLayout from './layouts/ProjectLayout';

// Import nhóm các trang hiển thị cộng đồng (Public Pages)
import Welcome from './pages/Welcome';
import Login from './pages/Login';
import Pricing from './pages/Pricing';

// Import nhóm các trang quản lý tổng quan Cấp độ 1 (Level 1 Pages)
import Workspaces from './pages/Workspaces';
import WorkspaceSettings from './pages/WorkspaceSettings';
import Projects from './pages/Projects';
import CreateWorkspace from './pages/CreateWorkspace';
import CreateProject from './pages/CreateProject';
import Members from './pages/Members';
import Profile from './pages/Profile';
import Subscription from './pages/Subscription';

// Import nhóm các trang phân tích chuyên sâu chi tiết Cấp độ 2 (Level 2 Pages)
import ProjectOverview from './pages/ProjectOverview';
import ProjectMentions from './pages/ProjectMentions';
import ProjectSentiment from './pages/ProjectSentiment';
import ProjectInfluencers from './pages/ProjectInfluencers';
import ProjectChannel from './pages/ProjectChannel';
import ProjectAspect from './pages/ProjectAspect';
import ProjectReports from './pages/ProjectReports';
import ProjectCreateBespoke from './pages/ProjectCreateBespoke';

function App() {
  return (
    <Router>
      <Routes>
        {/* ========================================================
            1. KHU VỰC PUBLIC (Tự do truy cập công cộng)
            ======================================================== */}
        <Route path="/" element={<Welcome />} />
        <Route path="/pricing" element={<Pricing />} />
        <Route path="/login" element={<Login />} />
        
        {/* ========================================================
            2. KHU VỰC PRIVATE LEVEL 1 (Sử dụng hệ thanh điều hướng Cấp 1)
            ======================================================== */}
        <Route element={<DashboardLayout />}>
          <Route path="/workspaces" element={<Workspaces />} />
          <Route path="/workspace-settings" element={<WorkspaceSettings />} />
          <Route path="/workspace/:workspaceId/projects" element={<Projects />} />
          
          {/* ĐÃ FIX: Đồng bộ đường dẫn Members để nhận ID của Workspace */}
          <Route path="/workspace/:workspaceId/members" element={<Members />} />
          
          <Route path="/profile" element={<Profile />} />
          <Route path="/subscription" element={<Subscription />} />
        </Route>

        {/* ========================================================
            3. KHU VỰC TÁCH BIỆT DIỆN TÍCH RỘNG (Không sidebar hệ thống)
            ======================================================== */}
        <Route path="/create-workspace" element={<CreateWorkspace />} />
        <Route path="/create-project" element={<CreateProject />} />
        
        {/* ========================================================
            4. KHU VỰC PRIVATE LEVEL 2 (Sử dụng hệ thanh điều hướng riêng của dự án)
            ======================================================== */}
        {/* Nhúng cả định danh Workspace vào trong tiến trình giám sát từng Project */}
        <Route path="/workspace/:workspaceId/project/:id" element={<ProjectLayout />}>
          <Route index element={<ProjectOverview />} />
          <Route path="mentions" element={<ProjectMentions />} />
          <Route path="sentiment" element={<ProjectSentiment />} />
          <Route path="influencers" element={<ProjectInfluencers />} />
          <Route path="channel" element={<ProjectChannel />} />
          <Route path="aspect" element={<ProjectAspect />} />
          <Route path="reports" element={<ProjectReports />} />
          <Route path="create-bespoke" element={<ProjectCreateBespoke />} />
        </Route>

        {/* ========================================================
            5. ĐIỀU HƯỚNG AN TOÀN KHI NHẬP SAI ĐƯỜNG DẪN (404 Not Found)
            ======================================================== */}
        <Route path="*" element={
          <div className="flex flex-col items-center justify-center min-h-screen bg-[#050A15] text-white">
            <h1 className="text-6xl font-extrabold text-[#FF7575] mb-4">404</h1>
            <p className="text-gray-400 mb-8">Trang bạn yêu cầu hiện không tồn tại trên máy chủ MCFH.</p>
            <a href="/" className="px-6 py-3 bg-white text-[#0A101D] font-bold rounded-lg transition-colors hover:bg-gray-200">
              Quay lại Trang chủ
            </a>
          </div>
        } />
      </Routes>
    </Router>
  );
}

export default App;