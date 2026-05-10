# ScottPlotQuickStartApp — Implementation Plan

**Date:** 2026-05-04  
**Status:** FINAL — ready to build  

---

## Goals

A minimal, working example of ScottPlot embedded in an Avalonia 12 desktop + WASM app.
Matches the official quickstart at https://scottplot.net/quickstart/avalonia/ — a single scatter
plot (`double[] dataX / dataY`) rendered in an `AvaPlot` control.  
The app shell reuses the AppTemplate pattern (NavigationView, theme switching, settings, NLog).

---

## Stack

| Component             | Version            | Notes                                              |
|-----------------------|--------------------|----------------------------------------------------|
| .NET                  | 10                 | `net10.0` / `net10.0-browser`                      |
| C#                    | 13                 | `<LangVersion>latest</LangVersion>`                |
| Avalonia              | 12.0.0 (pinned)    | FA 3.0 incompatible with 12.0.2                    |
| FluentAvaloniaUI      | 3.0.0-preview2     | Only FA version targeting Avalonia 12.0.0          |
| FluentIcons.Avalonia  | 2.0.325            |                                                    |
| CommunityToolkit.Mvvm | 8.4.0              |                                                    |
| ScottPlot.Avalonia    | 5.1.57 (tentative) | Compatibility must be verified — see Step 2        |
| SkiaSharp             | 3.119.x            | Shared by Avalonia 12 and ScottPlot 5.1            |
| NLog                  | 6.1.3              | File target only                                   |

---

## Project Identity

| Item            | Value                   |
|-----------------|-------------------------|
| Solution name   | `ScottPlotQuickStartApp`|
| Project folder  | `ScottPlotQuickStartApp`|
| Assembly name   | `ScottPlotQuickStartApp`|
| Root namespace  | `ScottPlotQuickStartApp`|

---

## What the Quickstart Page Shows

From https://scottplot.net/quickstart/avalonia/ — this is the **entire feature scope**:

### XAML namespace
```xml
xmlns:ScottPlot="clr-namespace:ScottPlot.Avalonia;assembly=ScottPlot.Avalonia"
```

### Control placement
```xml
<ScottPlot:AvaPlot Name="AvaPlot1"/>
```

### Code-behind / ViewModel wiring
```csharp
double[] dataX = { 1, 2, 3, 4, 5 };
double[] dataY = { 1, 4, 9, 16, 25 };

AvaPlot avaPlot1 = this.Find<AvaPlot>("AvaPlot1");
avaPlot1.Plot.Add.Scatter(dataX, dataY);
avaPlot1.Refresh();
```

The goal is this — nothing more, nothing less — running on both Desktop and WASM.

---

## Compatibility Risk & Resolution Strategy

ScottPlot.Avalonia 5.1.57 targets Avalonia 11.x. Avalonia 12 has breaking changes.
The SkiaSharp 3.119 alignment is our best hope (both require it).

### Resolution path (in order — stop at first success):

**Attempt 1 — NuGet as-is**
Add `ScottPlot.Avalonia` 5.1.57 via NuGet. Build. Check for:
- Assembly version conflicts on Avalonia packages
- SkiaSharp version mismatch
- Missing `UseHarfBuzz()` / startup crash
- Any `AvaPlot` namespace/type resolution failures

If it builds and renders: done.

**Attempt 2 — Build ScottPlot from source**
```
git clone https://github.com/ScottPlot/ScottPlot
```
Open `src/ScottPlot.Avalonia/ScottPlot.Avalonia.csproj`.
Change all Avalonia package refs from `11.x` → `12.0.0`.
Verify SkiaSharp is already on 3.119.x (it should be in 5.1.x).
Build with `dotnet build`. Fix any Avalonia 12 API breakages:
- Check for removed Window Decoration APIs
- Check for removed `Avalonia.Browser.Blazor` references
- Check `IBrush`, `IPen`, `SKCanvas` interop — these are the most common breakage points
Reference the built output via `<ProjectReference>` in ScottPlotQuickStartApp.

**Attempt 3 — Targeted patch fork**
If source build has multiple failures, create a minimal fork:
- Copy only `ScottPlot.Avalonia` project (and its ScottPlot.Core dependency)
- Strip all non-Avalonia platform projects to reduce noise
- Apply fixes iteratively, guided by build output
- Local NuGet pack → reference via local feed

---

## App Shell (from AppTemplate pattern)

| Feature                        | Included |
|-------------------------------|----------|
| NavigationView (collapsible)  | Yes      |
| Light / Dark / Auto theme     | Yes      |
| FluentAvaloniaTheme            | Yes      |
| Settings persistence (IsolatedStorage + STJ) | Yes |
| Window state persistence (Desktop) | Yes |
| Global exception handler      | Yes      |
| NLog file logging             | Yes      |
| MVVM (CommunityToolkit)       | Yes      |
| DI (Microsoft.Extensions.DI)  | Yes      |

Navigation pages:
- **Home** — app description, link to ScottPlot docs
- **Quickstart Plot** — the AvaPlot scatter chart

---

## MVVM Design for the Plot Page

Avoid code-behind for `AvaPlot` wiring where possible. Preferred pattern:

```csharp
// QuickstartViewModel.cs
public partial class QuickstartViewModel : ObservableObject
{
    public double[] DataX { get; } = { 1, 2, 3, 4, 5 };
    public double[] DataY { get; } = { 1, 4, 9, 16, 25 };
}
```

