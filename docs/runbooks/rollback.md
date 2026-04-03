# Rollback Procedure

## Helm Rollback

To roll back to a previous Helm release:

```bash
# List release history
helm history muntada -n muntada-prod

# Rollback to a specific revision
helm rollback muntada <REVISION> -n muntada-prod

# Verify rollout
kubectl rollout status deployment/muntada-api -n muntada-prod
kubectl rollout status deployment/muntada-frontend -n muntada-prod
```

## Docker Image Rollback

If rolling back to a specific image version:

```bash
# Set API image to a previous git SHA
kubectl set image deployment/muntada-api api=<DOCKERHUB_USER>/muntada-api:<GIT_SHA> -n muntada-prod

# Set frontend image
kubectl set image deployment/muntada-frontend frontend=<DOCKERHUB_USER>/muntada-frontend:<GIT_SHA> -n muntada-prod
```

## Database Migration Rollback

**IMPORTANT**: Database migrations MUST be forward and backward compatible.

```bash
# Check current migration status
dotnet ef migrations list --project backend/src/Muntada.Api

# Rollback to a specific migration
dotnet ef database update <MIGRATION_NAME> --project backend/src/Muntada.Api
```

## Verification After Rollback

1. Check health endpoints: `curl http://<service>/health`
2. Verify pods are running: `kubectl get pods -n muntada-prod`
3. Check logs: `kubectl logs -l app=muntada-api -n muntada-prod --tail=50`
4. Verify traces in Jaeger (production) or Aspire Dashboard (dev)
