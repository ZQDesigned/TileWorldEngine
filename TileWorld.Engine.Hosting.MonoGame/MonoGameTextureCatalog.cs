using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TileWorld.Engine.Render;

namespace TileWorld.Engine.Hosting.MonoGame;

internal sealed class MonoGameTextureCatalog : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.Ordinal);

    public MonoGameTextureCatalog(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        var debugWhite = new Texture2D(graphicsDevice, 1, 1);
        debugWhite.SetData([Color.White]);

        _textures.Add("debug/white", debugWhite);
    }

    public Texture2D GetRequiredTexture(string textureKey)
    {
        if (!_textures.TryGetValue(textureKey, out var texture))
        {
            throw new KeyNotFoundException($"No MonoGame texture is registered for key '{textureKey}'.");
        }

        return texture;
    }

    public bool HasTexture(string textureKey)
    {
        ArgumentNullException.ThrowIfNull(textureKey);
        return _textures.ContainsKey(textureKey);
    }

    public void RegisterBitmap(string textureKey, TextureBitmapRgba32 bitmap)
    {
        ArgumentNullException.ThrowIfNull(textureKey);
        ArgumentNullException.ThrowIfNull(bitmap);

        if (_textures.ContainsKey(textureKey))
        {
            return;
        }

        var texture = new Texture2D(_graphicsDevice, bitmap.Width, bitmap.Height);
        var colors = new Color[bitmap.Width * bitmap.Height];
        var pixels = bitmap.Pixels;
        for (var index = 0; index < pixels.Length; index++)
        {
            var pixel = pixels[index];
            colors[index] = new Color(pixel.R, pixel.G, pixel.B, pixel.A);
        }

        texture.SetData(colors);
        _textures.Add(textureKey, texture);
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
    }
}