```csharp
// QuickstartView.xaml.cs — minimal code-behind for AvaPlot wiring
// AvaPlot does not support full data binding; Refresh() must be called imperatively.
protected override void OnDataContextChanged(EventArgs e)
{
    base.OnDataContextChanged(e);
    if (DataContext is QuickstartViewModel vm)
    {
        AvaPlot1.Plot.Clear();
        AvaPlot1.Plot.Add.Scatter(vm.DataX, vm.DataY);
        AvaPlot1.Refresh();
    }
}
```

This is the minimum acceptable code-behind — all data/logic lives in the ViewModel.

---

## Theme Integration

ScottPlot has built-in light/dark plot styles. Hook them to the app theme:

```csharp
// Call whenever FluentAvaloniaTheme changes
private void ApplyPlotTheme()
{
    bool isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
    AvaPlot1.Plot.Style.Background(
        figure: isDark ? ScottPlot.Color.FromHex("#1e1e1e") : ScottPlot.Color.FromHex("#ffffff"),
        data:   isDark ? ScottPlot.Color.FromHex("#2d2d2d") : ScottPlot.Color.FromHex("#f8f8f8")
    );
    AvaPlot1.Refresh();
}
```

---

## WASM Considerations

WASM is a hard requirement. Known risks:

| Risk | Mitigation |
|------|-----------|
| ScottPlot Skia rendering in browser | SkiaSharp WASM is supported; test at Step 5 |
| `Avalonia.Browser.Blazor` removed in v12 | Use `Avalonia.Browser` (non-Blazor) — the standard Avalonia WASM path |
| Threading — `AvaPlot.Refresh()` on wrong thread | Wrap in `Dispatcher.UIThread.InvokeAsync()` |
| File size — SkiaSharp adds significant WASM payload | Acceptable for a template/demo |
| No `IsolatedStorage` in browser | `AppSettings` must gracefully degrade (already handled in AppTemplate pattern) |

WASM target: `net10.0-browser`. Browser entry point: standard Avalonia browser host.
Test locally with `dotnet run --project ScottPlotQuickStartApp.Browser`.

---

## Implementation Steps

```
Step 1  — Scaffold project
          dotnet new avalonia.app -n ScottPlotQuickStartApp
          Add FluentAvalonia, FluentIcons, CommunityToolkit.Mvvm, NLog
          Set up AppBuilder with UseHarfBuzz(), DI, NLog
          Port AppTemplate shell: NavigationView, theme, settings, window state, global ex handler

Step 2  — ScottPlot compatibility spike (BLOCKER — must pass before Step 3)
          Add ScottPlot.Avalonia 5.1.57 NuGet
          Place <ScottPlot:AvaPlot> on a blank page
          dotnet build → document every error
          If clean build: proceed to Step 3
          If errors: execute Attempt 2 (source build) from the strategy above

Step 3  — Quickstart plot page
          QuickstartView.axaml + QuickstartViewModel.cs
          Scatter plot with the 5-point dataset from the official quickstart
          AvaPlot wired in minimal code-behind (OnDataContextChanged)

Step 4  — Theme bridge
          Subscribe to ActualThemeVariantChanged
          Call ApplyPlotTheme() on change and on initial load

Step 5  — WASM target
          Add ScottPlotQuickStartApp.Browser project
          dotnet run → test in browser
          Fix any threading / Skia / IsolatedStorage issues

Step 6  — Polish & verification
          Test Desktop: Light theme, Dark theme, window resize, window state restore
          Test WASM: plot renders, pan/zoom work, no console errors
          Verify NLog writes to file on Desktop
          Verify settings persist across restarts (Desktop)
```

---

## Files to Create

```
ScottPlotQuickStartApp/
├── ScottPlotQuickStartApp.sln
├── ScottPlotQuickStartApp/
│   ├── ScottPlotQuickStartApp.csproj
│   ├── App.axaml + App.axaml.cs
│   ├── Program.cs                        (AppBuilder + UseHarfBuzz + DI)
│   ├── Views/
│   │   ├── MainWindow.axaml              (NavigationView shell)
│   │   ├── MainWindow.axaml.cs
│   │   ├── HomePage.axaml
│   │   ├── HomePage.axaml.cs
│   │   ├── QuickstartView.axaml          (AvaPlot page)
│   │   └── QuickstartView.axaml.cs
│   ├── ViewModels/
│   │   ├── MainWindowViewModel.cs
│   │   ├── HomeViewModel.cs
│   │   └── QuickstartViewModel.cs
│   ├── Services/
│   │   ├── AppSettings.cs
│   │   ├── SettingsService.cs
│   │   └── NavigationService.cs
│   └── Assets/
│       └── avalonia-logo.ico
└── ScottPlotQuickStartApp.Browser/
    ├── ScottPlotQuickStartApp.Browser.csproj
    └── Program.cs
```

If ScottPlot must be built from source, add:
```
ScottPlot/                                (cloned repo, src/ScottPlot.Avalonia referenced)
```

---

## What Is Explicitly Out of Scope

- Live/streaming data
- Multiple chart types (Bar, OHLC, Heatmap, etc.)
- CSV import
- PNG/SVG export
- Clipboard copy
- Custom color palette
- Unit tests

These can all be layered on top of this template in a follow-on project.

---

## ScottPlot Reference Links

- Quickstart: https://scottplot.net/quickstart/avalonia/
- GitHub: https://github.com/ScottPlot/ScottPlot
- Demo gallery: https://scottplot.net/demo/
- NuGet: https://www.nuget.org/packages/ScottPlot.Avalonia
- Avalonia 12 breaking changes: https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
