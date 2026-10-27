# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy các file csproj để tận dụng Docker layer caching khi restore NuGet packages
COPY WorkShiftServer/WorkShiftServer.API/WorkShiftServer.API.csproj WorkShiftServer/WorkShiftServer.API/
COPY WorkShiftServer/WorkShiftManagement.BLL/WorkShiftManagement.BLL.csproj WorkShiftServer/WorkShiftManagement.BLL/
COPY WorkShiftServer/WorkShiftManagement.DAL/WorkShiftManagement.DAL.csproj WorkShiftServer/WorkShiftManagement.DAL/

# Restore dependencies cho Web API
RUN dotnet restore WorkShiftServer/WorkShiftServer.API/WorkShiftServer.API.csproj

# Copy toàn bộ mã nguồn của WorkShiftServer
COPY WorkShiftServer/ WorkShiftServer/

# Build và publish bản Release
RUN dotnet publish WorkShiftServer/WorkShiftServer.API/WorkShiftServer.API.csproj -c Release -o /app/publish --no-restore

# Stage 2: Create runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Cài đặt curl để hỗ trợ health check nếu cần
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Tạo user không phải root để chạy ứng dụng an toàn
RUN useradd -m -u 1000 -s /bin/bash dotnetuser

WORKDIR /app

COPY --from=build /app/publish .

RUN chown -R dotnetuser:dotnetuser /app

USER dotnetuser

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_gcServer=1

# Khởi chạy file dll chính của Backend API
ENTRYPOINT ["dotnet", "WorkShiftServer.API.dll"]
