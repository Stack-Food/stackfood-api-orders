# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["StackFood.Orders.sln", "./"]
COPY ["src/StackFood.Orders.Domain/StackFood.Orders.Domain.csproj", "src/StackFood.Orders.Domain/"]
COPY ["src/StackFood.Orders.Application/StackFood.Orders.Application.csproj", "src/StackFood.Orders.Application/"]
COPY ["src/StackFood.Orders.Infrastructure/StackFood.Orders.Infrastructure.csproj", "src/StackFood.Orders.Infrastructure/"]
COPY ["src/StackFood.Orders.API/StackFood.Orders.API.csproj", "src/StackFood.Orders.API/"]

# Restore dependencies
RUN dotnet restore "StackFood.Orders.sln"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/src/StackFood.Orders.API"
RUN dotnet build "StackFood.Orders.API.csproj" -c Release -o /app/build
RUN dotnet publish "StackFood.Orders.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8081

# Copy published files
COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8081/health || exit 1

ENTRYPOINT ["dotnet", "StackFood.Orders.API.dll"]
