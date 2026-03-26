using TileWorld.Engine.Hosting;

namespace TileWorld.Testing.Desktop;

internal sealed class DesktopSandboxShellApplication : SceneHostApplication
{
    public DesktopSandboxShellApplication()
        : base(CreateWorldSelectScene())
    {
    }

    private static WorldSelectScene CreateWorldSelectScene()
    {
        return new WorldSelectScene(CreateSandboxWorldScene);
    }

    private static SandboxWorldScene CreateSandboxWorldScene(string worldPath)
    {
        return new SandboxWorldScene(worldPath, CreateWorldSelectScene);
    }
}
