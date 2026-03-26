using TileWorld.Engine.Hosting.MonoGame;
using TileWorld.Testing.Desktop;

MonoGameEngineHost.Run(
    new DesktopSandboxShellApplication(),
    new MonoGameHostOptions
    {
        WindowTitle = "TileWorld Desktop Sandbox",
        PreferredBackBufferWidth = 1280,
        PreferredBackBufferHeight = 720,
        IsMouseVisible = true,
        AllowUserResizing = true
    });
