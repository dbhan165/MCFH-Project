import axios from 'axios';

/**
 * Dev: gọi backend local :5254
 * Production build: cùng origin (nginx proxy /api → backend)
 * Override: VITE_API_BASE_URL (không có trailing slash)
 */
const baseURL =
  (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ||
  (import.meta.env.DEV ? 'http://localhost:5254' : '');

const axiosClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

axiosClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default axiosClient;
