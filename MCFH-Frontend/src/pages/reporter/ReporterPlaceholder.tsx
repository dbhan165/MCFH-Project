import ReporterLayout from '../../components/reporter/ReporterLayout';

interface ReporterPlaceholderProps {
  title: string;
  description: string;
  activeTopNav?: 'dashboard' | 'reports' | 'archive' | 'settings' | 'performance';
}

const ReporterPlaceholder = ({
  title,
  description,
  activeTopNav = 'dashboard',
}: ReporterPlaceholderProps) => (
  <ReporterLayout activeTopNav={activeTopNav}>
    <div className="flex flex-col items-center justify-center py-24 text-center">
      <h2 className="text-2xl font-bold text-[#0f172a] mb-2">{title}</h2>
      <p className="text-sm text-[#64748b] max-w-md">{description}</p>
      <p className="text-xs text-gray-400 mt-4">Đang phát triển...</p>
    </div>
  </ReporterLayout>
);

export default ReporterPlaceholder;
