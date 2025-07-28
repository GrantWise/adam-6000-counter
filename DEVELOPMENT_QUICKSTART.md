# Development Quick Start

## Simple 3-Step Setup

### 1. Start Backend Services
```bash
cd docker
docker-compose up -d
```

### 2. Start WebAPI
```bash
cd src/Industrial.Adam.Logger.WebApi
dotnet run
```

### 3. Start Frontend
```bash
cd adam-counter-frontend
cp .env.example .env.local
npm install --legacy-peer-deps
npm run dev
```

## Access Your Application

- **Frontend**: http://localhost:3001 (or 3000 if available)
- **API Docs**: http://localhost:5000/api-docs

## Infrastructure Services (Already Running)

- **Grafana**: http://localhost:3002 (admin/admin)
- **InfluxDB**: http://localhost:8086 (admin/admin123)
- **Prometheus**: http://localhost:9090

## That's It!

The frontend will automatically connect to the WebAPI and display real device data. No complex Docker builds, no networking issues, just simple local development.

For detailed information, see [docs/DEVELOPMENT_SETUP.md](docs/DEVELOPMENT_SETUP.md)