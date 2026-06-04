import axios from 'axios';

// Khởi tạo một instance của axios với cấu hình mặc định
const axiosClient = axios.create({
    // Thay đổi cổng này nếu Backend của bạn chạy cổng khác
    baseURL: 'http://localhost:5254', 
    headers: {
        'Content-Type': 'application/json',
    },
});

// Bạn có thể thêm Interceptors ở đây sau này (ví dụ: tự động đính kèm Token khi gửi request)
axiosClient.interceptors.request.use((config) => {
    // const token = localStorage.getItem('token');
    // if (token) {
    //     config.headers.Authorization = `Bearer ${token}`;
    // }
    return config;
});

export default axiosClient;