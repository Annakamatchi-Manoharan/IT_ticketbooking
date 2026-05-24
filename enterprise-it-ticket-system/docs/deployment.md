# Deployment Steps
1. Copy env templates and set production secrets.
2. Start containers: docker compose -f infra/docker-compose.yml up --build
3. Validate health endpoint and Swagger docs.
4. Configure TLS termination and WAF for production.
5. Configure backups, monitoring, and alerts.

