# Industrial ADAM Development Credentials & Configuration

**⚠️ DEVELOPMENT ONLY - NOT FOR PRODUCTION ⚠️**

## Database Credentials

### TimescaleDB (PostgreSQL)
**CORRECT CREDENTIALS** (verified working in Logger API):
```bash
TIMESCALE_HOST=localhost
TIMESCALE_PORT=5433
TIMESCALE_DATABASE=adam_counters
TIMESCALE_USERNAME=industrial_system
TIMESCALE_PASSWORD=IndustrialCounter2024!@#$

# Alternative variable names used in some contexts:
TIMESCALEDB_USERNAME=industrial_system  
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$
```

**NOTE**: The OEE API is currently using WRONG credentials. It should use the same as Logger API above.

### Database Connection String
```bash
Host=localhost;Port=5433;Database=adam_counters;Username=industrial_system;Password=IndustrialCounter2024!@#$
```

## JWT Configuration

```bash
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d
JWT_ISSUER=Industrial.Adam.System
JWT_AUDIENCE=Industrial.Adam.APIs
```

## Application Ports

### Backend APIs
```bash
# OEE WebApi
ASPNETCORE_URLS=http://localhost:5001

# Logger WebApi (default)
# Uses standard ASP.NET Core port configuration

# Equipment Scheduling WebApi
# Port 5141 (as configured)
```

### Frontend
```bash
# Platform Frontend
http://localhost:3001
```

## Complete Environment Variables for Backend Services

### OEE WebApi Startup
```bash
cd /home/grant/adam-6000-counter/src/Industrial.Adam.Oee/WebApi
ASPNETCORE_URLS=http://localhost:5001 \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run
```

### Logger WebApi Startup
```bash
cd /home/grant/adam-6000-counter/src/Industrial.Adam.Logger.WebApi
TIMESCALE_HOST=localhost \
TIMESCALE_PORT=5433 \
TIMESCALE_DATABASE=adam_counters \
TIMESCALE_USERNAME=industrial_system \
TIMESCALE_PASSWORD=IndustrialCounter2024!@#$ \
TIMESCALEDB_USERNAME=industrial_system \
TIMESCALEDB_PASSWORD=IndustrialCounter2024!@#$ \
JWT_SECRET_KEY=8f9e2a1b5c6d3e7f4g8h9i2j5k6l3m7n8o9p2q5r6s3t7u8v9w2x5y6z3a7b8c9d \
JWT_ISSUER=Industrial.Adam.System \
JWT_AUDIENCE=Industrial.Adam.APIs \
dotnet run
```

### Platform Frontend Startup
```bash
cd /home/grant/adam-6000-counter/platform-frontend
npm run dev
```

## Docker Database Startup

### TimescaleDB Container
```bash
TIMESCALE_DATABASE=adam_counters \
TIMESCALE_USERNAME=adam_user \
TIMESCALE_PASSWORD=adam_password \
docker-compose up -d timescaledb
```

## Development User Accounts

### Default Admin User (for testing)
```
Username: admin@industrial-adam.local
Password: AdminPass123!
Role: SystemAdmin
```

### Default Operator User (for testing)  
```
Username: operator@industrial-adam.local
Password: OperatorPass123!
Role: Operator
```

## API Endpoints Quick Reference

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Token refresh
- `GET /api/security/audit` - Security audit logs

### Device Management (Logger API)
- `GET /api/devices` - List all ADAM devices
- `GET /api/devices/{id}` - Get device details
- `GET /api/counters` - Query counter data
- `GET /api/counters/latest` - Latest readings

### OEE Management (OEE API)
- `GET /api/oee/current` - Current OEE metrics
- `GET /api/workorders` - List work orders
- `POST /api/workorders/start` - Start work order
- `GET /api/stoppages` - Stoppage events

### Equipment Scheduling
- `GET /api/resources` - Equipment hierarchy
- `GET /api/schedules` - Generated schedules
- `POST /api/schedules/generate` - Generate schedule

## Development Notes

1. **Database**: TimescaleDB running in Docker on port 5433
2. **Frontend**: React app with Vite dev server, hot reload enabled
3. **Backend**: Multiple .NET 9 APIs with JWT authentication
4. **CORS**: Configured to allow localhost development
5. **SSL**: Development uses HTTP (not HTTPS) for simplicity

## Troubleshooting

### Common Issues
1. **Port conflicts**: Change ports if services conflict
2. **Database connection**: Ensure TimescaleDB container is running
3. **CORS errors**: Check API CORS configuration for localhost
4. **JWT errors**: Verify all services use same JWT secret key

### Useful Commands
```bash
# Check running processes
ss -tlnp | grep :5001
ss -tlnp | grep :5433

# Restart TimescaleDB
docker-compose restart timescaledb

# Clear npm cache if frontend has issues
cd platform-frontend && npm cache clean --force
```

---
*Last updated: August 23, 2025*  
*⚠️ These credentials are for development only and must not be used in production environments ⚠️*