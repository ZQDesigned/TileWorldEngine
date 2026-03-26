# Getting Started

This repository currently ships with a desktop smoke-test application and a generated API documentation site.

## Build The Solution

```powershell
dotnet build TileWorldEngine.sln
```

## Run Automated Tests

```powershell
dotnet test TileWorldEngine.sln --no-build
```

## Run The Desktop Smoke Test

```powershell
dotnet run --project TileWorld.Testing.Desktop
```

## Smoke-Test Controls

- Left mouse: place the currently selected tile
- Right mouse: break the hovered tile
- `1 / 2 / 3`: switch the active tile
- `F1`: toggle the debug overlay
- `F5`: save the world immediately
- `WASD` or arrow keys: move the camera
- `Shift`: speed up camera movement

## Build The Docs Locally

```powershell
dotnet tool restore
dotnet tool run docfx docs/docfx.json
```

DocFX will write the generated site to:

```text
docs/_site
```

You can use that folder for local inspection or let the GitHub Actions workflow publish it to GitHub Pages.
