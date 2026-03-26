namespace TileWorld.Engine.Hosting;

/// <summary>
/// Allows a host to inject platform services into an application before initialization begins.
/// </summary>
public interface IHostedEngineApplication
{
    /// <summary>
    /// Supplies host services that remain valid for the lifetime of the application instance.
    /// </summary>
    /// <param name="hostServices">The host services exposed by the active runtime backend.</param>
    void SetHostServices(IEngineHostServices hostServices);
}
