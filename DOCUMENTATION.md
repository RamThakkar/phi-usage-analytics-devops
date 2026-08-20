# Phibonacci Usage Analytics — Complete Project Documentation

> **Last Updated:** August 18, 2026  
> **Tech Stack:** .NET 6 | EF Core | SQL Server | Redis | Static HTML | Chart.js  
> **Live URL:** https://usage.phibonaccisolutions.com

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture](#2-architecture)
3. [Project Structure](#3-project-structure)
4. [Database Schema](#4-database-schema)
5. [API Endpoints](#5-api-endpoints)
6. [Authentication & Security](#6-authentication--security)
7. [Redis Caching Strategy](#7-redis-caching-strategy)
8. [Platform ContentTypeId Mapping](#8-platform-contenttypeid-mapping)
9. [Frontend (Static HTML)](#9-frontend-static-html)
10. [Configuration (appsettings)](#10-configuration-appsettings)
11. [Deployment Guide](#11-deployment-guide)
12. [Task Scheduler (Cache Warm-up)](#12-task-scheduler-cache-warm-up)
13. [Adding a New Organization](#13-adding-a-new-organization)
14. [Error Logging](#14-error-logging)
15. [Troubleshooting](#15-troubleshooting)
16. [Performance Notes](#16-performance-notes)

---

## 1. Project Overview

This is a **read-only analytics dashboard** that shows content usage reports for Phibonacci's educational platform. Organizations (schools) can login and see how their licensed panels are being used — broken down by license, grade, subject, chapter, and topic.

### Key Features:
- Organization-scoped login (simple username/password from config)
- License-wise usage with pagination, search, sort, and status filter
- Drill-down: License → Grade → Subject → Chapter → Topic
- Video vs Simulation (Interactive) split at every level
- Charts (donut, line, horizontal bar) at each drill-down level
- Platform icons (Windows/Android) per license
- CSV export per license
- Redis cache (daily refresh at 5 AM IST)
- Token-based auth + rate limiting + login lockout

### Data Flow:
```
User → Static HTML (index.html)
         → fetch('/api/analytics/...')
              → AnalyticsController
                   → AnalyticsService
                        → AnalyticsRepository
                             → Check Redis Cache
                                  → HIT: return cached data (instant)
                                  → MISS: query SQL Server → store in Redis → return
```

---

## 2. Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    PRESENTATION                           │
│  wwwroot/index.html (Tailwind CSS + Chart.js + Vanilla JS) │
├─────────────────────────────────────────────────────────┤
│                    API LAYER                              │
│  Controllers/AnalyticsController.cs                      │
│  Middleware/ (Auth, RateLimit, ErrorHandling)             │
│  Services/ (SessionService, ErrorLogger)                 │
├─────────────────────────────────────────────────────────┤
│                    APPLICATION LAYER                      │
│  Services/AnalyticsService.cs                            │
│  Interfaces/ (IAnalyticsRepository, ICacheService)       │
│  DTOs/ (all response models)                             │
├─────────────────────────────────────────────────────────┤
│                    INFRASTRUCTURE LAYER                   │
│  Repositories/AnalyticsRepository.cs                     │
│  Cache/RedisCacheService.cs                              │
│  Data/SyllabusDbContext.cs                               │
├─────────────────────────────────────────────────────────┤
│                    DOMAIN LAYER                           │
│  Entities/ (License, PanelUsageData, Topic, etc.)        │
├─────────────────────────────────────────────────────────┤
│                    EXTERNAL                               │
│  SQL Server (PhiSyllabusDb) | Redis (localhost:7575)     │
└─────────────────────────────────────────────────────────┘
```

---

## 3. Project Structure

```
PhiUsageAnalytics.sln
├── src/
│   ├── PhiUsageAnalytics.Domain/              # Entities only (POCOs)
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
│   │   │   ├── OrganizationSummaryDto.cs
│   │   │   ├── LicenseUsageDto.cs
│   │   │   ├── GradeUsageDto.cs
│   │   │   ├── SubjectUsageDto.cs
│   │   │   ├── ChapterUsageDto.cs
│   │   │   ├── TopicUsageDto.cs
│   │   │   ├── DailyUsageTrendDto.cs
│   │   │   ├── PagedResultDto.cs
│   │   │   └── LoginRequestDto.cs
│   │   ├── Interfaces/
│   │   │   ├── IAnalyticsRepository.cs
│   │   │   └── ICacheService.cs
│   │   ├── Services/
│   │   │   └── AnalyticsService.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── PhiUsageAnalytics.Infrastructure/      # Data access + caching
│   │   ├── Data/
│   │   │   └── AnalyticsDbContext.cs (SyllabusDbContext)
│   │   ├── Cache/
│   │   │   └── RedisCacheService.cs
│   │   ├── Repositories/
│   │   │   └── AnalyticsRepository.cs
│   │   └── DependencyInjection.cs
│   │
│   └── PhiUsageAnalytics.Api/                 # Web API + Frontend
│       ├── Controllers/
│       │   └── AnalyticsController.cs
│       ├── Middleware/
│       │   ├── AuthMiddleware.cs
│       │   ├── RateLimitMiddleware.cs
│       │   └── ErrorHandlingMiddleware.cs
│       ├── Services/
│       │   ├── SessionService.cs
│       │   └── ErrorLogger.cs
│       ├── wwwroot/
│       │   ├── index.html
│       │   ├── favicon.png
│       │   └── images/
│       │       ├── windows.svg
│       │       └── android.svg
│       ├── Program.cs
│       ├── appsettings.json
│       └── appsettings.Development.json
│
├── errors/                                    # Error logs (auto-created, gitignored)
│   └── 2026-08-18/
│       └── error.log
├── .gitignore
├── README.md
└── DOCUMENTATION.md (this file)
```

---

## 4. Database Schema

### Database: PhiSyllabusDb

#### Tables Used:

| Table | Purpose | Key Columns |
|-------|---------|-------------|
| `Licenses` | License keys per organization | Key, OrganizationId, IsActive |
| `LicenseActivations` | Activation details (platform, consumer) | LicenseKey, Platform, ConsumerName, ActivatedDate |
| `PanelUsageDatas` | Content usage records (main data) | LicenseKey, ContentTypeId, UsageTime, GradeId, SubjectId, ChapterId, TopicId, CreatedDate |
| `Topics` | Topic master (ID only) | Id |
| `TopicDetails` | Topic names per language | TopicId, LanguageId, Name |
| `SubCategories` | Grade/Subject/Chapter hierarchy | Id, ParentId |
| `SubCategoryDetails` | Names per language | SubCategoryId, LanguageId, Name |

#### ContentTypeId Values:
| Platform | Video | Simulation/Interactive |
|----------|-------|----------------------|
| Windows | 2 | 3 |
| Android (activated before Aug 15, 2026) | 1 | 2 |
| Android (activated after Aug 15, 2026) | 2 | 3 |

#### Important Notes:
- `LicenseKey` column is `nvarchar(max)` — **cannot be indexed** directly
- Names are stored in separate "Detail" tables (multi-language support via LanguageId)
- We use `LanguageId = 1` (English) for all name lookups
- No FK between Licenses.OrganizationId and any org table (cross-database reference to PhiLMSDb)

---

## 5. API Endpoints

All endpoints require `Authorization: Bearer {token}` header (except login).

### Authentication:

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/analytics/login` | Login with username/password → returns token |
| POST | `/api/analytics/logout` | Invalidate session token |

### Data Endpoints:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/analytics/organization/{orgId}/summary` | Org summary (sessions, video, sim totals) |
| GET | `/api/analytics/organization/{orgId}/licenses` | License list (paginated, sortable, filterable) |
| GET | `/api/analytics/organization/{orgId}/trend` | Daily usage trend (for line chart) |
| GET | `/api/analytics/license/{key}/grades` | Grade breakdown for a license |
| GET | `/api/analytics/license/{key}/grades/{gradeId}/subjects` | Subject breakdown |
| GET | `/api/analytics/license/{key}/grades/{gradeId}/subjects/{subjectId}/chapters` | Chapter breakdown |
| GET | `/api/analytics/license/{key}/chapters/{chapterId}/topics` | Topic breakdown |
| GET | `/api/analytics/license/{key}/export` | Download CSV report |
| POST | `/api/analytics/organization/{orgId}/refresh-cache` | Manually clear Redis cache |

### License Listing Query Parameters:

```
?page=1&pageSize=10&search=86960&status=with_usage&sortBy=totalUsageSeconds&sortDir=desc
```

| Param | Options |
|-------|---------|
| `page` | Page number (default: 1) |
| `pageSize` | Items per page (default: 10) |
| `search` | Filter by license key (contains) |
| `status` | `with_usage`, `without_usage`, or empty (all) |
| `sortBy` | `totalSessions`, `totalUsageSeconds`, `videoUsageSeconds`, `simulationUsageSeconds`, `lastUsageDate` |
| `sortDir` | `asc` or `desc` |
| `fromDate` | Date filter start (yyyy-MM-dd) |
| `toDate` | Date filter end (yyyy-MM-dd) |

---

## 6. Authentication & Security

### Token-Based Auth (In-Memory Sessions):
- Login returns a GUID token (stored in `ConcurrentDictionary`)
- Token valid for 8 hours
- All API calls must include `Authorization: Bearer {token}`
- Tokens lost on app restart (users re-login)

### Rate Limiting:
- 30 requests/minute per IP address
- Returns `429 Too Many Requests` if exceeded

### Login Lockout:
- 5 failed login attempts → 15 minute IP lockout
- Cleared on successful login

### XSS Protection:
- `escapeHtml()` function sanitizes all data before innerHTML rendering

### HTTPS:
- `app.UseHttpsRedirection()` redirects all HTTP → HTTPS

---

## 7. Redis Caching Strategy

### Connection:
```
localhost:7575,abortConnect=False
```

### TTL (Time-To-Live):
- All keys expire at **5:00 AM IST (23:30 UTC)**
- Fresh data fetched from DB once per day (on first request after expiry)
- Subsequent requests served from Redis (<5ms)

### Cache Keys:

| Key Pattern | Data | When cached |
|-------------|------|-------------|
| `org:{orgId}:summary` | Organization totals | First summary request of the day |
| `org:{orgId}:licenses:all` | Full license list with usage | First license request of the day |
| `org:{orgId}:trend` | Daily trend chart data | First trend request of the day |
| `license:{key}:grades` | Grade breakdown | On drill-down |
| `license:{key}:grade:{id}:subjects` | Subject breakdown | On drill-down |
| `license:{key}:grade:{id}:subject:{id}:chapters` | Chapter breakdown | On drill-down |
| `license:{key}:chapter:{id}:topics` | Topic breakdown | On drill-down |

### Manual Cache Clear:
```
POST /api/analytics/organization/{orgId}/refresh-cache
```
Or via RedisInsight CLI: `FLUSHDB`

### Date-Filtered Queries:
When user applies a date filter, data is **NOT cached** (fetched fresh from DB every time). Only the default (no-filter) view is cached.

---

## 8. Platform ContentTypeId Mapping

### The Problem:
Android app sends different ContentTypeIds than Windows app:
- **Windows:** Video = 2, Simulation = 3
- **Android (old, before Aug 15, 2026):** Video = 1, Simulation = 2

### The Solution:
The system checks each license's **platform** and **activation date** from `LicenseActivations` table, then maps ContentTypeIds accordingly.

### Logic (in code):
```csharp
if (platform == "android" && activatedDate < cutoffDate)
    return (videoId: 1, simId: 2);  // Old Android mapping

return (videoId: 2, simId: 3);  // Windows + new Android (standard)
```

### Configuration:
```json
"AndroidContentTypeCutoffDate": "2026-08-15"
```

### When Android app is fixed:
Once ALL Android panels send Video=2, Sim=3, you can set the cutoff date to a past date (e.g., `"2020-01-01"`) to disable the old mapping.

---

## 9. Frontend (Static HTML)

### Location: `src/PhiUsageAnalytics.Api/wwwroot/index.html`

### Tech:
- **Tailwind CSS** (CDN) — styling
- **Chart.js 4.4** (CDN) — all charts
- **Vanilla JavaScript** — no framework, no build step

### Pages/Levels:
1. **Login** — Split-screen, username + password
2. **Organization Dashboard** — Summary cards + donut chart + trend chart + license table
3. **License Detail** — Grade-wise bar charts + table
4. **Grade → Subjects** — Horizontal bar charts + table
5. **Subject → Chapters** — Horizontal bar charts + table
6. **Chapter → Topics** — Donut (coverage) + bar chart + table

### Features:
- Progressive loading (page shell renders instantly, data fills in)
- Loading spinners on all API calls
- Breadcrumb navigation (clickable)
- Date range filter with smart Apply/Clear button
- License status filter (All / With Usage / Without Usage)
- Column sorting (server-side, across all pages)
- Pagination (First / Back / Next / Last)
- License key search (400ms debounce)
- Platform icons (Windows blue, Android green)
- Consumer name column
- CSV download button per license
- Color-coded rows (white=data, amber=no data)
- XSS protection via `escapeHtml()`

---

## 10. Configuration (appsettings)

### appsettings.json:
```json
{
  "ConnectionStrings": {
    "PhiSyllabusDb": "Server=.;Database=PhiSyllabusDb;User Id=sa;Password=***;Trusted_Connection=False;MultipleActiveResultSets=true;Connection Timeout=180;TrustServerCertificate=True",
    "RedisConnection": "localhost:7575,abortConnect=False"
  },
  "AnalyticsUsers": [
    {
      "Username": "tcs",
      "Password": "tcs@2026",
      "OrganizationId": "60ecb9a4-2700-4cd5-91a9-3d7501f09e48",
      "OrganizationName": "TCS DELHI KV PILOT"
    },
    {
      "Username": "cgbse",
      "Password": "cgbse@2026",
      "OrganizationId": "70009713-d1ff-48d4-9f18-0ae6d4b1d90f",
      "OrganizationName": "TCS CHHATTISGARH KV PILOT"
    }
  ],
  "AndroidContentTypeCutoffDate": "2026-08-15",
  "AllowedHosts": "*"
}
```

### Key Config Explained:

| Key | Purpose |
|-----|---------|
| `PhiSyllabusDb` | SQL Server connection (all data tables) |
| `RedisConnection` | Redis cache server |
| `AnalyticsUsers` | Login credentials → OrganizationId mapping |
| `AndroidContentTypeCutoffDate` | Date before which Android uses old ContentTypeId mapping |

---

## 11. Deployment Guide

### Prerequisites:
- Windows Server with IIS
- .NET 6 Runtime
- SQL Server (with PhiSyllabusDb)
- Redis Server on port 7575
- SSL certificate (Let's Encrypt or similar)

### Steps:

1. **Publish the application:**
```bash
dotnet publish src/PhiUsageAnalytics.Api -c Release -o ./publish
```

2. **Copy to server:**
Copy the `publish/` folder to your IIS site directory (e.g., `E:\Sites\phi-usage-analytics\`)

3. **Add favicon:**
Place `favicon.png` in the publish folder's `wwwroot/` directory.

4. **Configure appsettings.json:**
Update connection strings, credentials, and org details on the server.

5. **IIS Setup:**
- Create a new site pointing to the publish folder
- Application Pool: .NET CLR = No Managed Code, Pipeline = Integrated
- Bindings: HTTP (port 80) + HTTPS (port 443) with SSL certificate
- App Pool Advanced Settings: Start Mode = AlwaysRunning, Idle Timeout = 0

6. **Verify:**
Open `https://your-domain.com` → should show login page.

---

## 12. Task Scheduler (Cache Warm-up)

### Purpose:
Pre-loads data into Redis at 5:01 AM IST so users never see the slow first-load.

### Script Location:
`C:\Scripts\warm-analytics-cache.ps1`

### Script Content:
```powershell
$baseUrl = "https://usage.phibonaccisolutions.com"

$organizations = @(
    @{ Username = "tcs"; Password = "tcs@2026"; OrgId = "60ecb9a4-2700-4cd5-91a9-3d7501f09e48" },
    @{ Username = "cgbse"; Password = "cgbse@2026"; OrgId = "70009713-d1ff-48d4-9f18-0ae6d4b1d90f" }
)

foreach ($org in $organizations) {
    try {
        $body = @{ username = $org.Username; password = $org.Password } | ConvertTo-Json
        $login = Invoke-RestMethod -Uri "$baseUrl/api/analytics/login" -Method POST -ContentType "application/json" -Body $body
        if ($login.success) {
            $headers = @{ "Authorization" = "Bearer $($login.token)" }
            $orgId = $org.OrgId
            Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/summary" -Headers $headers | Out-Null
            Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/licenses?page=1&pageSize=10" -Headers $headers | Out-Null
            Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/trend" -Headers $headers | Out-Null
            Write-Host "[$(Get-Date)] Cache warmed for: $($org.Username)"
        }
    } catch {
        Write-Host "[$(Get-Date)] ERROR for $($org.Username): $_"
    }
}
```

### Task Scheduler:
- Trigger: Daily at **11:31 PM UTC** (= 5:01 AM IST)
- Program: `powershell.exe`
- Arguments: `-ExecutionPolicy Bypass -File "C:\Scripts\warm-analytics-cache.ps1"`
- Run whether user is logged on or not: Yes

---

## 13. Adding a New Organization

**No code changes needed.** Just:

1. Add entry to `appsettings.json` on the server:
```json
{
  "Username": "newschool",
  "Password": "newschool@2026",
  "OrganizationId": "guid-from-philmsdb-organizations-table",
  "OrganizationName": "New School Name"
}
```

2. Add to the warm-up script (`C:\Scripts\warm-analytics-cache.ps1`):
```powershell
@{ Username = "newschool"; Password = "newschool@2026"; OrgId = "guid-here" }
```

3. Restart the IIS site.

4. Give the user their credentials: Username = `newschool`, Password = `newschool@2026`

---

## 14. Error Logging

### Location:
```
{app-folder}/errors/{yyyy-MM-dd}/error.log
```

### Format:
```
[14:35:22] | GET /api/analytics/license/123/grades | SqlException | Invalid column name 'X'
         Stack: at Repository.GetGradeUsageAsync() → at Controller.GetGradeUsage()
```

### What's logged:
- Timestamp
- HTTP method + endpoint
- Exception type
- Message (max 200 chars)
- Stack trace (top 2 lines)

### Notes:
- New folder created per day automatically
- Thread-safe (concurrent writes won't corrupt)
- `errors/` folder is in `.gitignore`
- Logging never crashes the app (wrapped in try-catch)

---

## 15. Troubleshooting

### Page shows "Loading..." forever:
1. Check `errors/` folder for logs
2. Check if Redis is running: `redis-cli -p 7575 PING` (or RedisInsight)
3. Check if SQL Server is reachable
4. Restart IIS site

### Wrong Video/Simulation values:
1. Check the license's platform in `LicenseActivations` table
2. Verify `AndroidContentTypeCutoffDate` in appsettings
3. Clear Redis cache: `FLUSHDB` in RedisInsight

### Login fails:
1. Check username/password in `appsettings.json`
2. Check if IP is locked out (wait 15 minutes or restart app)
3. Check `.Trim()` — spaces in username/password?

### 401 Unauthorized on API calls:
1. Token expired (8 hours) — re-login
2. App restarted — all tokens invalidated — re-login
3. Token not sent in header — check frontend code

### 429 Too Many Requests:
1. Rate limit: 30 requests/minute per IP
2. Wait 60 seconds and retry
3. Or restart the app to clear rate limit counters

### SQL Timeout:
1. Check network connectivity to SQL Server
2. Redis cache should prevent most DB hits (clear and re-cache)
3. CommandTimeout is set to 120 seconds

### Cache not refreshing:
1. Check Task Scheduler "History" tab for errors
2. Run the warm-up script manually in PowerShell
3. Verify Redis TTL: keys should expire at 23:30 UTC (5 AM IST)

---

## 16. Performance Notes

### Current Bottleneck:
`PanelUsageDatas.LicenseKey` is `nvarchar(max)` — **cannot be indexed**. Queries do full table scans on 732K+ rows.

### Mitigation:
- **Redis cache** — DB hit only once per day per org
- **Task Scheduler** — pre-warms cache at night, users never wait
- **Progressive loading** — page renders instantly, data fills in
- **Pagination** — only 10 licenses per page
- **Server-side sort** — cached results sorted in memory

### If Performance Degrades Further:
1. Consider changing `LicenseKey` column to `nvarchar(50)` (requires schema change)
2. Then add index: `CREATE INDEX IX ON PanelUsageDatas (LicenseKey) INCLUDE (...)`
3. This would make all queries < 1 second

### Current Performance Profile:
| Operation | First of Day (no cache) | Cached (Redis) |
|-----------|------------------------|----------------|
| Login | Instant | Instant |
| Summary | 10-15s | <5ms |
| License list | 10-15s | <5ms |
| Daily trend | 5-10s | <5ms |
| Grade drill-down | 2-5s | <5ms |
| Subject/Chapter/Topic | 1-3s | <5ms |

---

## License

Private — Internal use only. Phibonacci Learning.
