using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Render;

namespace TileWorld.Engine.Hosting.MonoGame;

internal sealed class MonoGameRenderContext : IRenderContext
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly MonoGameTextureCatalog _textureCatalog;
    private bool _hasBegun;

    public MonoGameRenderContext(GraphicsDevice graphicsDevice, MonoGameTextureCatalog textureCatalog)
    {
        _graphicsDevice = graphicsDevice;
        _textureCatalog = textureCatalog;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        ViewportSizePixels = Int2.Zero;
    }

    public Int2 ViewportSizePixels { get; private set; }

    public void UpdateViewportSize(Int2 viewportSizePixels)
    {
        ViewportSizePixels = viewportSizePixels;
    }

    public void Clear(ColorRgba32 color)
    {
        if (_hasBegun)
        {
            _spriteBatch.End();
            _hasBegun = false;
        }

        _graphicsDevice.Clear(new Color(color.R, color.G, color.B, color.A));
    }

    public void DrawSprite(SpriteDrawCommand command)
    {
        EnsureBegun();

        _spriteBatch.Draw(
            _textureCatalog.GetRequiredTexture(command.TextureKey),
            new Rectangle(
                command.DestinationRectPixels.X,
                command.DestinationRectPixels.Y,
                command.DestinationRectPixels.Width,
                command.DestinationRectPixels.Height),
            new Rectangle(
                command.SourceRect.X,
                command.SourceRect.Y,
                command.SourceRect.Width,
                command.SourceRect.Height),
            new Color(command.Tint.R, command.Tint.G, command.Tint.B, command.Tint.A),
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            command.LayerDepth);
    }

    public void EndFrame()
    {
        if (_hasBegun)
        {
            _spriteBatch.End();
            _hasBegun = false;
        }
    }

    private void EnsureBegun()
    {
        if (_hasBegun)
        {
            return;
        }

        _spriteBatch.Begin(
            SpriteSortMode.FrontToBack,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        _hasBegun = true;
    }
}
