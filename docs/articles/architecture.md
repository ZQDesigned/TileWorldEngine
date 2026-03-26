# Architecture Overview

`TileWorld.Engine` is built around a chunked tile-world model rather than a general scene-graph or object-centric runtime.

## Core Principles

- `Tile-first`: tile cells are the smallest interactive unit.
- `Chunk-first`: chunks are the smallest loading, saving, dirty-tracking, and render-caching unit.
- `Facade-first`: external gameplay code should primarily work through `WorldRuntime`.
- `Backend-decoupled`: the core engine avoids exposing MonoGame types.
- `Explicit subsystem boundaries`: runtime editing, querying, persistence, and rendering are separate systems with clear responsibilities.

## Solution Structure

- `TileWorld.Engine`
  - Core engine types, runtime facade, rendering abstractions, persistence, diagnostics, and input abstractions.
- `TileWorld.Engine.Hosting.MonoGame`
  - MonoGame compatibility host that owns the backend lifecycle and render submission.
- `TileWorld.Engine.Tests`
  - Unit and architecture guard tests.
- `TileWorld.Testing.Desktop`
  - A small `IEngineApplication` implementation used as a manual verification host.

## Stable External Surface

The main public integration points are:

- `WorldRuntime`
- `IEngineApplication`
- `IRenderContext`
- `MonoGameEngineHost`

Most lower-level runtime services are intentionally internal so gameplay code does not bind itself to temporary engine plumbing.

## Rendering Model

The engine produces backend-neutral draw commands and chunk render caches.

The compatibility host is responsible for:

- creating the platform window,
- translating input into engine input snapshots,
- mapping draw commands into backend calls.

This keeps the gameplay/runtime side portable even if the rendering backend changes later.

## Persistence Model

The current world persistence format is:

- `world.json` for world metadata
- `chunks/{x}_{y}.chk` for chunk payloads

The runtime currently supports:

- explicit manual saves,
- shutdown saves,
- periodic auto-save,
- idle-triggered auto-save after mutations.
