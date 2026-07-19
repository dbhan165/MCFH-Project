import { Navigate, Outlet } from 'react-router-dom';
import { getAccessToken, getUserRole } from '../../utils/authStorage';

type PrivateRouteProps = {
  allowedRoles?: string[];
};

function normalizeRole(role: string | null) {
  return role?.trim().toLowerCase() ?? '';
}

function resolveRoleHomePath(role: string) {
  switch (normalizeRole(role)) {
    case 'admin':
      return '/admin/dashboard';
    case 'reporter':
      return '/reporter/tasks';
    default:
      return '/workspaces';
  }
}

const PrivateRoute = ({ allowedRoles }: PrivateRouteProps) => {
  const token = getAccessToken();
  const currentRole = getUserRole();

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && allowedRoles.length > 0) {
    const normalizedCurrentRole = normalizeRole(currentRole);
    const normalizedAllowedRoles = allowedRoles.map(normalizeRole);

    if (!normalizedAllowedRoles.includes(normalizedCurrentRole)) {
      return <Navigate to={resolveRoleHomePath(currentRole ?? '')} replace />;
    }
  }

  return <Outlet />;
};

export default PrivateRoute;
