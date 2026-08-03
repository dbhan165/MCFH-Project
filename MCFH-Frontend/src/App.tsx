import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
  useLocation,
} from "react-router-dom";
import PrivateRoute from "./components/auth/PrivateRoute";

// Public
import Welcome from "./pages/Welcome";
import Login from "./pages/Login";
import ResetPassword from "./pages/ResetPassword";
import Pricing from "./pages/Pricing";
import ScrapePackagesPublic from "./pages/ScrapePackagesPublic";

// Admin + Reporter (remote)
import AdminDashboard from "./pages/admin/AdminDashboard";
import UserManagement from "./pages/admin/UserManagement";
import AdminUserDetail from "./pages/admin/AdminUserDetail";
import SubscriptionPlans from "./pages/admin/SubscriptionPlans";
import ProxyManagement from "./pages/admin/ProxyManagement";
import FbSourceManagement from "./pages/admin/FbSourceManagement";
import ScrapePackageManagement from "./pages/admin/ScrapePackageManagement";
import CookieManagement from "./pages/admin/CookieManagement";
import SystemSettings from "./pages/admin/SystemSettings";
import AdminProfile from "./pages/admin/AdminProfile";
import AuditLogs from "./pages/admin/AuditLogs";
import BespokeRequests from "./pages/reporter/BespokeRequests";
import RequestDetail from "./pages/reporter/RequestDetail";
import MyPerformance from "./pages/reporter/MyPerformance";
import ReporterDashboard from "./pages/reporter/ReporterDashboard";
import ReporterArchive from "./pages/reporter/ReporterArchive";
import ReporterProfile from "./pages/reporter/ReporterProfile";
import ReporterSettings from "./pages/reporter/ReporterSettings";

// Workspace + Project (local)
import DashboardLayout from "./layouts/DashboardLayout";
import ProjectLayout from "./layouts/ProjectLayout";
import Workspaces from "./pages/Workspaces";
import WorkspaceSettings from "./pages/WorkspaceSettings";
import Projects from "./pages/Projects";
import CreateWorkspace from "./pages/CreateWorkspace";
import CreateProject from "./pages/CreateProject";
import ScrapeOrderTracking from "./pages/ScrapeOrderTracking";
import PaymentReturn from "./pages/PaymentReturn";
import EditProject from "./pages/EditProject";
import Members from "./pages/Members";
import Invitations from "./pages/Invitations";
import Profile from "./pages/Profile";
import Subscription from "./pages/Subscription";
import SubscriptionUpgrade from "./pages/SubscriptionUpgrade";
import ProjectOverview from "./pages/ProjectOverview";
import ProjectMentions from "./pages/ProjectMentions";
import ProjectSentiment from "./pages/ProjectSentiment";
import ProjectInfluencers from "./pages/ProjectInfluencers";
import ProjectChannel from "./pages/ProjectChannel";
import ProjectAspect from "./pages/ProjectAspect";
import ProjectReports from "./pages/ProjectReports";
import ProjectComparison from "./pages/ProjectComparison";
import { AppModalProvider } from "./contexts/AppModalContext";
import { ScrapeJobProvider } from "./contexts/ScrapeJobContext";
import ErrorBoundary from "./components/ErrorBoundary";
import ProjectBespokeReports from "./pages/ProjectBespokeReports";

