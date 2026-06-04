import axiosClient from './axiosClient';

export const authApi = {
    // ==========================================
    // 1. NHÓM ĐĂNG NHẬP & ĐĂNG KÝ
    // ==========================================
    
    // 1. Hàm Đăng nhập bằng Email/Mật khẩu
    login: (email, password) => {
        return axiosClient.post('/api/auth/login', { email, password });
    },

    // 2. Hàm Đăng ký tài khoản mới
    register: (fullName, email, phone, password) => {
        return axiosClient.post('/api/auth/register', { 
            fullName, 
            email, 
            phone, 
            password 
        });
    },

    // 3. Hàm Đăng nhập bằng Google SSO (Truyền ID Token lấy từ Google SDK)
    googleLogin: (idToken) => {
        return axiosClient.post('/api/auth/google-login', { idToken });
    },


    // ==========================================
    // 2. NHÓM XÁC THỰC EMAIL (OTP)
    // ==========================================
    
    // 4. Hàm Xác thực Email (Backend hỗ trợ truyền 1 trong 2: otpCode hoặc verificationToken)
    verifyEmail: (email, otpCode = null, verificationToken = null) => {
        return axiosClient.post('/api/auth/verify-email', { 
            email, 
            otpCode,
            verificationToken 
        });
    },

    // 5. Hàm Gửi lại mã OTP (Khi mã cũ hết hạn sau 15 phút)
    resendOtp: (email) => {
        return axiosClient.post('/api/auth/resend-otp', { email });
    },


    // ==========================================
    // 3. NHÓM QUẢN LÝ MẬT KHẨU
    // ==========================================
    
    // 6. Hàm Yêu cầu Khôi phục mật khẩu (Gửi email chứa link Reset)
    forgotPassword: (email) => {
        return axiosClient.post('/api/auth/forgot-password', { email });
    },

    // 7. Hàm Đặt lại mật khẩu mới (Dùng ở trang Reset Password sau khi bấm link trong email)
    resetPassword: (token, newPassword, confirmPassword) => {
        return axiosClient.post('/api/auth/reset-password', { 
            token, 
            newPassword, 
            confirmPassword 
        });
    },

    // 8. Hàm Đổi mật khẩu (Dùng trong trang Profile khi user ĐÃ ĐĂNG NHẬP)
    // Lưu ý: API này yêu cầu Header có chứa JWT Token (Authorize)
    changePassword: (currentPassword, newPassword, confirmPassword) => {
        return axiosClient.post('/api/auth/change-password', { 
            currentPassword, 
            newPassword, 
            confirmPassword 
        });
    }
};