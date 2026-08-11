import React, { useState, useEffect } from 'react';
import { X, UploadCloud, Loader2 } from 'lucide-react';

interface UploadRevisionModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSubmit: (file: File | null) => Promise<void>;
}

export const UploadRevisionModal = ({ isOpen, onClose, onSubmit }: UploadRevisionModalProps) => {
    const [file, setFile] = useState<File | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        if (isOpen) setFile(null);
    }, [isOpen]);

    if (!isOpen) return null;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSubmitting(true);
        await onSubmit(file);
        setIsSubmitting(false);
        onClose();
    };

    return (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
            <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose} />
            <div className="relative bg-[#151B2B] border border-white/10 rounded-2xl w-full max-w-lg shadow-2xl flex flex-col animate-in zoom-in-95 duration-200">
                <button type="button" onClick={onClose} className="absolute top-4 right-4 p-1.5 text-gray-400 hover:text-white hover:bg-white/10 rounded-lg transition-colors z-10">
                    <X size={20} />
                </button>
                <div className="p-6 border-b border-white/5 shrink-0">
                    <h2 className="text-xl font-bold text-white flex items-center gap-2">
                        <UploadCloud className="w-5 h-5 text-blue-400" />
                        Upload Bản sửa đổi
                    </h2>
                </div>
                <form onSubmit={handleSubmit} className="p-6 space-y-4">
                    <div>
                        <label className="block text-sm font-bold text-gray-300 mb-2">Chọn file báo cáo (PDF, PPTX) *</label>
                        <input
                            type="file"
                            required
                            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                            className="w-full text-white file:mr-4 file:py-2 file:px-4 file:rounded-xl file:border-0 file:text-sm file:font-semibold file:bg-blue-600/20 file:text-blue-300 hover:file:bg-blue-600/30"
                        />
                    </div>
                    <div className="flex justify-end gap-3 mt-6">
                        <button type="button" onClick={onClose} className="px-4 py-2 rounded-xl font-bold text-gray-400 hover:text-white hover:bg-white/5 transition-colors">
                            Hủy
                        </button>
                        <button type="submit" disabled={isSubmitting || !file} className="flex items-center gap-2 bg-blue-600 hover:bg-blue-500 disabled:opacity-50 text-white px-6 py-2 rounded-xl font-bold transition-colors">
                            {isSubmitting ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Tải lên'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

interface SendToReporterModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSubmit: (note: string) => Promise<void>;
    /** Số lần sắp gửi (1..3). */
    roundNumber?: number;
    maxRounds?: number;
}

export const SendToReporterModal = ({
    isOpen,
    onClose,
    onSubmit,
    roundNumber = 1,
    maxRounds = 3,
}: SendToReporterModalProps) => {
    const [note, setNote] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        if (isOpen) setNote('');
    }, [isOpen]);

    if (!isOpen) return null;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        const trimmed = note.trim();
        if (!trimmed) return;
        setIsSubmitting(true);
        try {
            await onSubmit(trimmed);
            onClose();
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
            <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose} />
            <div className="relative bg-[#151B2B] border border-white/10 rounded-2xl w-full max-w-lg shadow-2xl flex flex-col animate-in zoom-in-95 duration-200">
                <button type="button" onClick={onClose} className="absolute top-4 right-4 p-1.5 text-gray-400 hover:text-white hover:bg-white/10 rounded-lg transition-colors z-10">
                    <X size={20} />
                </button>
                <div className="p-6 border-b border-white/5 shrink-0">
                    <h2 className="text-xl font-bold text-white">
                        Gửi lần {roundNumber}/{maxRounds}
                    </h2>
                    <p className="text-sm text-gray-400 mt-1">
                        Mô tả phần cần sửa để Reporter biết chỉnh đúng chỗ.
                    </p>
                </div>
                <form onSubmit={handleSubmit} className="p-6 space-y-4">
                    <div>
                        <label className="block text-sm font-bold text-gray-300 mb-2">
                            Nội dung gửi Reporter *
                        </label>
                        <textarea
                            required
                            rows={5}
                            value={note}
                            onChange={(e) => setNote(e.target.value)}
                            placeholder="VD: Phần sentiment chưa đúng với iPhone 8, bỏ các bài về iPhone 13, bổ sung kênh TikTok..."
                            className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl text-sm text-white placeholder-gray-500 focus:outline-none focus:border-purple-500 resize-y min-h-[120px]"
                        />
                    </div>
                    <div className="flex justify-end gap-3 mt-2">
                        <button type="button" onClick={onClose} className="px-4 py-2 rounded-xl font-bold text-gray-400 hover:text-white hover:bg-white/5 transition-colors">
                            Hủy
                        </button>
                        <button
                            type="submit"
                            disabled={isSubmitting || !note.trim()}
                            className="flex items-center gap-2 bg-purple-600 hover:bg-purple-500 disabled:opacity-50 text-white px-6 py-2 rounded-xl font-bold transition-colors"
                        >
                            {isSubmitting ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Gửi Reporter'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};
