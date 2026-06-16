const ProjectOverview = () => {
    return (
      <div className="animate-in fade-in duration-500 space-y-6 max-w-7xl mx-auto">
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6">
          {['Tổng Mentions', 'Chỉ số Cảm xúc (NSR)', 'Lượt Tương tác', 'Phạm vi tiếp cận (Reach)'].map((title, i) => (
            <div key={i} className="bg-[#151B2B] border border-white/5 rounded-2xl p-6">
              <p className="text-sm text-gray-400 mb-2">{title}</p>
              <h3 className="text-3xl font-bold text-white">
                {i === 1 ? '45%' : i === 0 ? '3,452' : i === 2 ? '12.4K' : '2.1M'}
              </h3>
            </div>
          ))}
        </div>
        <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 h-96 flex flex-col">
          <h3 className="font-bold mb-4">Biểu đồ Tổng quan</h3>
          <div className="flex-1 border border-dashed border-white/10 rounded-lg flex items-center justify-center text-gray-500">
            Biểu đồ Line Chart Render tại đây
          </div>
        </div>
      </div>
    );
  };
  export default ProjectOverview;