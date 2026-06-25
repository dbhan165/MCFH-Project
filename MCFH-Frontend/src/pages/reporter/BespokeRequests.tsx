import { useNavigate } from 'react-router-dom';
import { Calendar, Filter, MoreHorizontal, FileText } from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';

type ColumnStatus = 'pending' | 'in_progress' | 'completed';
type CardAction = 'quote' | 'workspace' | 'pdf';

interface RequestCard {
  id: string;
  date?: string;
  title: string;
  clientInitials: string;
  clientName: string;
  badge?: string;
  action: CardAction;
  actionLabel: string;
}

interface KanbanColumn {
  status: ColumnStatus;
  title: string;
  dotColor: string;
  count: number;
  cards: RequestCard[];
}

const columns: KanbanColumn[] = [
  {
    status: 'pending',
    title: 'Chờ báo giá',
    dotColor: 'bg-sky-400',
    count: 2,
    cards: [
      {
        id: 'REQ-1042',
        date: '01/06/2026',
        title: 'Phân tích Khủng hoảng PetCareHub',
        clientInitials: 'PC',
        clientName: 'PetCareHub',
        action: 'quote',
        actionLabel: 'Xem & Báo giá',
      },
      {
        id: 'REQ-1040',
        date: '28/05/2026',
        title: 'Theo dõi sentiment thương hiệu Q2',
        clientInitials: 'BL',
        clientName: 'BlueLine Media',
        action: 'quote',
        actionLabel: 'Xem & Báo giá',
      },
       {
        id: 'REQ-1040',
        date: '28/05/2026',
        title: 'Theo dõi tập đoàn FPT',
        clientInitials: 'BL',
        clientName: 'FPT Corporation',
        action: 'quote',
        actionLabel: 'Xem & Báo giá',
      },
    ],
  },
];

const ActionButton = ({
  action,
  label,
  cardId,
}: {
  action: CardAction;
  label: string;
  cardId: string;
}) => {
  const navigate = useNavigate();

  if (action === 'quote') {
    return (
      <button 
        type="button"
        onClick={() => navigate(`/reporter/requests/${cardId}`)}
        className="w-full py-2.5 border-2 border-teal-600 text-teal-700 hover:bg-teal-50 rounded-lg text-sm font-semibold transition-colors cursor-pointer"
      >
        {label}
      </button>
    );
  }

  if (action === 'workspace') {
    return (
      <button 
        type="button"
        onClick={() => navigate(`/reporter/pipeline/${cardId}`)}
        className="w-full py-2.5 bg-teal-600 hover:bg-teal-700 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm cursor-pointer"
      >
        {label}
      </button>
    );
  }

  return (
    <button className="w-full flex items-center justify-center gap-2 py-2.5 border border-gray-300 text-[#64748b] hover:bg-gray-50 rounded-lg text-sm font-medium transition-colors">
      <FileText className="w-4 h-4" />
      {label}
    </button>
  );
};

const BespokeRequests = () => {
  return (
    <ReporterLayout activeTopNav="reports">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <h2 className="text-xl lg:text-2xl font-bold text-[#0f172a]">
          Quản lý Đơn Yêu cầu{' '}
          <span className="text-base font-normal text-[#64748b]">(Bespoke Requests)</span>
        </h2>
        <button className="flex items-center gap-2 px-4 py-2.5 bg-white border border-gray-200 rounded-lg text-sm font-medium text-[#0f172a] hover:bg-gray-50 transition-colors shrink-0 self-start sm:self-auto">
          <Filter className="w-4 h-4 text-gray-500" />
          Filter
        </button>
      </div>

      {/* Cột cha giữ nguyên kích thước rộng */}
      <div className="w-full">
        {columns.map((column) => (
          <div key={column.status} className="flex flex-col min-h-[400px]">
            <div className="flex items-center justify-between mb-4 px-1">
              <div className="flex items-center gap-2.5">
                <span className={`w-2.5 h-2.5 rounded-full ${column.dotColor}`} />
                <h3 className="text-sm font-semibold text-[#0f172a]">{column.title}</h3>
                <span className="text-xs font-semibold text-[#64748b] bg-gray-100 px-2 py-0.5 rounded-full">
                  {column.count}
                </span>
              </div>
              <button className="p-1.5 text-gray-400 hover:text-[#0f172a] hover:bg-gray-100 rounded-lg transition-colors">
                <MoreHorizontal className="w-4 h-4" />
              </button>
            </div>

            {/* ĐÃ CHỈNH SỬA: Biến danh sách thẻ con thành Grid 2 cột nằm ngang hàng nhau */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 items-start flex-1">
              {column.cards.map((card) => (
                <div
                  key={card.id}
                  className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm hover:shadow-md transition-shadow"
                >
                  <div className="flex items-start justify-between gap-2 mb-3">
                    <span className="text-xs font-semibold text-[#64748b]">#{card.id}</span>
                    <div className="flex items-center gap-2">
                      {card.badge && (
                        <span className={`text-[10px] font-bold uppercase tracking-wider px-2 py-1 rounded ${
                          card.badge === 'ĐÃ THANH TOÁN' ? 'bg-blue-50 text-blue-600' : 'bg-emerald-50 text-emerald-600'
                        }`}>
                          {card.badge}
                        </span>
                      )}
                      {card.date && (
                        <span className="flex items-center gap-1 text-xs text-[#64748b]">
                          <Calendar className="w-3 h-3" />
                          {card.date}
                        </span>
                      )}
                    </div>
                  </div>

                  <h4 className="text-base font-bold text-[#0f172a] mb-4 leading-snug">{card.title}</h4>

                  <div className="flex items-center gap-2.5 mb-5">
                    <span className="w-8 h-8 bg-sky-100 text-sky-700 rounded-md flex items-center justify-center text-xs font-bold shrink-0">
                      {card.clientInitials}
                    </span>
                    <span className="text-sm text-[#64748b]">{card.clientName}</span>
                  </div>

                  <ActionButton action={card.action} label={card.actionLabel} cardId={card.id} />
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </ReporterLayout>
  );
};

// ĐẢM BẢO DÒNG NÀY LÀ EXPORT DEFAULT
export default BespokeRequests;