# ===========================
# Build Stage
# ===========================
FROM node:22-alpine AS builder

WORKDIR /app

# Copy package files trước để tận dụng cache
COPY package*.json ./

# Nếu dùng pnpm hoặc yarn thì thay đổi tương ứng
RUN npm ci

# Copy source
COPY . .

# Build production
RUN npm run build

# ===========================
# Runtime Stage
# ===========================
FROM nginx:1.27-alpine

# Xóa default html
RUN rm -rf /usr/share/nginx/html/*

# Copy build output
COPY --from=builder /app/dist /usr/share/nginx/html

# SPA routing (React Router)
RUN printf '%s\n' \
'server {' \
'    listen 80;' \
'    server_name _;' \
'' \
'    root /usr/share/nginx/html;' \
'    index index.html;' \
'' \
'    location / {' \
'        try_files $uri $uri/ /index.html;' \
'    }' \
'' \
'    location /assets/ {' \
'        expires 1y;' \
'        add_header Cache-Control "public, immutable";' \
'    }' \
'}' \
> /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]