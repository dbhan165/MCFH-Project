import { useState, useEffect } from 'react';
// import axios from 'axios'; // Mở comment dòng này khi BE C# đã sẵn sàng

interface PricingPlan {
  id: number;
  name: string;
  description: string;
  price: string;
  period: string;
  features: string[];
  buttonText: string;
  isPopular: boolean;
}

const Pricing = () => {
  const [plans, setPlans] = useState<PricingPlan[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchPricingPlans = async () => {
      try {
        setIsLoading(true);
        await new Promise(resolve => setTimeout(resolve, 1000)); 
        
        const mockData: PricingPlan[] = [
          {
            id: 1,
            name: 'Khởi động',
            description: 'Phù hợp cho cá nhân hoặc doanh nghiệp nhỏ mới làm quen với Social Listening.',
            price: 'Miễn phí',
            period: '',
            features: [
              '1 Workspace (Không gian làm việc)',
              'Thu thập tối đa 1.000 Mentions/tháng',
              'Dashboard thống kê cơ bản',
              'Lọc từ khóa tiêu chuẩn'
            ],
            buttonText: 'Bắt đầu miễn phí',
            isPopular: false,
          },
          {
            id: 2,
            name: 'Chuyên nghiệp (VIP)',
            description: 'Dành cho các chiến dịch Marketing cần phân tích dữ liệu chuyên sâu bằng AI.',
            price: '1.999.000đ',
            period: '/tháng',
            features: [
              'Lên đến 5 Workspaces',
              'Thu thập 50.000 Mentions/tháng',
              'Mở khóa AI Phân tích Cảm xúc (Sentiment)',
              'Công cụ Theo dõi Influencers (KOLs/KOCs)',
              'Xuất báo cáo PDF/Excel tự động'
            ],
            buttonText: 'Nâng cấp VIP ngay',
            isPopular: true,
          },
          {
            id: 3,
            name: 'Giải pháp Đặc thù',
            description: 'Hệ thống may đo và dịch vụ phân tích dữ liệu thủ công từ chuyên gia (Bespoke).',
            price: 'Liên hệ',
            period: '',
            features: [
              'Không giới hạn Workspaces',
              'Dung lượng Mentions theo nhu cầu',
              'Đặt hàng Báo cáo Chuyên sâu (Bespoke Report)',
              'Chuyên viên hỗ trợ riêng (1-kèm-1)',
              'Cam kết SLA & Hỗ trợ kỹ thuật 24/7'
            ],
            buttonText: 'Nhận báo giá',
            isPopular: false,
          }
        ];

        setPlans(mockData);
      } catch (error) {
        console.error("Lỗi khi tải bảng giá:", error);
      } finally {
        setIsLoading(false); 
      }
    };

    fetchPricingPlans();
  }, []);

  // Giao diện Loading đồng bộ Dark Theme
  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#0A101D] flex justify-center items-center selection:bg-[#FF7575] selection:text-white">
        <div className="flex flex-col items-center relative z-10">
          <div className="animate-spin rounded-full h-12 w-12 border-b-4 border-[#FF7575] mb-4"></div>
          <p className="text-[#9BA1B0] font-medium tracking-wide">Đang tải bảng giá cập nhật...</p>
        </div>
      </div>
    );
  }

  return (
    // Wrapper chính với màu nền Navy tối và pattern lưới chìm
    <div className="min-h-screen bg-[#0A101D] text-white font-sans py-16 relative overflow-hidden selection:bg-[#FF7575] selection:text-white flex items-center">
      
      {/* Background Grid Pattern (Giống hệt trang Welcome) */}
      <div 
        className="absolute inset-0 z-0 opacity-20 pointer-events-none"
        style={{ 
          backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', 
          backgroundSize: '32px 32px' 
        }}
      ></div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10 w-full">
        
        {/* Header Section */}
        <div className="text-center max-w-3xl mx-auto mb-16">
          <h2 className="text-sm font-bold text-[#FF7575] tracking-widest uppercase mb-3 flex items-center justify-center gap-2">
            <span className="w-1.5 h-1.5 rounded-full bg-[#FF7575]"></span>
            Bảng giá dịch vụ
            <span className="w-1.5 h-1.5 rounded-full bg-[#FF7575]"></span>
          </h2>
          <p className="mt-2 text-4xl font-extrabold text-white sm:text-5xl tracking-tight">
            Chọn gói giải pháp phù hợp
          </p>
          <p className="mt-4 text-lg text-[#9BA1B0]">
            Nâng cấp hệ thống lắng nghe mạng xã hội của bạn ngay hôm nay. Hủy bất cứ lúc nào.
          </p>
        </div>

        {/* Pricing Cards Grid */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {plans.map((plan) => (
            <div 
              key={plan.id} 
              className={`relative rounded-2xl flex flex-col p-8 transition-all duration-300 hover:-translate-y-2 
                bg-linear-to-br from-[#1A2235]/80 to-[#0A101D]/80 backdrop-blur-md
                ${
                plan.isPopular 
                  ? 'border border-[#FF7575]/60 shadow-[0_0_30px_rgba(255,117,117,0.15)] ring-1 ring-[#FF7575]/30' 
                  : 'border border-white/10 hover:border-white/30 hover:shadow-2xl hover:shadow-white/5'
              }`}
            >
              {/* Badge nổi bật cho gói VIP */}
              {plan.isPopular && (
                <div className="absolute top-0 right-6 transform -translate-y-1/2">
                  <span className="bg-[#FF7575] text-white text-xs font-bold uppercase tracking-wider py-1.5 px-4 rounded-full shadow-lg shadow-[#FF7575]/30">
                    Khuyên dùng
                  </span>
                </div>
              )}

              {/* Tên gói & Mô tả */}
              <div className="mb-6">
                <h3 className="text-2xl font-bold text-white">{plan.name}</h3>
                <p className="mt-3 text-[#9BA1B0] min-h-12 text-sm leading-relaxed">{plan.description}</p>
              </div>

              {/* Giá tiền */}
              <div className="mb-6 pb-6 border-b border-white/10">
                <span className="text-4xl font-extrabold text-white">{plan.price}</span>
                <span className="text-lg font-medium text-[#9BA1B0] ml-1">{plan.period}</span>
              </div>

              {/* Danh sách tính năng */}
              <ul className="mb-8 space-y-4 flex-1">
                {plan.features.map((feature, index) => (
                  <li key={index} className="flex items-start">
                    {/* Icon Checkmark đổi sang màu Hồng Đỏ của theme */}
                    <svg className="shrink-0 h-5 w-5 text-[#FF7575] mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M5 13l4 4L19 7" />
                    </svg>
                    <span className="ml-3 text-gray-300 text-sm">{feature}</span>
                  </li>
                ))}
              </ul>

              {/* Nút Action */}
              <button 
                className={`mt-auto w-full py-3.5 px-4 rounded-lg text-sm font-bold tracking-wide text-center transition-all duration-200 ${
                  plan.isPopular 
                    ? 'bg-[#FF7575] text-white hover:bg-[#ff6262] shadow-[0_4px_14px_0_rgba(255,117,117,0.39)]' 
                    : 'bg-white/5 text-white hover:bg-white/10 border border-white/10'
                }`}
              >
                {plan.buttonText}
              </button>
            </div>
          ))}
        </div>

      </div>
    </div>
  );
};

export default Pricing;