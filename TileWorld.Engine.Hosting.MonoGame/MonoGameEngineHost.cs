using System;

namespace TileWorld.Engine.Hosting.MonoGame;

/// <summary>
/// Runs an <see cref="IEngineApplication"/> inside the MonoGame compatibility host.
/// </summary>
public static class MonoGameEngineHost
{
    /// <summary>
    /// Starts the MonoGame-backed host loop for the supplied application.
    /// </summary>
    /// <param name="application">The engine application to run.</param>
    /// <param name="options">Optional host window and lifecycle configuration.</param>
    public static void Run(IEngineApplication application, MonoGameHostOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(application);

        using var game = new MonoGameHostGame(application, options ?? new MonoGameHostOptions());
        game.Run();
    }
}
