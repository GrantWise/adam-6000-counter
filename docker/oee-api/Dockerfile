# Multi-stage build for OEE API service
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Use non-root user for security
USER app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for dependency resolution
COPY ["src/Industrial.Adam.Oee/WebApi/Industrial.Adam.Oee.WebApi.csproj", "Industrial.Adam.Oee.WebApi/"]
COPY ["src/Industrial.Adam.Oee/Application/Industrial.Adam.Oee.Application.csproj", "Industrial.Adam.Oee.Application/"]
COPY ["src/Industrial.Adam.Oee/Infrastructure/Industrial.Adam.Oee.Infrastructure.csproj", "Industrial.Adam.Oee.Infrastructure/"]
COPY ["src/Industrial.Adam.Oee/Domain/Industrial.Adam.Oee.Domain.csproj", "Industrial.Adam.Oee.Domain/"]

# Restore dependencies
RUN dotnet restore "Industrial.Adam.Oee.WebApi/Industrial.Adam.Oee.WebApi.csproj"

# Copy source code
COPY src/Industrial.Adam.Oee/ .

# Build the application
WORKDIR "/src/WebApi"
RUN dotnet build "Industrial.Adam.Oee.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Industrial.Adam.Oee.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Create logs directory
USER root
RUN mkdir -p /app/logs && chown app:app /app/logs
USER app

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Industrial.Adam.Oee.WebApi.dll"]