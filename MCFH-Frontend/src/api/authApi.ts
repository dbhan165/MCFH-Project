import axiosClient from './axiosClient';

export const authApi = {
    // 1. Hàm Đăng nhập
    login: (email: string, password: string) => {
        // Khớp với [HttpPost("login")] và LoginDto
        return axiosClient.post('/api/auth/login', { email, password });
    },

    // 2. Hàm Đăng ký
    register: (fullName: string, email: string, phone: string, password: string) => {
        // Khớp với [HttpPost("register")] và RegisterDto
        return axiosClient.post('/api/auth/register', { 
            fullName, 
            email, 
            phone, 
            password 
        });
    },

    // 3. Hàm Xác thực Email (Nhập OTP)
    verifyEmail: (email: string, otpCode: string) => {
        // Khớp với [HttpPost("verify-email")] và VerifyEmailDto
        return axiosClient.post('/api/auth/verify-email', { 
            email, 
            otpCode 
        });
    }
};