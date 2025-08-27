# Industrial ADAM-6000 Counter Platform

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![.NET Version](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-blue)

## 🏭 Enterprise Industrial Data Acquisition System

Production-ready platform for ADAM-6051 digital counter modules with real-time monitoring, OEE calculation, and comprehensive data analytics. Built with Clean Architecture, CQRS, and Domain-Driven Design principles.

## 🚀 Quick Start

```bash
# Clone the repository
git clone https://github.com/your-org/adam-6000-counter.git
cd adam-6000-counter

# Start all services with Docker
docker-compose up -d

# Access the platform
open http://localhost:3001  # Frontend Dashboard
open http://localhost:5139  # Logger API
open http://localhost:5001  # OEE API
```

For detailed setup: **[Quick Start Guide](./docs/01-getting-started/quickstart.md)**

## 📚 Documentation

All documentation is organized in the `/docs` folder:

- **[Getting Started](./docs/01-getting-started/)** - Installation, setup, troubleshooting
- **[Architecture](./docs/02-architecture/)** - System design and patterns
- **[Development](./docs/03-development/)** - Coding standards, API reference
- **[Deployment](./docs/04-deployment/)** - Production deployment, configuration
- **[Modules](./docs/05-modules/)** - Service-specific documentation
- **[Hardware](./docs/06-hardware/)** - ADAM device integration

**[📖 View Complete Documentation Index](./docs/)**

## ✨ Key Features

- **Real-Time Data Acquisition** - Sub-second polling with microsecond precision
- **OEE Monitoring** - Complete Overall Equipment Effectiveness tracking
- **Modern Web Dashboard** - React 18 interface with live updates
- **TimescaleDB Storage** - Optimized time-series data management
- **21 CFR Part 11 Compliant** - Full data integrity and audit trails
- **Docker Ready** - Complete containerization with docker-compose

## 🛠️ Technology Stack

| Layer | Technologies |
|-------|-------------|
| **Frontend** | React 18, TypeScript, Vite, TailwindCSS, shadcn/ui |
| **Backend** | .NET 8/9, C# 12, SignalR, Serilog |
| **Database** | TimescaleDB (PostgreSQL), Redis |
| **Infrastructure** | Docker, nginx, Grafana, Prometheus |
| **Hardware** | ADAM-6051 Modbus TCP/IP |

## 🏗️ Project Structure

```
adam-6000-counter/
├── docs/                     # 📚 All documentation
├── src/                      # 🔧 Backend services (.NET)
├── platform-frontend/        # 📊 React dashboard
├── scripts/                  # 🛠️ Development scripts
├── docker/                   # 🐳 Docker configuration
├── README.md                # This file
├── QUICKSTART.md           # Fast setup guide
└── CLAUDE.md               # AI development guide
```

## 🤝 Contributing

1. Read [Development Standards](./docs/03-development/coding-standards.md)
2. Follow [CLAUDE.md](./CLAUDE.md) for AI-assisted development
3. Write tests for new features
4. Submit PRs to `develop` branch

## 📄 License

MIT License - See [LICENSE](./LICENSE) file

## 🆘 Support

- [Documentation](./docs/) - Complete guides
- [Troubleshooting](./docs/01-getting-started/troubleshooting.md) - Common issues
- [GitHub Issues](https://github.com/your-org/adam-6000-counter/issues) - Bug reports

---

*Built with ❤️ for Industrial Automation*