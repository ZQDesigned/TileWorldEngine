using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Hosting;
using TileWorld.Engine.Input;
using TileWorld.Engine.Render;

namespace TileWorld.Engine.Tests.Hosting;

public sealed class SceneHostApplicationTests
{
    [Fact]
    public void InitializeUpdateRenderAndShutdownDelegateToActiveScene()
    {
        var scene = new FakeScene();
        var application = new SceneHostApplication(scene);
        var hostServices = new FakeHostServices();
        var renderContext = new FakeRenderContext();

        application.SetHostServices(hostServices);
        application.Initialize();
        application.Update(new FrameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60.0), false), FrameInput.Empty);
        application.Render(renderContext);
        application.Shutdown();

        Assert.Same(hostServices, application.HostServices);
        Assert.Equal(1, scene.EnterCount);
        Assert.Equal(1, scene.UpdateCount);
        Assert.Equal(1, scene.RenderCount);
        Assert.Equal(1, scene.ExitCount);
        Assert.Same(application, scene.HostApplication);
    }

    [Fact]
    public void SwitchSceneExitsPreviousSceneAndEntersReplacement()
    {
        var firstScene = new FakeScene();
        var secondScene = new FakeScene();
        var application = new SceneHostApplication(firstScene);

        application.Initialize();
        application.SwitchScene(secondScene);

        Assert.Equal(1, firstScene.EnterCount);
        Assert.Equal(1, firstScene.ExitCount);
        Assert.Equal(1, secondScene.EnterCount);
        Assert.Same(secondScene, application.ActiveScene);
    }

    private sealed class FakeScene : IEngineScene
    {
        public int EnterCount { get; private set; }

        public int ExitCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int RenderCount { get; private set; }

        public SceneHostApplication HostApplication { get; private set; } = null!;

        public void OnEnter(SceneHostApplication sceneHost)
        {
            HostApplication = sceneHost;
            EnterCount++;
        }

        public void OnExit()
        {
            ExitCount++;
        }

        public void Update(FrameTime frameTime, FrameInput input)
        {
            UpdateCount++;
        }

        public void Render(IRenderContext renderContext)
        {
            RenderCount++;
        }
    }

    private sealed class FakeHostServices : IEngineHostServices
    {
        public ITextureBitmapRegistry Textures { get; } = new FakeTextureBitmapRegistry();

        public ITextInputService TextInput { get; } = new FakeTextInputService();

        public bool ExitRequested { get; private set; }

        public void RequestExit()
        {
            ExitRequested = true;
        }
    }

    private sealed class FakeTextureBitmapRegistry : ITextureBitmapRegistry
    {
        public bool HasTexture(string textureKey)
        {
            return false;
        }

        public void RegisterTextureBitmap(string textureKey, TextureBitmapRgba32 bitmap)
        {
        }
    }

    private sealed class FakeTextInputService : ITextInputService
    {
        public bool IsRequestActive => false;

        public string CurrentText => string.Empty;

        public Task<string> RequestTextAsync(TextInputRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(string.Empty);
        }
    }

    private sealed class FakeRenderContext : IRenderContext
    {
        public Int2 ViewportSizePixels => new(1280, 720);

        public void Clear(ColorRgba32 color)
        {
        }

        public void DrawSprite(SpriteDrawCommand command)
        {
        }
    }
}
