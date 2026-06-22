import { useState } from 'react';
import { Filter, ShieldCheck, MoreVertical, ChevronLeft, ChevronRight } from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';

type UserRole = 'ADMIN' | 'USER' | 'EDITOR';
type UserStatus = 'Active' | 'Banned';

interface UserRecord {
  id: number;
  name: string;
  email: string;
  avatar: string;
  role: UserRole;
  status: UserStatus;
  lastLogin: string;
}

const users: UserRecord[] = [
  {
    id: 1,
    name: 'Alex Rivera',
    email: 'alex.rivera@agency.com',
    avatar: 'https://i.pravatar.cc/150?img=11',
    role: 'ADMIN',
    status: 'Active',
    lastLogin: '2023-10-24 14:32',
  },
  {
    id: 2,
    name: 'Sarah Jenkins',
    email: 'sarah.j@tech.io',
    avatar: 'https://i.pravatar.cc/150?img=5',
    role: 'USER',
    status: 'Active',
    lastLogin: '2023-10-24 09:15',
  },
  {
    id: 3,
    name: 'John Doe',
    email: 'john.d@disposable.net',
    avatar: 'https://i.pravatar.cc/150?img=12',
    role: 'EDITOR',
    status: 'Banned',
    lastLogin: '2023-09-12 18:20',
  },
];

const roleStyles: Record<UserRole, string> = {
  ADMIN: 'bg-slate-800 text-white',
  USER: 'bg-blue-50 text-blue-600',
  EDITOR: 'bg-blue-50 text-blue-600',
};

const statusDotStyles: Record<UserStatus, string> = {
  Active: 'bg-green-500',
  Banned: 'bg-gray-400',
};

const totalUsers = 1240;
const totalPages = 42;

const UserManagement = () => {
  const [currentPage, setCurrentPage] = useState(1);

  return (
    <AdminLayout searchPlaceholder="Search by email or name...">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">User Management</h2>
          <p className="text-[#6b7280] text-sm mt-1">
            Manage accounts, permissions, and monitor system activities.
          </p>
        </div>

        <div className="flex items-center gap-3 shrink-0 flex-wrap">
          <button className="flex items-center gap-2 px-4 py-2.5 bg-white border border-gray-200 rounded-lg text-sm font-medium text-[#111827] hover:bg-gray-50 transition-colors">
            <Filter className="w-4 h-4 text-gray-500" />
            Role
          </button>
          <button className="flex items-center gap-2 px-4 py-2.5 bg-white border border-gray-200 rounded-lg text-sm font-medium text-[#111827] hover:bg-gray-50 transition-colors">
            <ShieldCheck className="w-4 h-4 text-gray-500" />
            Status
          </button>
          <button className="px-5 py-2.5 bg-[#ef4444] hover:bg-red-600 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm">
            Add New User
          </button>
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50/50">
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  User
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Email
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Role
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Status
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Last Login
                </th>
                <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {users.map((user) => (
                <tr key={user.id} className="hover:bg-gray-50/50 transition-colors">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-3">
                      <img
                        src={user.avatar}
                        alt={user.name}
                        className="w-9 h-9 rounded-full object-cover border border-gray-200"
                      />
                      <span className="font-medium text-[#111827]">{user.name}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 text-[#6b7280]">{user.email}</td>
                  <td className="px-6 py-4">
                    <span
                      className={`inline-block text-[10px] font-bold uppercase tracking-wider px-2.5 py-1 rounded ${roleStyles[user.role]}`}
                    >
                      {user.role}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <span className="inline-flex items-center gap-2 text-sm text-[#111827]">
                      <span className={`w-2 h-2 rounded-full ${statusDotStyles[user.status]}`} />
                      {user.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-[#6b7280]">{user.lastLogin}</td>
                  <td className="px-6 py-4 text-right">
                    <button className="p-2 text-gray-400 hover:text-[#111827] hover:bg-gray-100 rounded-lg transition-colors">
                      <MoreVertical className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="flex flex-col sm:flex-row items-center justify-between gap-4 px-6 py-4 border-t border-gray-100">
          <p className="text-sm text-[#6b7280]">
            Showing <span className="font-medium text-[#111827]">1</span> to{' '}
            <span className="font-medium text-[#111827]">3</span> of{' '}
            <span className="font-medium text-[#111827]">{totalUsers.toLocaleString()}</span> users
          </p>

          <div className="flex items-center gap-1">
            <button
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              className="w-9 h-9 flex items-center justify-center rounded-lg border border-gray-200 text-gray-500 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>

            {[1, 2, 3].map((page) => (
              <button
                key={page}
                onClick={() => setCurrentPage(page)}
                className={`w-9 h-9 flex items-center justify-center rounded-lg text-sm font-medium transition-colors ${
                  currentPage === page
                    ? 'bg-[#ef4444] text-white'
                    : 'border border-gray-200 text-[#111827] hover:bg-gray-50'
                }`}
              >
                {page}
              </button>
            ))}

            <span className="px-2 text-gray-400 text-sm">...</span>

            <button
              onClick={() => setCurrentPage(totalPages)}
              className={`w-9 h-9 flex items-center justify-center rounded-lg text-sm font-medium border border-gray-200 text-[#111827] hover:bg-gray-50 transition-colors ${
                currentPage === totalPages ? 'bg-[#ef4444] text-white border-[#ef4444]' : ''
              }`}
            >
              {totalPages}
            </button>

            <button
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
              className="w-9 h-9 flex items-center justify-center rounded-lg border border-gray-200 text-gray-500 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
};

export default UserManagement;
