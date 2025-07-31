# ADAM-6051 C# Logger Dockerfile
# Multi-stage build for optimized production image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Industrial.Adam.Logger.sln ./
# Copy only active project files (V1 projects are archived)
COPY src/Industrial.Adam.Logger.IntegrationTests/Industrial.Adam.Logger.IntegrationTests.csproj ./src/Industrial.Adam.Logger.IntegrationTests/
COPY src/Industrial.Adam.Logger.WebApi/Industrial.Adam.Logger.WebApi.csproj ./src/Industrial.Adam.Logger.WebApi/
COPY src/Industrial.Adam.Logger.Core/Industrial.Adam.Logger.Core.csproj ./src/Industrial.Adam.Logger.Core/
COPY src/Industrial.Adam.Logger.Core.Tests/Industrial.Adam.Logger.Core.Tests.csproj ./src/Industrial.Adam.Logger.Core.Tests/
COPY src/Industrial.Adam.Logger.Console/Industrial.Adam.Logger.Console.csproj ./src/Industrial.Adam.Logger.Console/
COPY src/Industrial.Adam.Logger.Simulator/Industrial.Adam.Logger.Simulator.csproj ./src/Industrial.Adam.Logger.Simulator/

# Restore dependencies for the solution
RUN dotnet restore Industrial.Adam.Logger.sln

# Copy source code
COPY src/ ./src/

# Build and publish the application
RUN dotnet publish ./src/Industrial.Adam.Logger.Console/Industrial.Adam.Logger.Console.csproj \
    -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish ./

# Copy entrypoint script
COPY docker/csharp/entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

# Create directories for configuration and logs
RUN mkdir -p /app/config /app/logs

# Copy configuration files (V2 format)
COPY docker/config/adam_config_v2.json /app/config/
COPY docker/config/adam_config_production_v2.json /app/config/

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production

# Health check - verify the dotnet process is running
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD pgrep -f dotnet || exit 1

# Run the application through entrypoint script
ENTRYPOINT ["./entrypoint.sh"]