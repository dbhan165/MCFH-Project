# ==========================================================
# BUILD STAGE
# ==========================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy file project trước để tận dụng Docker cache
COPY ["MCFHBackend.csproj", "./"]

# Restore dependencies
RUN dotnet restore "./MCFHBackend.csproj"

# Copy toàn bộ source code
COPY . .

# Publish ứng dụng
RUN dotnet publish "./MCFHBackend.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false


# ==========================================================
# RUNTIME STAGE
# ==========================================================
# Phải khớp Microsoft.Playwright trong MCFHBackend.csproj (hiện 1.60.0).
# Từ 1.60 MCR không còn tag -jammy cho playwright/dotnet; dùng -noble (Ubuntu 24.04).
FROM mcr.microsoft.com/playwright/dotnet:v1.60.0-noble AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=Asia/Ho_Chi_Minh

# Copy ứng dụng đã publish
COPY --from=build /app/publish .

# Tạo các thư mục cần thiết
RUN mkdir -p \
    /app/Logs \
    /app/Reports \
    /app/Uploads \
    /app/Temp \
    /app/Cookies \
    /app/Playwright \
    /app/Export

EXPOSE 8080

ENTRYPOINT ["dotnet", "MCFHBackend.dll"]