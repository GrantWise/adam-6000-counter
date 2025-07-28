# Development Setup Guide

## Overview

This setup runs the backend infrastructure (InfluxDB, Grafana, Logger) in Docker, while the WebAPI and Frontend run locally for easy development and debugging.

## Architecture

```
┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│   Frontend (3000)   │────▶│  WebAPI (5000)      │────▶│  InfluxDB (8086)    │
│   Next.js (Local)   │     │  .NET (Local)       │     │  Docker Container   │
└─────────────────────┘     └─────────────────────┘     └─────────────────────┘
         │                            │
         │                            │
         ▼                            ▼
    React Query                 SignalR Hubs
    HTTP/WebSocket              Real-time Updates
```

## Quick Start

### 1. Start Backend Infrastructure

```bash
cd docker
docker-compose up -d
```

This starts:
- **InfluxDB** (port 8086) - Time series database
- **Grafana** (port 3002) - Dashboards  
- **Logger Service** - Reads from ADAM devices
- **Prometheus** (port 9090) - Metrics collection

### 2. Start WebAPI Locally

```bash
cd src/Industrial.Adam.Logger.WebApi
dotnet run
```

The WebAPI will be available at: http://localhost:5000
API Documentation: http://localhost:5000/api-docs

### 3. Start Frontend Locally

```bash
cd adam-counter-frontend
cp .env.example .env.local
npm install  # or pnpm install
npm run dev
```

The frontend will be available at: http://localhost:3000

## Access Points

- **Frontend UI**: http://localhost:3000
- **API Documentation**: http://localhost:5000/api-docs
- **Grafana**: http://localhost:3002 (admin/admin)
- **InfluxDB**: http://localhost:8086 (admin/admin123)
- **Prometheus**: http://localhost:9090

## Development Workflow

### Frontend Development
- Hot reload enabled automatically
- Changes to React components update instantly
- API calls go to local WebAPI at port 5000

### Backend Development
- WebAPI runs with hot reload via `dotnet watch run`
- Changes to C# code trigger automatic restart
- Database and other services remain in Docker

### Full Stack Development
- Frontend calls WebAPI via REST and SignalR
- WebAPI connects to InfluxDB in Docker
- Logger service reads device data and stores in InfluxDB

## Configuration

### Frontend Configuration
Create `.env.local`:
```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

### WebAPI Configuration
Uses `appsettings.Development.json` for local development.

## Troubleshooting

### Frontend Can't Connect to API
1. Check if WebAPI is running: http://localhost:5000/api-docs
2. Verify CORS settings in WebAPI allow localhost:3000
3. Check browser console for errors

### WebAPI Database Connection Issues
1. Ensure Docker services are running: `docker-compose ps`
2. Check InfluxDB health: http://localhost:8086
3. Verify connection string in appsettings.json

### SignalR Connection Issues
1. Check browser developer tools → Network tab
2. Verify WebSocket connections to localhost:5000
3. Check WebAPI logs for SignalR errors

## Stopping Services

```bash
# Stop Docker services
cd docker
docker-compose down

# WebAPI and Frontend - Ctrl+C in their terminals
```

## Production Notes

This development setup is optimized for local development. For production:
- WebAPI should be deployed to cloud/server
- Frontend should be built and deployed to static hosting
- All services can be containerized for production deployment

## Benefits of This Approach

✅ **Fast Development**: Hot reload for both frontend and backend  
✅ **Easy Debugging**: Direct access to logs and debugger  
✅ **Isolated Infrastructure**: Database and services in containers  
✅ **Simple**: No complex Docker builds or networking  
✅ **Flexible**: Can easily switch between local and remote APIs