# Phibonacci Usage Analytics — Features Guide

> **For:** CxO Team / Client Presentations  
> **Platform:** https://usage.phibonaccisolutions.com

---

## Dashboard Overview

When you login, the dashboard shows a complete picture of how an organization's panels are being used — from high-level summary down to individual topic engagement.

---

## 📊 Summary Cards (Top Row)

| Card | What it means |
|------|---------------|
| **Total Licenses** | Number of active panel installations (Windows + Android) |
| **Total Sessions** | How many times content was opened across all licenses |
| **Total Time** | Combined hours of Video + Simulation usage |
| **Video Time** | Time spent watching educational videos |
| **Simulation Time** | Time spent using interactive simulations |

---

## 🍩 Content Type Distribution (Donut Chart)

Shows the **percentage split** between Video and Simulation usage.

**How to read it:**
- Purple = Video percentage
- Orange = Simulation percentage
- Helps answer: "Are students watching videos more or using simulations more?"

---

## 📈 Daily Usage Trend (Line Chart)

Shows daily usage over time — one line for Video (purple), one for Simulation (orange).

**How to read it:**
- Peaks = days with high usage (school days)
- Dips = weekends/holidays
- Helps answer: "Is usage consistent or declining?"

---

## 📊 Comparative Analysis (Last 30 Days vs Previous 30 Days)

Compares the **current 30-day period** against the **previous 30-day period**.

| Metric | Current | Change |
|--------|---------|--------|
| Sessions | 1,250 | ↑ 23.5% |
| Total Time | 142h | ↑ 15.2% |
| Video | 89h | ↑ 18.0% |
| Simulation | 53h | ↓ 5.3% |

**How to read it:**
- **Green ↑** = Usage increased compared to previous month (good!)
- **Red ↓** = Usage decreased (needs attention)
- **— 0%** = No change
- If no data exists for this period, it shows "No usage data available"

**Use case:** Shows the management whether content adoption is growing or declining.

---

## 🏆 Top Grades by Usage

Shows which **grades** (classes) use the panels the most.

```
#1  Grade 10   ████████████████  296h · 2805 sessions
#2  Grade 8    ████████████       180h · 1500 sessions
#3  Grade 6    ████████           120h · 980 sessions
```

**How to read it:**
- Longer bar = more usage
- Helps answer: "Which grades are most engaged with the content?"

**Note:** If licenses have expired and no recent data exists, it shows "No usage data available."

---

## 🔥 Most Popular Topics (Top 10)

Shows the **most-watched/used topics** across ALL licenses in the organization.

```
#1  Introduction to Metals                      102h 28m
    Grade 10 → Science → Chemistry              592 sessions

#2  Magnetic Effects of Current                  44h 17m
    Grade 10 → Science → Magnetic Effects        528 sessions
```

**How to read it:**
- Ranked by total usage time (highest first)
- Shows the full path: Grade → Subject → Chapter
- Helps answer: "What content are students spending the most time on?"

**Use case:** Identifies the most valuable content that drives engagement.

---

## ⚡ Least Engaged Topics (Opened but Skipped)

Shows topics that students **open but close within 60 seconds** — consistently across 3+ attempts.

```
#1  Female Reproductive System                   avg 18s
    Grade 10 → Science → How do Organisms...     opened 5x

#2  Electric Cell and Bulb                       avg 18s
    Grade 7 → Science → Electricity              opened 6x
```

**How to read it:**
- "avg 18s" = On average, students close this topic after just 18 seconds
- "opened 5x" = It was attempted 5 times (not an accident — pattern of disengagement)
- Helps answer: "Which content needs improvement or isn't resonating with students?"

**Use case:** Identifies content that may need to be redesigned, simplified, or replaced. If students consistently skip a topic, the content might be:
- Too difficult to understand
- Not visually engaging
- Loading too slowly
- Incorrectly categorized

---

