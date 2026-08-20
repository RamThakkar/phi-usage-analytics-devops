# Phibonacci Usage Analytics

A .NET 6 API with static HTML dashboard for viewing organization-wise content usage reports. Powered by Redis cache for instant page loads.

**Live:** https://usage.phibonaccisolutions.com

---

## What It Does

```
Login (username + password)
  → Organization Dashboard (summary cards + charts)
    → License-wise usage (paginated, sortable, searchable)
      → Grade-wise breakdown
        → Subject-wise breakdown (with charts)
          → Chapter-wise breakdown (with charts)
            → Topic-wise details (Video ✅/❌ + Simulation ✅/❌ + coverage chart)
```

---

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | .NET 6 Web API |
| Database | SQL Server (PhiSyllabusDb — read-only) |
| ORM | Entity Framework Core 6 |
| Cache | Redis (port 7575, 24h TTL) |
| Frontend | Static HTML + Tailwind CSS + Chart.js |
| Auth | Token-based (in-memory sessions) |
| Architecture | Clean Architecture (4 projects) |

---

## Project Structure

```
PhiUsageAnalytics.sln
├── src/
│   ├── PhiUsageAnalytics.Domain/              # Entities
│   │   └── Entities/
│   │       ├── License.cs
│   │       ├── LicenseActivation.cs
│   │       ├── PanelUsageData.cs
│   │       ├── SubCategory.cs
│   │       ├── SubCategoryDetail.cs
│   │       ├── Topic.cs
│   │       └── TopicDetail.cs
│   │
│   ├── PhiUsageAnalytics.Application/         # Business logic
│   │   ├── DTOs/
│   │   ├── Interfaces/ (IAnalyticsRepository, ICacheService)
│   │   ├── Services/ (AnalyticsService)
│   │   └── DependencyInjection.cs
│   │
│   ├── PhiUsageAnalytics.Infrastructure/      # Data access + Redis
│   │   ├── Data/ (SyllabusDbContext)
│   │   ├── Cache/ (RedisCacheService)
│   │   ├── Repositories/ (AnalyticsRepository)
│   │   └── DependencyInjection.cs
│   │
│   └── PhiUsageAnalytics.Api/                 # Web API + Frontend
│       ├── Controllers/ (AnalyticsController)
│       ├── Middleware/ (Auth, RateLimit, ErrorHandling)
│       ├── Services/ (SessionService, ErrorLogger)
│       ├── wwwroot/ (index.html, favicon.png, images/)
│       ├── Program.cs
│       └── appsettings.json
│
├── errors/                  # Auto-created error logs (gitignored)
├── DOCUMENTATION.md         # Complete detailed documentation
├── .gitignore
└── README.md
```

---

## Getting Started

### Prerequisites

- .NET 6 SDK (or .NET 8+ SDK which can target net6.0)
- SQL Server with existing `PhiSyllabusDb` database
- Redis Server running on port 7575

### Setup

1. **Clone:**
   ```bash
   git clone https://github.com/RamThakkar/phi-usage-analytics.git
   cd phi-usage-analytics
   ```

2. **Update `appsettings.json`:**
   ```json
   {
     "ConnectionStrings": {
       "PhiSyllabusDb": "Server=.;Database=PhiSyllabusDb;User Id=sa;Password=YOUR_PASS;Trusted_Connection=False;MultipleActiveResultSets=true;TrustServerCertificate=True",
       "RedisConnection": "localhost:7575,abortConnect=False"
     },
     "AnalyticsUsers": [
       {
         "Username": "tcs",
         "Password": "tcs@2026",
         "OrganizationId": "your-org-guid",
         "OrganizationName": "Your Organization Name"
       }
     ],
     "AndroidContentTypeCutoffDate": "2026-08-15"
   }
   ```

3. **Run:**
   ```bash
   dotnet run --project src/PhiUsageAnalytics.Api
   ```

4. **Open:** `https://localhost:5001`

5. **Login:** Username = `tcs`, Password = `tcs@2026`

---

## API Endpoints

All endpoints require `Authorization: Bearer {token}` (except login).

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/analytics/login` | Login → returns token |
| POST | `/api/analytics/logout` | Invalidate token |
| GET | `/api/analytics/organization/{orgId}/summary` | Org summary |
| GET | `/api/analytics/organization/{orgId}/licenses` | License list (paginated) |
| GET | `/api/analytics/organization/{orgId}/trend` | Daily trend chart |
| GET | `/api/analytics/license/{key}/grades` | Grade breakdown |
| GET | `/api/analytics/license/{key}/grades/{gradeId}/subjects` | Subjects |
| GET | `/api/analytics/license/{key}/grades/{gradeId}/subjects/{subjectId}/chapters` | Chapters |
| GET | `/api/analytics/license/{key}/chapters/{chapterId}/topics` | Topics |
| GET | `/api/analytics/license/{key}/export` | Download CSV |
| POST | `/api/analytics/organization/{orgId}/refresh-cache` | Clear Redis cache |

---

## Security

- **Token auth** — 8-hour session tokens (in-memory)
- **Rate limiting** — 30 requests/minute per IP
- **Login lockout** — 5 failed attempts = 15 min block
- **HTTPS redirect** — HTTP auto-redirects to HTTPS
- **XSS protection** — All rendered data escaped

---

## Caching (Redis)

- All data cached in Redis until **5:00 AM IST** (23:30 UTC)
- First request of the day hits DB → stores in Redis
- All subsequent requests → instant from Redis (<5ms)
- Task Scheduler warms cache at 5:01 AM IST (users never wait)
- Manual clear: `POST /api/analytics/organization/{id}/refresh-cache`

---

## Adding a New Organization

No code changes. Just add to `appsettings.json`:

```json
{
  "Username": "newschool",
  "Password": "newschool@2026",
  "OrganizationId": "guid-from-database",
  "OrganizationName": "New School Name"
}
```

Restart the app. Done.

---

## Detailed Documentation

See **[DOCUMENTATION.md](DOCUMENTATION.md)** for complete details on:
- Architecture, database schema, all configurations
- Platform ContentTypeId mapping (Android vs Windows bug)
- Deployment guide (IIS + Redis + SSL + Task Scheduler)
- Troubleshooting guide
- Performance optimization notes

---

## License

Private — Phibonacci Learning. Internal use only.
