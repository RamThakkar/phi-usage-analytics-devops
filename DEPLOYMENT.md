# Deployment Guide — Phibonacci Analytics

## What This Does

**Problem:** Every time you change code, you have to manually build, copy files to server, restart app — and worry about overwriting `appsettings.json`.

**Solution:** After this one-time setup, you just merge your code to `main` branch and it automatically deploys. `appsettings.json` is never touched.

---

## Prerequisites

- Your Windows Server with IIS (where the app is already running)
- Your GitHub repository: `https://github.com/RamThakkar/phi-usage-analytics`
- .NET 6 SDK installed on the server
- Administrator access to the server

---

## Step-by-Step Setup

### STEP 1: Find your App Pool name

1. Open **IIS Manager** on your server
2. Click **Application Pools** on the left
3. Find the App Pool that runs your analytics app
4. **Note down the name** (e.g., `PhiUsageAnalytics` or `DefaultAppPool` or whatever it is)

> ⚠️ You'll need this name later. If it's NOT `PhiUsageAnalytics`, you'll need to update the workflow file.

---

### STEP 2: Find your deploy folder path

1. In IIS Manager, click **Sites** on the left
2. Find your analytics site/application
3. Right-click → **Explore** (or check the Physical Path in the right panel)
4. **Note down the full path** (e.g., `C:\inetpub\wwwroot\PhiUsageAnalytics`)

> ⚠️ You'll need this path later too.

---

### STEP 3: Go to GitHub to create a runner

1. Open your browser
2. Go to: `https://github.com/RamThakkar/phi-usage-analytics`
3. Click **Settings** (top menu, far right — you need to be the repo owner)
4. In left sidebar, click **Actions** → then click **Runners**
5. Click the green button **"New self-hosted runner"**

---

### STEP 4: On the GitHub page, select:

- **Runner image:** Windows
- **Architecture:** x64

GitHub will now show you a set of commands. **Keep this page open** — you'll copy from it.

---

### STEP 5: On your server, open PowerShell as Administrator

Right-click PowerShell → **Run as Administrator**

---

### STEP 6: Create a folder for the runner

```powershell
mkdir C:\github-runner
cd C:\github-runner
```

---

### STEP 7: Download the runner

Copy the download command from the GitHub page. It looks like:

```powershell
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v2.XXX.X/actions-runner-win-x64-2.XXX.X.zip -OutFile actions-runner-win-x64.zip
```

> ⚠️ Use the EXACT URL from your GitHub page (the version number may differ).

---

### STEP 8: Extract the runner

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-x64.zip", "$PWD")
```

---

### STEP 9: Configure the runner

Copy the config command from the GitHub page. It looks like:

```powershell
.\config.cmd --url https://github.com/RamThakkar/phi-usage-analytics --token ABCDEFGH123456789
```

> ⚠️ Use the EXACT token from your GitHub page (it expires in a few hours).

It will ask you questions. **Just press Enter for all of them** (accept defaults):

```
Enter the name of the runner group: [press Enter]
Enter the name of runner: [press Enter]  
Enter any additional labels: [press Enter]
Enter name of work folder: [press Enter]
```

You should see: `√ Runner successfully added`

---

### STEP 10: Install as a Windows Service

```powershell
.\svc.cmd install
```

It will ask: **Enter the name of the user to run the service** — just press Enter (uses the default system account).

Then start it:

```powershell
.\svc.cmd start
```

You should see: `√ Service started successfully`

---

### STEP 11: Verify on GitHub

1. Go back to the GitHub page: Settings → Actions → Runners
2. You should see your server listed with a **green dot** and "Idle" status
3. If you see a green dot — **you're done with setup!**

---

### STEP 12: Update the workflow file (if needed)

If your App Pool name or deploy path is different from the defaults, edit:  
`.github/workflows/deploy-self-hosted.yml`

Find and update these values:

```yaml
$poolName = "PhiUsageAnalytics"          ← Change to YOUR App Pool name
$dest = "C:\inetpub\wwwroot\PhiUsageAnalytics"  ← Change to YOUR deploy path
```

Commit and push this change.

---

## How to Deploy (after setup)

From now on, every time you want to deploy:

1. **Push your code** to a feature branch (e.g., `feature/new-thing`)
2. **Create a Pull Request** to `main`
3. **Merge the PR**
4. **Wait ~2 minutes** — check the **Actions** tab on GitHub to see progress
5. **Done!** Your site is updated.

---

## What Happens During Deployment

```
You merge PR to main
       ↓
GitHub tells your server "new code available"
       ↓
Server pulls the code
       ↓
Server runs: dotnet publish (builds the app)
       ↓
Server runs: Stop-WebAppPool "PhiUsageAnalytics"
       (ONLY your app stops, other apps NOT affected)
       ↓
Server runs: robocopy (copies new files, SKIPS appsettings.json)
       ↓
Server runs: Start-WebAppPool "PhiUsageAnalytics"
       (Your app starts again with new code)
       ↓
✅ Done! Site live with new code. Config untouched.
```

---

## Files That Are NEVER Overwritten

These files/folders on your server are always preserved:

- `appsettings.json` — your production database/Redis config
- `appsettings.Development.json` — if exists
- `appsettings.Production.json` — if exists
- `logs/` folder
- `errors/` folder
- `visits/` folder

---

## Troubleshooting

### "Runner is Offline" on GitHub

→ On your server, open Services (`services.msc`)  
→ Find "GitHub Actions Runner"  
→ Make sure it's **Running** and set to **Automatic** startup

### Deployment failed

→ Go to GitHub → **Actions** tab → click the failed run → read the error log

### Need to change appsettings.json

→ RDP into server → edit the file manually → restart App Pool from IIS Manager

### Need to manually deploy (emergency)

On the server:
```powershell
cd C:\path\to\your\repo
git pull origin main
dotnet publish src/PhiUsageAnalytics.Api/PhiUsageAnalytics.Api.csproj -c Release -o C:\inetpub\wwwroot\PhiUsageAnalytics
# Then restart app pool from IIS Manager
```

---

## Summary

| What | Details |
|------|---------|
| Trigger | Push/merge to `main` branch |
| Build | .NET 6, Release mode |
| Deploy method | robocopy (copies files, skips config) |
| What stops | Only your App Pool (not IIS, not other apps) |
| Config preserved | ✅ appsettings.json never overwritten |
| Downtime | ~5 seconds (App Pool stop → start) |
| Rollback | Revert the merge on GitHub, it auto-deploys the previous version |
