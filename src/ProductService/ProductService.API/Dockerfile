FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/ProductService/ProductService.API/ProductService.API.csproj", "src/ProductService/ProductService.API/"]
COPY ["src/ProductService/ProductService.Application/ProductService.Application.csproj", "src/ProductService/ProductService.Application/"]
COPY ["src/ProductService/ProductService.Domain/ProductService.Domain.csproj", "src/ProductService/ProductService.Domain/"]
COPY ["src/ProductService/ProductService.Infrastructure/ProductService.Infrastructure.csproj", "src/ProductService/ProductService.Infrastructure/"]
COPY ["src/Shared/Shared.Domain/Shared.Domain.csproj", "src/Shared/Shared.Domain/"]
COPY ["src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "src/Shared/Shared.Infrastructure/"]

RUN dotnet restore "src/ProductService/ProductService.API/ProductService.API.csproj"

COPY . .
RUN dotnet build "src/ProductService/ProductService.API/ProductService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/ProductService/ProductService.API/ProductService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.API.dll"]
