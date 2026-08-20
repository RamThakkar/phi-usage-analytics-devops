# ============================================
# Phibonacci Analytics - Cache Warm-up Script
# Runs at 11:30 PM UTC (5:00 AM IST) daily
# via Windows Task Scheduler
# ============================================
# Warms: Summary, Trend, Insights, Licenses,
#         Grades (for ALL licenses), Subjects
# ============================================

$baseUrl = "https://usage.phibonaccisolutions.com"
$warmupKey = "phi-cache-warmup-2026-secure"
$logFile = "C:\DatabaseBackupSchedular\logs\cache-warmup-$(Get-Date -Format 'yyyy-MM-dd').log"

# Ensure log directory exists
$logDir = Split-Path $logFile
if (!(Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }

function Write-Log($message) {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $entry = "[$timestamp] $message"
    Write-Host $entry
    Add-Content -Path $logFile -Value $entry
}

# Organization credentials (update with your actual values)
$organizations = @(
    @{ Username = "your-username-1"; Password = "your-password-1"; OrgId = "your-org-id-1" },
    @{ Username = "your-username-2"; Password = "your-password-2"; OrgId = "your-org-id-2" }
)

Write-Log "========== Cache Warm-up Started =========="

foreach ($org in $organizations) {
    try {
        Write-Log "Processing: $($org.Username)"

        # Login
        $body = @{ username = $org.Username; password = $org.Password } | ConvertTo-Json
        $loginHeaders = @{ "X-Warmup-Key" = $warmupKey }
        $login = Invoke-RestMethod -Uri "$baseUrl/api/analytics/login" -Method POST -ContentType "application/json" -Body $body -Headers $loginHeaders
        
        if (!$login.success) {
            Write-Log "  ERROR: Login failed for $($org.Username)"
            continue
        }

        $headers = @{ 
            "Authorization" = "Bearer $($login.token)"
            "X-Warmup-Key" = $warmupKey
        }
        $orgId = $org.OrgId

        # ===== LEVEL 1: Organization Summary =====
        Write-Log "  Warming: Summary, Trend, Licenses..."
        Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/summary" -Headers $headers | Out-Null
        Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/trend" -Headers $headers | Out-Null

        # ===== LEVEL 1: Insights =====
        Write-Log "  Warming: Insights (comparison, topics, heatmap)..."
        Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/comparison?days=30" -Headers $headers | Out-Null
        Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/top-bottom" -Headers $headers | Out-Null
        Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/popular-topics" -Headers $headers | Out-Null
        Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/least-engaged-topics" -Headers $headers | Out-Null
        Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/inactive-licenses?days=7" -Headers $headers | Out-Null
        Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/heatmap?days=30" -Headers $headers | Out-Null

        # ===== LEVEL 1: Get ALL licenses (paginate) =====
        Write-Log "  Warming: License pages..."
        $page = 1
        $allLicenses = @()
        
        do {
            $licensesResponse = Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/licenses?page=$page&pageSize=50&sortBy=totalUsageSeconds&sortDir=desc" -Headers $headers
            $allLicenses += $licensesResponse.items
            $page++
        } while ($page -le $licensesResponse.totalPages)

        Write-Log "  Found $($allLicenses.Count) licenses"

        # Also warm first page with default sort (what user sees on load)
        Invoke-RestMethod -Uri "$baseUrl/api/analytics/organization/$orgId/licenses?page=1&pageSize=10&sortBy=totalUsageSeconds&sortDir=desc" -Headers $headers | Out-Null

        # ===== LEVEL 2: Grades for ALL licenses with usage =====
        $licensesWithUsage = $allLicenses | Where-Object { $_.hasUsageData -eq $true }
        Write-Log "  Warming: Grades for $($licensesWithUsage.Count) licenses with usage..."
        
        $gradeCount = 0
        foreach ($license in $licensesWithUsage) {
            try {
                $licenseKey = $license.licenseKey
                $gradesResponse = Invoke-RestMethod -Uri "$baseUrl/api/analytics/license/$licenseKey/grades" -Headers $headers
                $gradeCount++

                # ===== LEVEL 3: Subjects for each grade =====
                foreach ($grade in $gradesResponse) {
                    try {
                        Invoke-RestMethod -Uri "$baseUrl/api/analytics/license/$licenseKey/grades/$($grade.gradeId)/subjects" -Headers $headers | Out-Null
                    } catch {
                        # Silently continue - subject cache is nice-to-have
                    }
                }

                # Brief pause to avoid overwhelming the server
                Start-Sleep -Milliseconds 200
            } catch {
                Write-Log "  WARN: Failed grades for license $($license.licenseKey): $_"
            }
        }

        Write-Log "  Warmed grades for $gradeCount licenses"

        # ===== Logout =====
        try {
            Invoke-RestMethod -Uri "$baseUrl/api/analytics/logout" -Method POST -Headers $headers | Out-Null
        } catch { }

        Write-Log "  DONE: $($org.Username)"
        Write-Log ""

    } catch {
        Write-Log "  ERROR for $($org.Username): $_"
    }
}

Write-Log "========== Cache Warm-up Complete =========="
Write-Log ""

# ===== RESTART SERVER =====
# Full restart needed to free memory from:
# - SQL Server buffer pool
# - .NET application (IIS/Kestrel) memory overhead
# - Any other accumulated memory usage
#
# Using 'shutdown /r' which is more reliable than Restart-Computer:
# - /r = restart
# - /t 30 = 30 second delay (allows script to finish logging)
# - /f = force close running applications
# - /d p:0:0 = planned restart reason (no unexpected shutdown flag)

Write-Log "Server restart scheduled in 30 seconds..."
Write-Log "Reason: Free SQL Server + Application memory after cache warming"

shutdown /r /t 30 /f /d p:0:0 /c "Scheduled restart after cache warm-up to free memory"

Write-Log "Shutdown command issued. Server will restart shortly."
Write-Log "========== Script End =========="
