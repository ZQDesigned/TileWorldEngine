namespace TileWorld.Engine.Tests.Storage;

internal sealed class TestDirectoryScope : IDisposable
{
    public TestDirectoryScope()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "TileWorldEngineTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
