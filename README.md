# ScottPlotQuickStartApp

A minimal working example of [ScottPlot 5](https://scottplot.net) embedded in an [Avalonia 12](https://avaloniaui.net) application, targeting both **Desktop** (Windows/Linux/macOS) and **Browser (WASM)**.

This project exists to document the compatibility fixes required to make ScottPlot work with Avalonia 12, and serves as a verified starting point for future Avalonia data plotting applications.

**Author:** Matthew Wormington  
**Version:** 1.0.0 — 4 May 2026  
**License:** MIT — see [LICENSE](LICENSE)

---

## AI Disclosure

This project was developed using **vibe coding** with [Claude Code](https://claude.ai/claude-code) (Anthropic, claude-sonnet-4-6). AI assistance was used throughout: project architecture, all source code, compatibility research and debugging, and this documentation.

All code was verified to build without errors and confirmed to render correctly at runtime on both Desktop and Browser (WASM) targets by the author. The author takes full responsibility for the published content.

> This disclosure follows the transparency recommendations of the Nature Methods editorial *"Using AI responsibly in scientific publishing"* (Nature Methods 23, 271, 2026; https://doi.org/10.1038/s41592-026-03020-1), which requires disclosure of AI tools used, the model/version, the scope of use, and confirmation that all AI-assisted content has been validated by the author.

---

## Stack

| Component | Version |
|---|---|
| .NET | 10 |
| C# | 13 (`latest`) |
| Avalonia | 12.0.2 |
| Avalonia.Themes.Fluent | 12.0.2 |
| CommunityToolkit.Mvvm | 8.4.0 |
| ScottPlot.Avalonia | Built from source (see below) |

---

## Project Structure

```
ScottPlotQuickStartApp/
├── ScottPlotQuickStartApp.slnx
├── Directory.Packages.props               # Central NuGet version management
├── ScottPlotQuickStartApp/                # Shared library (UI + ViewModels)
│   ├── Views/
│   │   ├── MainWindow.axaml               # Desktop window wrapper
│   │   ├── MainView.axaml                 # AvaPlot scatter chart
│   │   └── MainView.axaml.cs             # Plot wiring (OnLoaded code-behind)
│   ├── ViewModels/
│   │   └── MainViewModel.cs
│   └── App.axaml / App.axaml.cs
├── ScottPlotQuickStartApp.Desktop/        # Desktop runner (net10.0)
│   └── Program.cs                         # AppBuilder with UseHarfBuzz()
└── ScottPlotQuickStartApp.Browser/        # WASM runner (net10.0-browser)
    └── Program.cs
```

The solution also requires the patched ScottPlot source (see below) to be present at `../ScottPlot` relative to this solution.

---

## Running the App

**Desktop:**
```
dotnet run --project ScottPlotQuickStartApp.Desktop
```

**Browser (WASM):**
```
dotnet run --project ScottPlotQuickStartApp.Browser
```
Then open `http://localhost:5235/` in a browser. The first run takes longer due to the Emscripten WASM link step.

---

## The Plot

Matches the official ScottPlot Avalonia quickstart exactly:

```csharp
double[] dataX = { 1, 2, 3, 4, 5 };
double[] dataY = { 1, 4, 9, 16, 25 };

AvaPlot1.Plot.Add.Scatter(dataX, dataY);
AvaPlot1.Refresh();
```

The `AvaPlot` control is declared in XAML:

```xml
xmlns:ScottPlot="clr-namespace:ScottPlot.Avalonia;assembly=ScottPlot.Avalonia"
...
<ScottPlot:AvaPlot Name="AvaPlot1" />
```

And wired up in the `Loaded` event in `MainView.axaml.cs`. Pan and zoom are built into `AvaPlot` with no additional code.

---

## Critical Finding: ScottPlot NuGet Does Not Work with Avalonia 12

> **Do not use the `ScottPlot.Avalonia` NuGet package with Avalonia 12.**

The package (any version up to at least 5.1.59 as of May 2026) compiles cleanly but produces a **blank/transparent render** at runtime. There are two root causes:

### Bug 1 — Type identity mismatch (blank display)

`AvaPlot.cs` uses Avalonia's Skia rendering lease to get a `SKCanvas`:

```csharp
var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
if (leaseFeature is null) return;  // <-- always returns here with Avalonia 12
```

`TryGetFeature<T>()` does a type identity check. The NuGet package was compiled against Avalonia 11's assembly, so its reference to `ISkiaSharpApiLeaseFeature` is the Avalonia 11 type. At runtime, the Avalonia 12 host provides the Avalonia 12 version of that type — different assembly identity → `TryGetFeature<>` returns `null` → render method exits immediately → blank display.

**Fix:** Compile `ScottPlot.Avalonia` from source against Avalonia 12 so both sides share the same type identity.

### Bug 2 — OnLostFocus signature change

In Avalonia 12, `InputElement.OnLostFocus` changed its parameter type:

```csharp
// Avalonia 11 — in ScottPlot source
protected override void OnLostFocus(RoutedEventArgs e)

// Avalonia 12 — required
protected override void OnLostFocus(FocusChangedEventArgs e)
```

**Fix:** Update the method signature in `AvaPlot.cs`.

Both bugs are tracked in [ScottPlot issue #5228](https://github.com/ScottPlot/ScottPlot/issues/5228).

---

## How to Apply the ScottPlot Patch

### 1. Clone ScottPlot source

```
git clone --depth 1 https://github.com/ScottPlot/ScottPlot.git
```

Place the clone so that `ScottPlot/` is a sibling of `ScottPlotQuickStartApp/`.

### 2. Edit the csproj

File: `ScottPlot/src/ScottPlot5/ScottPlot5 Controls/ScottPlot.Avalonia/ScottPlot.Avalonia.csproj`

```xml
<!-- Before -->
<TargetFrameworks>net462;net8.0;net10.0</TargetFrameworks>
<SignAssembly>True</SignAssembly>
<AssemblyOriginatorKeyFile>../../Key.snk</AssemblyOriginatorKeyFile>
...
<PackageReference Include="Avalonia" Version="11.3.4" />
<PackageReference Include="Avalonia.Skia" Version="11.3.4" />

<!-- After -->
<TargetFramework>net10.0</TargetFramework>
...
<PackageReference Include="Avalonia" Version="12.0.2" />
<PackageReference Include="Avalonia.Skia" Version="12.0.2" />
```

### 3. Fix AvaPlot.cs

File: `ScottPlot/src/ScottPlot5/ScottPlot5 Controls/ScottPlot.Avalonia/AvaPlot.cs`

```csharp
// Before
protected override void OnLostFocus(RoutedEventArgs e)

// After
protected override void OnLostFocus(FocusChangedEventArgs e)
```

(`FocusChangedEventArgs` is in the `Avalonia.Input` namespace, which is already imported.)

### 4. Reference via ProjectReference

In `ScottPlotQuickStartApp.csproj`, reference the patched source directly instead of NuGet:

```xml
<ProjectReference Include="..\..\ScottPlot\src\ScottPlot5\ScottPlot5 Controls\ScottPlot.Avalonia\ScottPlot.Avalonia.csproj" />
```

---

## AppBuilder Requirements (Avalonia 12)

Avalonia 12 requires `.UseHarfBuzz()` for text shaping. Without it an `InvalidOperationException` is thrown at startup. Add it to the Desktop `AppBuilder`:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseHarfBuzz()   // required in Avalonia 12
    .WithInterFont()
    .LogToTrace();
```

---

## Reference Links

- ScottPlot Avalonia quickstart: https://scottplot.net/quickstart/avalonia/
- ScottPlot GitHub: https://github.com/ScottPlot/ScottPlot
- ScottPlot issue #5228 (Avalonia 12 blank display): https://github.com/ScottPlot/ScottPlot/issues/5228
- Avalonia 12 breaking changes: https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- AI disclosure reference: https://doi.org/10.1038/s41592-026-03020-1