## ⚠️ Inactive Licenses (No Usage in Last 7 Days)

Shows licenses (panels) that haven't recorded any usage recently.

```
🤖 03428069155700  (Ds Joshi)       10 days idle
🪟 30184155058546  (Class 10A)      18 days idle
```

**How to read it:**
- 🤖 = Android panel, 🪟 = Windows panel
- Consumer name shown (who activated it)
- "10 days idle" = No usage for 10 days

**Use case:** Helps identify:
- Panels that might be broken/offline
- Schools where teachers aren't using the product
- Follow-up opportunities for the sales/support team

---

## 🕐 Usage Heatmap (Hour × Day of Week)

Shows **when** panels are being used — by hour and day.

```
         0  3  6  9  12 15 18 21
Sun      ░░░░░░░░░░░░░░░░░░░░░░░░
Mon      ░░░░░░░░░▓▓▓▓▓▓░░░░░░░░░
Tue      ░░░░░░░░░▓▓▓▓▓░░░░░░░░░░
Wed      ░░░░░░░░░▓▓▓▓▓▓▓░░░░░░░░
Thu      ░░░░░░░░░▓▓▓▓▓▓░░░░░░░░░
Fri      ░░░░░░░░░▓▓▓▓░░░░░░░░░░░
Sat      ░░░░░░░░░░░░░░░░░░░░░░░░
```

**How to read it:**
- Darker cells = more usage at that time
- Time is in **IST (Indian Standard Time)**
- Typical school pattern: heavy usage 9 AM - 2 PM, Monday-Friday

**Period selector:** Switch between 7 days, 30 days, or 90 days view.

**Use case:** Helps answer:
- "Are panels being used during school hours?" (expected)
- "Is there after-school usage?" (bonus engagement)
- "Which days have the most activity?" (helps plan)

---

## 📋 License-wise Usage Table

The main table showing all active licenses with:

| Column | Meaning |
|--------|---------|
| Platform icon | 🪟 Windows or 🤖 Android |
| License Key | Unique panel identifier |
| Consumer | Who activated it (teacher/school name) |
| Sessions | Total content views |
| Total Time | Total hours used |
| Video | Hours of video watched |
| Simulation | Hours of interactive content used |
| Grades | Number of different grades accessed |
| Last Usage | When it was last used |
| Action | View Details (drill-down) or Download CSV |

**Features:**
- 🔍 Search by license key
- 📊 Sort by any column (click header arrows)
- 🏷️ Filter: All / With Usage / Without Usage
- 📄 Pagination: First/Back/Next/Last
- 📥 Download CSV report per license

---

## 🔽 Drill-Down (Click "View Details")

Each license can be explored deeper:

```
License → Grades (with charts)
           → Subjects (with charts)
             → Chapters (with charts)
               → Topics (Video ✅/❌ + Simulation ✅/❌)
```

**At each level you see:**
- Total sessions, video time, simulation time, last usage
- Horizontal bar charts showing relative usage
- At topic level: whether Video was watched ✅ and Simulation was used ✅

---

## 📅 Date Filter

Apply a custom date range to view data for a specific period:
- Select From/To dates → click **Apply** (indigo button)
- To clear: click **Clear** (red button) — resets to all-time view

---

## 🔄 Data Freshness

- Data refreshes daily at **5:00 AM IST**
- During the day, all data is served from cache (instant loading)
- If you need immediate refresh: contact admin to clear cache

---

## Key Takeaways for CxO:

1. **Adoption tracking** — See exactly which schools/licenses are actively using the product
2. **Content quality** — Identify most popular AND least engaging content
3. **Growth measurement** — Month-over-month comparison shows trajectory
4. **Idle detection** — Spot inactive licenses that need follow-up
5. **Usage patterns** — Heatmap confirms panels are used during school hours
6. **Deep analysis** — Drill from org-level down to individual topic level

---

*Document prepared for Phibonacci Analytics Dashboard — August 2026*
