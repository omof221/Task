# Multi-stage build — 12-Factor: Build ve run aşamaları kesin olarak ayrılır
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Önce sadece .csproj dosyalarını kopyala (layer cache optimizasyonu)
COPY ["src/AuthService/AuthService.API/AuthService.API.csproj", "src/AuthService/AuthService.API/"]
COPY ["src/AuthService/AuthService.Application/AuthService.Application.csproj", "src/AuthService/AuthService.Application/"]
COPY ["src/AuthService/AuthService.Domain/AuthService.Domain.csproj", "src/AuthService/AuthService.Domain/"]
COPY ["src/AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "src/AuthService/AuthService.Infrastructure/"]
COPY ["src/Shared/Shared.Domain/Shared.Domain.csproj", "src/Shared/Shared.Domain/"]
COPY ["src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "src/Shared/Shared.Infrastructure/"]

RUN dotnet restore "src/AuthService/AuthService.API/AuthService.API.csproj"

COPY . .
RUN dotnet build "src/AuthService/AuthService.API/AuthService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/AuthService/AuthService.API/AuthService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.API.dll"]
