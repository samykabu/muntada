# Secret Rotation Procedure

## Overview

All secrets are stored in Kubernetes Secrets and mounted as environment variables.
Application configuration supports hot-reload via IOptionsMonitor.

## Rotating Database Credentials

```bash
# 1. Generate new password
NEW_PASSWORD=$(openssl rand -base64 32)

# 2. Update Kubernetes Secret
kubectl create secret generic muntada-sql-secret \
  --from-literal=sa-password="$NEW_PASSWORD" \
  --namespace muntada-prod \
  --dry-run=client -o yaml | kubectl apply -f -

# 3. Restart pods to pick up new secret (rolling restart)
kubectl rollout restart deployment/muntada-api -n muntada-prod

# 4. Verify connectivity
kubectl exec -it deployment/muntada-api -n muntada-prod -- \
  curl -s http://localhost:8080/health/ready
```

## Rotating RabbitMQ Credentials

```bash
kubectl create secret generic rabbitmq-secret \
  --from-literal=username=muntada \
  --from-literal=password="$(openssl rand -base64 32)" \
  --namespace muntada-prod \
  --dry-run=client -o yaml | kubectl apply -f -

kubectl rollout restart deployment/muntada-api -n muntada-prod
```

## Rotating LiveKit API Keys

```bash
kubectl create secret generic livekit-secret \
  --from-literal=api-key="$(openssl rand -hex 16)" \
  --from-literal=api-secret="$(openssl rand -base64 32)" \
  --namespace muntada-prod \
  --dry-run=client -o yaml | kubectl apply -f -

kubectl rollout restart deployment/muntada-api -n muntada-prod
```

## Verification

After any rotation:
1. Check `/health/ready` — all dependency checks must pass
2. Check application logs for connection errors
3. Verify traces in Jaeger for failed requests
