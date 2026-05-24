# Architecture
- Monorepo: apps/api + apps/web + docs + tests + infra
- Backend: Express MVC + PostgreSQL + JWT RBAC + security middleware
- Frontend: Next.js + Tailwind responsive dashboard
- Infra: Docker Compose with Postgres, Redis, API, Web

```mermaid
erDiagram
  USERS ||--o{ TICKETS : creates
  USERS ||--o{ TICKETS : assigned
  TICKETS ||--o{ COMMENTS : has
  USERS ||--o{ COMMENTS : writes
  USERS ||--o{ NOTIFICATIONS : receives
  USERS ||--o{ AUDIT_LOGS : performs
```

