import { ChevronLeft } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

const Comparison = () => {
  const navigate = useNavigate();
  const compareProjectMockData = {
    title: 'Comparison',
    subtitle: 'So sánh hiệu suất social listening giữa hai project',
    selectedRange: {
      label: '01/05 - 30/05/2024',
    },
    leftProject: {
      id: 'tesla',
      name: 'Tesla',
      subtitle: 'Xe điện',
      color: '#FF9AA2',
      nsrScore: 68,
      totalMentions: 12450,
      positiveRate: 65,
      totalEngagement: 18420,
      keyTopic: 'Điện xe và pin',
    },
    rightProject: {
      id: 'fpt',
      name: 'FPT',
      subtitle: 'Đại học FPT',
      color: '#54E1D1',
      nsrScore: 42,
      totalMentions: 15200,
      positiveRate: 30,
      totalEngagement: 13800,
      keyTopic: 'Giáo dục & tuyển dụng',
    },
    platformBreakdown: [
      { platform: 'Facebook', teslaMentions: 5200, fptMentions: 8200, teslaShare: 34, fptShare: 66 },
      { platform: 'TikTok', teslaMentions: 2400, fptMentions: 5400, teslaShare: 31, fptShare: 69 },
      { platform: 'News', teslaMentions: 2800, fptMentions: 4200, teslaShare: 40, fptShare: 60 },
      { platform: 'Maps', teslaMentions: 2050, fptMentions: 1400, teslaShare: 59, fptShare: 41 },
    ],
    insights: [
      {
        type: 'positive',
        text: 'Tesla duy trì chỉ số NSR cao hơn (+26đ) mặc dù tổng thảo luận thấp hơn FPT. Đây là dấu hiệu thảo luận chất lượng và có độ tập trung tích cực.',
      },
      {
        type: 'neutral',
        text: 'FPT hiện có volume cuộc thảo luận lớn hơn 22% và đang dẫn đầu trên TikTok với hơn 90% SOV trong mảng giáo dục.',
      },
      {
        type: 'warning',
        text: 'Spike vào ngày 15/05 của Tesla liên quan đến tin trạm sạc; tuy nhiên hệ thống phản hồi nhanh đã duy trì NSR trên 60 trong 72 giờ sau đó.',
      },
    ],
  };

  const {
    title,
    subtitle,
    selectedRange,
    leftProject,
    rightProject,
    platformBreakdown,
    insights,
  } = compareProjectMockData;

  const gradientBg = 'bg-[radial-gradient(circle_at_top_left,_rgba(255,154,162,0.12),_transparent_25%),radial-gradient(circle_at_top_right,_rgba(84,225,209,0.12),_transparent_20%),linear-gradient(180deg,_#060B18_0%,_#090F1C_55%,_#050A15_100%)]';

  return (
    <div className={`${gradientBg} min-h-screen pb-16 sm:pb-20`}>
      <div className="mx-auto max-w-7xl px-4 pt-8 sm:px-6 lg:px-8">
        <main className="space-y-6">
          <header className="rounded-[32px] border border-white/10 bg-[#0C1424]/80 p-6 shadow-[0_20px_80px_rgba(8,14,26,0.35)] backdrop-blur-xl">
            <div className="flex items-center justify-between mb-4">
              <button
                onClick={() => navigate(-1)}
                className="inline-flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-gray-300 hover:bg-white/10 hover:text-white transition-colors"
              >
                <ChevronLeft size={18} />
                Quay về
              </button>
            </div>
            <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
              <div>
                <div className="text-3xl font-semibold text-white tracking-tight">Comparison</div>
                <div className="mt-2 text-sm text-[#9DA8CC]">So sánh hiệu suất, cảm xúc và thảo luận của hai project khác nhau.</div>
              </div>
              <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
                <div className="relative flex w-full max-w-md items-center rounded-3xl border border-white/10 bg-[#06101F]/90 px-4 py-3 text-sm text-[#B7C1D6] shadow-[inset_0_1px_0_rgba(255,255,255,0.04)]">
                  <span className="mr-3 text-[#7B8BA8]">🔎</span>
                  <input
                    className="w-full bg-transparent text-sm text-white outline-none placeholder:text-[#6E7B95]"
                    placeholder="Tìm kiếm dữ liệu đối sánh..."
                  />
                </div>
                <div className="inline-flex items-center gap-2 rounded-3xl border border-white/10 bg-[#06101F]/90 px-4 py-3 text-sm text-[#B7C1D6] shadow-[inset_0_1px_0_rgba(255,255,255,0.04)]">
                  <span>🕒</span>
                  <span>{selectedRange.label}</span>
                </div>
              </div>
            </div>
          </header>

            <section className="grid gap-6 md:grid-cols-3">
              <div className="rounded-[32px] border border-white/10 bg-[#0F162B]/85 p-6 shadow-[0_24px_60px_rgba(8,14,26,0.35)]">
                <div className="text-xs uppercase tracking-[0.28em] text-[#7A8FB5]">Chỉ số NSR (Net Sentiment Rate)</div>
                <div className="mt-6 flex items-center justify-between gap-6">
                  <div>
                    <div className="text-4xl font-semibold text-[#FF9AA2]">{leftProject.nsrScore}</div>
                    <div className="mt-1 text-xs uppercase text-[#7A8FB5]">Tesla</div>
                  </div>
                  <div>
                    <div className="text-4xl font-semibold text-[#54E1D1]">{rightProject.nsrScore}</div>
                    <div className="mt-1 text-xs uppercase text-[#7A8FB5]">FPT</div>
                  </div>
                </div>
              </div>
              <div className="rounded-[32px] border border-white/10 bg-[#0F162B]/85 p-6 shadow-[0_24px_60px_rgba(8,14,26,0.35)]">
                <div className="text-xs uppercase tracking-[0.28em] text-[#7A8FB5]">Tổng thảo luận (Mentions)</div>
                <div className="mt-6 flex items-center justify-between gap-6">
                  <div>
                    <div className="text-4xl font-semibold text-white">12,450</div>
                    <div className="mt-1 text-xs uppercase text-[#7A8FB5]">Tesla</div>
                  </div>
                  <div>
                    <div className="text-4xl font-semibold text-[#54E1D1]">15,200</div>
                    <div className="mt-1 text-xs uppercase text-[#7A8FB5]">FPT</div>
                  </div>
                </div>
              </div>
              <div className="rounded-[32px] border border-white/10 bg-[#0F162B]/85 p-6 shadow-[0_24px_60px_rgba(8,14,26,0.35)]">
                <div className="text-xs uppercase tracking-[0.28em] text-[#7A8FB5]">Sức mạnh cảm xúc (Positive %)</div>
                <div className="mt-6 space-y-4">
                  <div>
                    <div className="mb-2 flex items-center justify-between text-sm text-[#B7C1D6]"><span>Tesla</span><span>65%</span></div>
                    <div className="h-2.5 rounded-full bg-white/10">
                      <div className="h-full rounded-full bg-[#FF9AA2]" style={{ width: '65%' }} />
                    </div>
                  </div>
                  <div>
                    <div className="mb-2 flex items-center justify-between text-sm text-[#B7C1D6]"><span>FPT</span><span>30%</span></div>
                    <div className="h-2.5 rounded-full bg-white/10">
                      <div className="h-full rounded-full bg-[#54E1D1]" style={{ width: '30%' }} />
                    </div>
                  </div>
                </div>
              </div>
            </section>

            <section className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
              <div className="rounded-[32px] border border-white/10 bg-[#0F162B]/85 p-6 shadow-[0_24px_60px_rgba(8,14,26,0.35)]">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="text-xl font-semibold text-white">Thị phần thảo luận</div>
                    <p className="mt-2 text-sm text-[#9DA8CC]">Phân bổ tỷ lệ % mentions giữa hai đối tượng mục tiêu.</p>
                  </div>
                  <div className="rounded-3xl border border-white/10 bg-white/5 px-4 py-2 text-xs text-[#C7D0E3]">27.6K TOTAL MENTIONS</div>
                </div>
                <div className="mt-8 flex gap-4">
                  <div className="flex items-center gap-3 rounded-[24px] bg-[#081021] px-4 py-3">
                    <span className="h-3 w-3 rounded-full bg-[#FF9AA2]" />
                    <span className="text-sm text-[#D6E1FF]">Tesla (45%)</span>
                  </div>
                  <div className="flex items-center gap-3 rounded-[24px] bg-[#081021] px-4 py-3">
                    <span className="h-3 w-3 rounded-full bg-[#54E1D1]" />
                    <span className="text-sm text-[#D6E1FF]">FPT (55%)</span>
                  </div>
                </div>
                <div className="mt-8 rounded-[32px] border border-white/10 bg-[#06101F]/90 p-8 text-center text-white">
                  <div className="text-5xl font-semibold">27.6K</div>
                  <div className="mt-2 text-sm text-[#9DA8CC]">TOTAL MENTIONS</div>
                </div>
              </div>

              <div className="rounded-[32px] border border-white/10 bg-[#0F162B]/85 p-6 shadow-[0_24px_60px_rgba(8,14,26,0.35)]">
                <div className="text-xl font-semibold text-white">Phân bố kênh truyền thông</div>
                <div className="mt-6 space-y-4">
                  {platformBreakdown.map((item) => (
                    <div key={item.platform} className="space-y-2">
                      <div className="flex items-center justify-between text-sm text-[#B7C1D6]">
                        <span>{item.platform}</span>
                        <span>{item.teslaMentions.toLocaleString()}</span>
                      </div>
                      <div className="h-3 rounded-full bg-white/10 overflow-hidden">
                        <div className="h-full rounded-full bg-[#FF9AA2]" style={{ width: `${Math.min(item.teslaShare, 100)}%` }} />
                      </div>
                      <div className="mt-2 flex items-center justify-between text-sm text-[#B7C1D6]">
                        <span />
                        <span>{item.fptMentions.toLocaleString()}</span>
                      </div>
                      <div className="h-3 rounded-full bg-white/10 overflow-hidden">
                        <div className="h-full rounded-full bg-[#54E1D1]" style={{ width: `${Math.min(item.fptShare, 100)}%` }} />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </section>

            <section className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
              <div className="rounded-[32px] border border-white/10 bg-[#0F162B]/85 p-6 shadow-[0_24px_60px_rgba(8,14,26,0.35)]">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="text-xl font-semibold text-white">Xu hướng thảo luận theo thời gian</div>
                    <p className="mt-2 text-sm text-[#9DA8CC]">Theo dõi hai đường trend của Tesla và FPT trong 30 ngày.</p>
                  </div>
                </div>
                <div className="mt-8 rounded-[32px] border border-white/10 bg-[#06101F]/90 p-6">
                  <div className="h-[260px] rounded-[28px] bg-gradient-to-b from-[#08101F] to-[#0D1728]" />
                </div>
              </div>

              <div className="rounded-[32px] border border-white/10 bg-[#0F162B]/85 p-6 shadow-[0_24px_60px_rgba(8,14,26,0.35)]">
                <div className="flex items-center justify-between">
                  <div className="text-xl font-semibold text-white">Kinetic AI Insights</div>
                  <button className="rounded-3xl bg-[#FF9AA2]/10 px-4 py-2 text-sm font-semibold text-[#FFB4B4] transition hover:bg-[#FF9AA2]/20">Xuất báo cáo PDF</button>
                </div>
                <div className="mt-6 space-y-4">
                  {insights.map((item) => (
                    <div key={item.text} className="rounded-3xl border border-white/10 bg-[#06101F]/90 p-4">
                      <div className={`inline-flex items-center gap-2 rounded-full px-3 py-1 text-xs font-semibold ${
                        item.type === 'positive' ? 'bg-[#2B5843]/80 text-emerald-300' : item.type === 'warning' ? 'bg-[#57401D]/80 text-amber-300' : 'bg-[#1B2740]/80 text-slate-300'
                      }`}>
                        <span>{item.type === 'positive' ? '✔' : item.type === 'warning' ? '⚠' : 'ℹ'}</span>
                        {item.type === 'positive' ? 'Positive' : item.type === 'warning' ? 'Warning' : 'Neutral'}
                      </div>
                      <p className="mt-3 text-sm leading-7 text-[#D4DCE8]">{item.text}</p>
                    </div>
                  ))}
                </div>
              </div>
            </section>
          </main>
      </div>
    </div>
  );
};

export default Comparison;
