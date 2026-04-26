namespace TileWorld.Engine.Hosting;

/// <summary>
/// Exposes host-owned services to engine applications that need limited access to lifecycle or platform facilities.
/// </summary>
public interface IEngineHostServices
{
    /// <summary>
    /// Gets the host-managed texture registration service.
    /// </summary>
    ITextureBitmapRegistry Textures { get; }

    /// <summary>
    /// Gets the host-managed text input service.
    /// </summary>
    ITextInputService TextInput { get; }

    /// <summary>
    /// Requests that the active host begin shutting the application down.
    /// </summary>
    void RequestExit();
}
