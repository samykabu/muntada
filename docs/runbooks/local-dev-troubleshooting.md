# Local Development Troubleshooting

## Aspire won't start

**Symptom**: `dotnet run --project aspire/Muntada.AppHost` fails.

**Fix**:
- Ensure Docker Desktop is running
- Check .NET SDK: `dotnet --version` (needs 10.0+)
- Check Aspire workload: `dotnet workload list`
- Install if missing: `dotnet workload install aspire`

## Port already in use

**Symptom**: Address already in use error.

**Fix**:
```bash
# Find what's using the port
netstat -ano | findstr :5000   # Windows
lsof -i :5000                  # macOS/Linux

# Kill the process or change ports in AppHost
```

## SQL Server connection fails

**Symptom**: Health check `/health/ready` returns unhealthy for SQL.

**Fix**:
- Verify SQL container is running: `docker ps | grep sql`
- Check Aspire Dashboard for SQL health
- Aspire injects connection strings automatically — do NOT hardcode

## Frontend can't reach backend

**Symptom**: Network errors in browser console.

**Fix**:
- Verify both services are running in Aspire Dashboard
- Check CORS: backend must allow `http://localhost:3000`
- Check `VITE_API_BASE_URL` in frontend `.env.local`

## Tests fail

**Symptom**: `dotnet test` or `npm test` fails.

**Fix**:
- Ensure all NuGet packages restored: `dotnet restore backend/Muntada.slnx`
- Ensure npm packages installed: `cd frontend && npm ci`
- Check test output for specific error messages
