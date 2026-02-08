# Progress Path - Multi-stage Dockerfile
# This Dockerfile supports both development (with hot-reload) and production builds.
#
# Stages:
#   - development: SDK image with hot-reload support (used by docker-compose)
#   - build: SDK image for production build
#   - runtime: Slim ASP.NET runtime for production deployment
#
# Usage:
#   Development: docker compose up --build (targets 'development' stage)
#   Production:  docker build -t progresspath . (builds through to 'runtime' stage)

# =============================================================================
# Stage 1: Development (Hot-reload with dotnet watch)
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /src

# Install Node.js for Tailwind CSS building
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Configure ASP.NET Core for development
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:5000
# Enable polling file watcher (required for Docker volume mounts)
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
# Auto-restart on changes that require full restart
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true

# Expose development port
EXPOSE 5000

# Source code is mounted via docker-compose volume
# dotnet watch command is specified in docker-compose.yml

# =============================================================================
# Stage 2: Build (Production)
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install Node.js for Tailwind CSS build
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy project file and restore dependencies
COPY ProgressPath/ProgressPath.csproj ./
RUN dotnet restore

# Copy package.json and install npm dependencies for Tailwind
COPY ProgressPath/package.json ./
RUN npm install

# Copy all source files
COPY ProgressPath/ ./

# Build Tailwind CSS
RUN npm run build:css

# Publish the application in Release configuration
RUN dotnet publish -c Release -o /app/publish --no-restore

# =============================================================================
# Stage 3: Runtime (Production)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Configure ASP.NET Core to listen on port 80
ENV ASPNETCORE_URLS=http://+:80

# Expose port 80 for the application
EXPOSE 80

# Set the entrypoint to run the application
ENTRYPOINT ["dotnet", "ProgressPath.dll"]
