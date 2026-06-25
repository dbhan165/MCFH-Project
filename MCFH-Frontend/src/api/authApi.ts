import axiosClient from './axiosClient';
import type { AuthResponse, UserProfile } from '../utils/authStorage';

export const authApi = {
    login: (email: string, password: string) => {
        return axiosClient.post<AuthResponse>('/api/auth/login', { email, password });
    },

    register: (fullName: string, email: string, phone: string, password: string) => {
        return axiosClient.post('/api/auth/register', { fullName, email, phone, password });
    },

    googleLogin: (idToken: string) => {
        return axiosClient.post<AuthResponse>('/api/auth/google-login', { idToken });
    },

    verifyEmail: (email: string, otpCode: string | null = null, verificationToken: string | null = null) => {
        return axiosClient.post('/api/auth/verify-email', { email, otpCode, verificationToken });
    },

    resendOtp: (email: string) => {
        return axiosClient.post('/api/auth/resend-otp', { email });
    },

    forgotPassword: (email: string) => {
        return axiosClient.post('/api/auth/forgot-password', { email });
    },

    resetPassword: (token: string, newPassword: string, confirmPassword: string) => {
        return axiosClient.post('/api/auth/reset-password', { token, newPassword, confirmPassword });
    },

    changePassword: (currentPassword: string, newPassword: string, confirmPassword: string) => {
        return axiosClient.post('/api/auth/change-password', { currentPassword, newPassword, confirmPassword });
    },

    getProfile: () => {
        return axiosClient.get<UserProfile>('/api/auth/profile');
    },

    updateProfile: (fullName: string, phone: string, avatarUrl?: string) => {
        return axiosClient.put<UserProfile>('/api/auth/profile', { fullName, phone, avatarUrl });
    },
};
