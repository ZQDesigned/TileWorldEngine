using TileWorld.Engine.Hosting.MonoGame;
using TileWorld.Testing.Desktop;

MonoGameEngineHost.Run(
    new SmokeTestEngineApplication(),
    new MonoGameHostOptions
    {
        WindowTitle = "TileWorld Smoke Test",
        PreferredBackBufferWidth = 1280,
        PreferredBackBufferHeight = 720,
        IsMouseVisible = true,
        AllowUserResizing = true
    });
