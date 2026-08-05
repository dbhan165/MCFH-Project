import { Component, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  error: Error | null;
}

class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: { componentStack?: string | null }) {
    // eslint-disable-next-line no-console
    console.error('[ErrorBoundary]', error, info);
  }

  render() {
    if (this.state.error) {
      return (
        <div className="min-h-screen bg-[#050A15] text-white p-8">
          <div className="max-w-3xl mx-auto bg-[#0A101D] border border-red-500/30 rounded-xl p-6">
            <h1 className="text-2xl font-bold text-red-300 mb-3">Đã có lỗi xảy ra</h1>
            <p className="text-sm text-gray-400 mb-4">
              Trang không thể render. Vui lòng báo lại cho team kỹ thuật kèm nội dung bên dưới.
            </p>
            <pre className="bg-black/40 p-4 rounded text-xs text-red-200 overflow-auto whitespace-pre-wrap">
              {String(this.state.error?.stack ?? this.state.error)}
            </pre>
            <button
              onClick={() => window.location.reload()}
              className="mt-4 px-4 py-2 bg-[#FF7575] rounded text-sm font-semibold"
            >
              Tải lại trang
            </button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}

export default ErrorBoundary;