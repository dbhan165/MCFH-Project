# ===========================
# Build Stage
# ===========================
FROM node:22-alpine AS builder

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

# Production: để trống → browser gọi cùng origin (/api/...), nginx proxy sang backend
ARG VITE_API_BASE_URL=
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

# ===========================
# Runtime Stage
# ===========================
FROM nginx:1.27-alpine

RUN rm -rf /usr/share/nginx/html/*

COPY --from=builder /app/dist /usr/share/nginx/html

# SPA + proxy API/Swagger/Hangfire tới service backend trong docker network
RUN printf '%s\n' \
'server {' \
'    listen 80;' \
'    server_name _;' \
'' \
'    root /usr/share/nginx/html;' \
'    index index.html;' \
'' \
'    location /api/ {' \
'        proxy_pass http://backend:8080/api/;' \
'        proxy_http_version 1.1;' \
'        proxy_set_header Host $host;' \
'        proxy_set_header X-Real-IP $remote_addr;' \
'        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;' \
'        proxy_set_header X-Forwarded-Proto $scheme;' \
'        proxy_set_header Authorization $http_authorization;' \
'        proxy_pass_header Authorization;' \
'    }' \
'' \
'    location /swagger {' \
'        proxy_pass http://backend:8080/swagger;' \
'        proxy_http_version 1.1;' \
'        proxy_set_header Host $host;' \
'        proxy_set_header X-Forwarded-Proto $scheme;' \
'    }' \
'' \
'    location /hangfire {' \
'        proxy_pass http://backend:8080/hangfire;' \
'        proxy_http_version 1.1;' \
'        proxy_set_header Host $host;' \
'        proxy_set_header X-Forwarded-Proto $scheme;' \
'    }' \
'' \
'    location /assets/ {' \
'        expires 1y;' \
'        add_header Cache-Control "public, immutable";' \
'        try_files $uri =404;' \
'    }' \
'' \
'    location / {' \
'        try_files $uri $uri/ /index.html;' \
'    }' \
'}' \
> /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
