namespace TileWorld.Engine.Hosting.MonoGame;

/// <summary>
/// Configures the MonoGame compatibility host window.
/// </summary>
public sealed class MonoGameHostOptions
{
    /// <summary>
    /// Gets the window title shown by the compatibility host.
    /// </summary>
    public string WindowTitle { get; init; } = "TileWorld";

    /// <summary>
    /// Gets the preferred back-buffer width in pixels.
    /// </summary>
    public int PreferredBackBufferWidth { get; init; } = 1280;

    /// <summary>
    /// Gets the preferred back-buffer height in pixels.
    /// </summary>
    public int PreferredBackBufferHeight { get; init; } = 720;

    /// <summary>
    /// Gets a value indicating whether the host should show the mouse cursor.
    /// </summary>
    public bool IsMouseVisible { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the host window can be resized by the user.
    /// </summary>
    public bool AllowUserResizing { get; init; } = true;
}
