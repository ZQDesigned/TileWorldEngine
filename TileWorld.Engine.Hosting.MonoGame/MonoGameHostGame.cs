using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TileWorld.Engine.Core.Diagnostics;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Input;

namespace TileWorld.Engine.Hosting.MonoGame;

internal sealed class MonoGameHostGame : Game
{
    private readonly IEngineApplication _application;
    private readonly double _autoCloseAfterSeconds;
    private readonly GraphicsDeviceManager _graphics;
    private readonly MonoGameFrameInputBuilder _inputBuilder = new();
    private readonly MonoGameHostOptions _options;
    private bool _shutdownAttempted;
    private MonoGameRenderContext _renderContext = null!;
    private MonoGameTextureCatalog _textureCatalog = null!;

    public MonoGameHostGame(IEngineApplication application, MonoGameHostOptions options)
    {
        _application = application;
        _options = options;
        _autoCloseAfterSeconds = ResolveAutoCloseAfterSeconds();
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = options.PreferredBackBufferWidth,
            PreferredBackBufferHeight = options.PreferredBackBufferHeight
        };

        IsMouseVisible = options.IsMouseVisible;
        Window.AllowUserResizing = options.AllowUserResizing;
        Exiting += HandleExiting;
    }

    protected override void Initialize()
    {
        try
        {
            Window.Title = _options.WindowTitle;
            EngineDiagnostics.Configure(new DebugOutputDiagnosticSink());
            EngineDiagnostics.Info("MonoGame compatibility host initialized.");

            _application.Initialize();

            base.Initialize();
        }
        catch (Exception exception)
        {
            HandleFatalException("Initialize", exception);
            throw;
        }
    }

    protected override void LoadContent()
    {
        _textureCatalog = new MonoGameTextureCatalog(GraphicsDevice);
        _renderContext = new MonoGameRenderContext(GraphicsDevice, _textureCatalog);

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        try
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
                return;
            }

            var frameInput = _inputBuilder.Build(new Int2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
            _application.Update(
                new FrameTime(gameTime.TotalGameTime, gameTime.ElapsedGameTime, IsFixedTimeStep),
                frameInput);

            if (_autoCloseAfterSeconds > 0 &&
                gameTime.TotalGameTime.TotalSeconds >= _autoCloseAfterSeconds)
            {
                Exit();
                return;
            }

            base.Update(gameTime);
        }
        catch (Exception exception)
        {
            HandleFatalException("Update", exception);
            throw;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        try
        {
            if (_renderContext is not null)
            {
                _renderContext.UpdateViewportSize(new Int2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

                try
                {
                    _application.Render(_renderContext);
                }
                finally
                {
                    _renderContext.EndFrame();
                }
            }

            base.Draw(gameTime);
        }
        catch (Exception exception)
        {
            HandleFatalException("Render", exception);
            throw;
        }
    }

    protected override void UnloadContent()
    {
        _textureCatalog?.Dispose();
        base.UnloadContent();
    }

    private void HandleExiting(object sender, EventArgs args)
    {
        TryShutdown("Exit");
    }

    private static double ResolveAutoCloseAfterSeconds()
    {
        var rawValue = Environment.GetEnvironmentVariable("TILEWORLD_ENGINEHOST_AUTOCLOSE_SECONDS");
        return double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? seconds
            : 0;
    }

    private sealed class DebugOutputDiagnosticSink : IDiagnosticSink
    {
        public void Write(DiagnosticLevel level, string message)
        {
            var formatted = $"[{level}] {message}";

            Debug.WriteLine(formatted);
            Console.WriteLine(formatted);
        }
    }

    private void HandleFatalException(string stage, Exception exception)
    {
        EngineDiagnostics.Error($"MonoGame compatibility host fatal exception during {stage}: {exception}");
        TryShutdown($"Fatal {stage}");
    }

    private void TryShutdown(string reason)
    {
        if (_shutdownAttempted)
        {
            return;
        }

        _shutdownAttempted = true;

        try
        {
            EngineDiagnostics.Info($"MonoGame compatibility host shutdown requested. Reason={reason}.");
            _application.Shutdown();
        }
        catch (Exception exception)
        {
            EngineDiagnostics.Error($"MonoGame compatibility host shutdown failed: {exception}");
        }
    }
}
