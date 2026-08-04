# Default Window Height Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show a complete monitor-card row at the default window size.

**Architecture:** Change only the WPF window height. Preserve cards, scrolling, and resizing behavior.

**Tech Stack:** .NET 10, WPF

## Global Constraints

- Set default height to exactly `840` DIPs.
- Keep `MinHeight="560"` and vertical scrolling unchanged.
- Leave implementation uncommitted and unpushed for manual testing.

---

### Task 1: Increase default height

**Files:**
- Modify: `MainWindow.xaml:11`

**Interfaces:**
- Produces: main window default `Height="840"`

- [ ] **Step 1: Change the XAML value**

```xml
Height="840"
```

- [ ] **Step 2: Build**

Run: `dotnet build Wallppr.csproj -c Release --no-restore`

Expected: zero warnings and zero errors.
