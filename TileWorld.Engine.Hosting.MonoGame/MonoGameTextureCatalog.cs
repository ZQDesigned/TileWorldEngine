using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileWorld.Engine.Hosting.MonoGame;

internal sealed class MonoGameTextureCatalog : IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.Ordinal);

    public MonoGameTextureCatalog(GraphicsDevice graphicsDevice)
    {
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

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
    }
}