function AppRoutes() {
  const location = useLocation();
  const normalizedPath = location.pathname.replace(/\/{2,}/g, "/");

  if (normalizedPath !== location.pathname) {
    return (
      <Navigate to={normalizedPath + location.search + location.hash} replace />
    );
  }

  return (
    <Routes>
      {/* Public */}
      <Route path="/" element={<Welcome />} />
      <Route path="/login" element={<Login />} />
      <Route path="/reset-password" element={<ResetPassword />} />
      <Route path="/pricing" element={<Pricing />} />
      <Route path="/scrape-packages" element={<ScrapePackagesPublic />} />

      {/* Private */}
      <Route element={<PrivateRoute />}>
        <Route element={<PrivateRoute allowedRoles={["Admin"]} />}>
          {/* Admin */}
          <Route
            path="/admin"
            element={<Navigate to="/admin/dashboard" replace />}
          />
          <Route path="/admin/dashboard" element={<AdminDashboard />} />
          <Route path="/admin/users" element={<UserManagement />} />
          <Route path="/admin/users/:userId" element={<AdminUserDetail />} />
          <Route path="/admin/subscriptions" element={<SubscriptionPlans />} />
          <Route path="/admin/proxies" element={<ProxyManagement />} />
          <Route path="/admin/fb-sources" element={<FbSourceManagement />} />
          <Route path="/admin/scrape-packages" element={<ScrapePackageManagement />} />
          <Route path="/admin/cookies" element={<CookieManagement />} />
          <Route path="/admin/settings" element={<SystemSettings />} />
          <Route path="/admin/profile" element={<AdminProfile />} />
          <Route path="/admin/audit-logs" element={<AuditLogs />} />
        </Route>

        <Route element={<PrivateRoute allowedRoles={["Reporter", "Admin"]} />}>
          {/* Reporter */}
          <Route
            path="/reporter"
            element={<Navigate to="/reporter/tasks" replace />}
          />
          <Route path="/reporter/tasks" element={<BespokeRequests />} />
          <Route path="/reporter/requests/:id" element={<RequestDetail />} />
          <Route path="/reporter/dashboard" element={<ReporterDashboard />} />
          <Route path="/reporter/performance" element={<MyPerformance />} />
          <Route path="/reporter/archive" element={<ReporterArchive />} />
          <Route path="/reporter/profile" element={<ReporterProfile />} />
          <Route path="/reporter/settings" element={<ReporterSettings />} />
          {/* Legacy mock routes → Tasks */}
          <Route path="/reporter/pipeline/:id" element={<Navigate to="/reporter/tasks" replace />} />
          <Route path="/reporter/delivery/:id" element={<Navigate to="/reporter/tasks" replace />} />
          <Route path="/reporter/workspace/:id" element={<Navigate to="/reporter/tasks" replace />} />
        </Route>

        {/* Workspace */}
        <Route element={<DashboardLayout />}>
          <Route path="/workspaces" element={<Workspaces />} />
          <Route path="/invitations" element={<Invitations />} />
          <Route
            path="/workspace/:workspaceId/settings"
            element={<WorkspaceSettings />}
          />
          <Route
            path="/workspace/:workspaceId/projects"
            element={<Projects />}
          />
          <Route path="/workspace/:workspaceId/members" element={<Members />} />
          <Route path="/profile" element={<Profile />} />
          <Route path="/subscription" element={<Subscription />} />
          <Route
            path="/subscription/upgrade"
            element={<SubscriptionUpgrade />}
          />
          <Route
            path="/workspace/:workspaceId/project/bespoke-reports"
            element={<ProjectBespokeReports />}
          />
        </Route>

        <Route path="/create-workspace" element={<CreateWorkspace />} />
        <Route path="/create-project" element={<CreateProject />} />
        <Route
          path="/workspace/:workspaceId/orders/:orderId"
          element={<ScrapeOrderTracking />}
        />
        <Route path="/payment/return" element={<PaymentReturn />} />
        <Route
          path="/workspace/:workspaceId/project/:projectId/edit"
          element={<EditProject />}
        />
        <Route
          path="/workspace/:workspaceId/project/:id/comparison"
          element={<ProjectComparison />}
        />

        {/* Project analytics */}
        <Route
          path="/workspace/:workspaceId/project/:id"
          element={<ProjectLayout />}
        >
          <Route index element={<ProjectOverview />} />
          <Route path="mentions" element={<ProjectMentions />} />
          <Route path="sentiment" element={<ProjectSentiment />} />
          <Route path="influencers" element={<ProjectInfluencers />} />
          <Route path="channel" element={<ProjectChannel />} />
          <Route path="aspect" element={<ProjectAspect />} />
          <Route path="reports" element={<ProjectReports />} />
        </Route>
      </Route>

      <Route
        path="/workspace-settings"
        element={<Navigate to="/workspaces" replace />}
      />

      <Route
        path="*"
        element={
          <div className="flex flex-col items-center justify-center min-h-screen bg-[#050A15] text-white">
            <h1 className="text-6xl font-extrabold text-[#00B4D8] mb-4">404</h1>
            <p className="text-gray-400 mb-8">
              Trang bạn yêu cầu không tồn tại.
            </p>
            <a
              href="/"
              className="px-6 py-3 bg-white text-[#0A101D] font-bold rounded-lg hover:bg-gray-200"
            >
              Quay lại Trang chủ
            </a>
          </div>
        }
      />
    </Routes>
  );
}

function App() {
  return (
    <AppModalProvider>
      <ScrapeJobProvider>
        <Router>
          <ErrorBoundary>
            <AppRoutes />
          </ErrorBoundary>
        </Router>
      </ScrapeJobProvider>
    </AppModalProvider>
  );
}

export default App;
