import { CheckCircle2, CreditCard, Download, Zap } from 'lucide-react';

const Subscription = () => {
  return (
    <div className="flex flex-col h-full text-white bg-[#050A15] overflow-y-auto">
      <header className="h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 flex items-center px-8 sticky top-0 z-20 shrink-0">
        <div className="text-gray-300 font-medium tracking-wide">Quản lý Tài chính</div>
      </header>

      <div className="p-8 md:p-10 max-w-5xl mx-auto w-full">
        <h1 className="text-4xl font-bold tracking-tight mb-8">Gói cước (Billing)</h1>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 mb-8">
          
          {/* Thẻ Gói hiện tại */}
          <div className="lg:col-span-2 bg-gradient-to-br from-[#0A101D] to-[#151B2B] border border-[#FF7575]/30 rounded-2xl p-8 relative overflow-hidden">
            <div className="absolute top-0 right-0 p-8 opacity-10"><Zap size={100} /></div>
            <div className="relative z-10">
              <div className="inline-block px-3 py-1 bg-[#FF7575]/20 text-[#FF7575] rounded-full text-xs font-bold uppercase tracking-wider mb-4 border border-[#FF7575]/30">
                Gói hiện tại
              </div>
              <h2 className="text-3xl font-bold text-white mb-2">Professional Plan</h2>
              <p className="text-gray-400 mb-6">Thanh toán tự động 2.500.000 VNĐ vào ngày 15/07/2026.</p>
              
              <div className="flex items-center gap-4">
                <button className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-6 py-3 rounded-lg font-bold transition-colors">
                  Nâng cấp gói (Upgrade)
                </button>
                <button className="bg-transparent hover:bg-white/5 border border-white/20 text-white px-6 py-3 rounded-lg font-bold transition-colors">
                  Hủy gia hạn
                </button>
              </div>
            </div>
          </div>

          {/* Thẻ Thống kê Tài nguyên */}
          <div className="bg-[#0A101D] border border-white/5 rounded-2xl p-8">
            <h3 className="text-lg font-bold mb-6">Mức sử dụng tài nguyên</h3>
            <div className="space-y-6">
              <div>
                <div className="flex justify-between text-sm mb-2"><span className="text-gray-400">Dự án (Projects)</span><span className="font-bold">3 / 5</span></div>
                <div className="w-full bg-[#151B2B] h-2 rounded-full overflow-hidden"><div className="bg-[#00B4D8] w-[60%] h-full"></div></div>
              </div>
              <div>
                <div className="flex justify-between text-sm mb-2"><span className="text-gray-400">Data Mentions</span><span className="font-bold">62.6K / 100K</span></div>
                <div className="w-full bg-[#151B2B] h-2 rounded-full overflow-hidden"><div className="bg-yellow-500 w-[62%] h-full"></div></div>
              </div>
              <div>
                <div className="flex justify-between text-sm mb-2"><span className="text-gray-400">Thành viên nhóm</span><span className="font-bold">4 / 10</span></div>
                <div className="w-full bg-[#151B2B] h-2 rounded-full overflow-hidden"><div className="bg-emerald-500 w-[40%] h-full"></div></div>
              </div>
            </div>
          </div>

        </div>

        {/* Lịch sử thanh toán */}
        <div className="bg-[#0A101D] border border-white/5 rounded-2xl overflow-hidden">
          <div className="p-6 border-b border-white/5 flex items-center gap-2">
            <CreditCard className="text-[#FF7575]" />
            <h3 className="text-lg font-bold">Lịch sử thanh toán</h3>
          </div>
          <table className="w-full text-left">
            <thead>
              <tr className="bg-white/[0.02]">
                <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Mã hóa đơn</th>
                <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Ngày thanh toán</th>
                <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Số tiền</th>
                <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Trạng thái</th>
                <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase text-right">Chứng từ</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/5">
              {[
                { id: 'INV-2026-004', date: '15/06/2026', amount: '2,500,000 ₫' },
                { id: 'INV-2026-003', date: '15/05/2026', amount: '2,500,000 ₫' },
              ].map((invoice, i) => (
                <tr key={i} className="hover:bg-white/[0.02]">
                  <td className="px-6 py-4 font-mono text-sm">{invoice.id}</td>
                  <td className="px-6 py-4 text-gray-300">{invoice.date}</td>
                  <td className="px-6 py-4 font-bold">{invoice.amount}</td>
                  <td className="px-6 py-4">
                    <span className="flex items-center gap-1.5 text-emerald-500 text-sm font-semibold">
                      <CheckCircle2 size={16} /> Thành công
                    </span>
                  </td>
                  <td className="px-6 py-4 text-right">
                    <button className="text-gray-400 hover:text-[#00B4D8] transition-colors" title="Tải PDF">
                      <Download size={20} className="inline" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

      </div>
    </div>
  );
};

export default Subscription;