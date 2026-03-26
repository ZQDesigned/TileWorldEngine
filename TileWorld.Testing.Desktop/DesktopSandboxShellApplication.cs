using TileWorld.Engine.Hosting;

namespace TileWorld.Testing.Desktop;

internal sealed class DesktopSandboxShellApplication : SceneHostApplication
{
    public DesktopSandboxShellApplication()
        : base(new WorldSelectScene(static worldPath => new SandboxWorldScene(worldPath)))
    {
    }
}
