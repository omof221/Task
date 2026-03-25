FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/LogService/LogService.API/LogService.API.csproj", "src/LogService/LogService.API/"]
COPY ["src/LogService/LogService.Application/LogService.Application.csproj", "src/LogService/LogService.Application/"]
COPY ["src/LogService/LogService.Domain/LogService.Domain.csproj", "src/LogService/LogService.Domain/"]
COPY ["src/LogService/LogService.Infrastructure/LogService.Infrastructure.csproj", "src/LogService/LogService.Infrastructure/"]
COPY ["src/Shared/Shared.Domain/Shared.Domain.csproj", "src/Shared/Shared.Domain/"]
COPY ["src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "src/Shared/Shared.Infrastructure/"]

RUN dotnet restore "src/LogService/LogService.API/LogService.API.csproj"

COPY . .
RUN dotnet build "src/LogService/LogService.API/LogService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/LogService/LogService.API/LogService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LogService.API.dll"]
