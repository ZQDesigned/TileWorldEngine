using System;
using TileWorld.Engine.Input;
using TileWorld.Engine.Render;

namespace TileWorld.Engine.Hosting;

/// <summary>
/// Hosts a single active engine scene and supports explicit scene switching.
/// </summary>
public class SceneHostApplication : IEngineApplication, IHostedEngineApplication
{
    private static readonly IEngineHostServices NullHostServices = new NullEngineHostServices();
    private IEngineScene _activeScene;
    private bool _isInitialized;

    /// <summary>
    /// Creates a scene-hosted application with an initial active scene.
    /// </summary>
    /// <param name="initialScene">The initial scene to activate during initialization.</param>
    public SceneHostApplication(IEngineScene initialScene)
    {
        _activeScene = initialScene ?? throw new ArgumentNullException(nameof(initialScene));
        HostServices = NullHostServices;
    }

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public IEngineScene ActiveScene => _activeScene;

    /// <summary>
    /// Gets the host services injected by the active runtime backend.
    /// </summary>
    public IEngineHostServices HostServices { get; private set; }

    /// <inheritdoc />
    public void SetHostServices(IEngineHostServices hostServices)
    {
        HostServices = hostServices ?? throw new ArgumentNullException(nameof(hostServices));
    }

    /// <inheritdoc />
    public virtual void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _activeScene.OnEnter(this);
    }

    /// <inheritdoc />
    public virtual void Update(FrameTime frameTime, FrameInput input)
    {
        if (!_isInitialized)
        {
            return;
        }

        _activeScene.Update(frameTime, input ?? FrameInput.Empty);
    }

    /// <inheritdoc />
    public virtual void Render(IRenderContext renderContext)
    {
        if (!_isInitialized)
        {
            return;
        }

        _activeScene.Render(renderContext);
    }

    /// <inheritdoc />
    public virtual void Shutdown()
    {
        if (!_isInitialized)
        {
            return;
        }

        _activeScene.OnExit();
        _isInitialized = false;
    }

    /// <summary>
    /// Replaces the current active scene with a new scene.
    /// </summary>
    /// <param name="nextScene">The scene that should become active.</param>
    public void SwitchScene(IEngineScene nextScene)
    {
        ArgumentNullException.ThrowIfNull(nextScene);

        if (ReferenceEquals(_activeScene, nextScene))
        {
            return;
        }

        if (_isInitialized)
        {
            _activeScene.OnExit();
        }

        _activeScene = nextScene;

        if (_isInitialized)
        {
            _activeScene.OnEnter(this);
        }
    }

    private sealed class NullEngineHostServices : IEngineHostServices
    {
        public ITextureBitmapRegistry Textures { get; } = new NullTextureBitmapRegistry();

        public ITextInputService TextInput { get; } = new NullTextInputService();

        public void RequestExit()
        {
        }
    }

    private sealed class NullTextInputService : ITextInputService
    {
        public bool IsRequestActive => false;

        public string CurrentText => string.Empty;

        public System.Threading.Tasks.Task<string> RequestTextAsync(TextInputRequest request, System.Threading.CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return System.Threading.Tasks.Task.FromResult<string>(null);
        }
    }

    private sealed class NullTextureBitmapRegistry : ITextureBitmapRegistry
    {
        public bool HasTexture(string textureKey)
        {
            ArgumentNullException.ThrowIfNull(textureKey);
            return false;
        }

        public void RegisterTextureBitmap(string textureKey, Render.TextureBitmapRgba32 bitmap)
        {
            ArgumentNullException.ThrowIfNull(textureKey);
            ArgumentNullException.ThrowIfNull(bitmap);
        }
    }
}
