using TileWorld.Engine.Render;

namespace TileWorld.Engine.Hosting;

/// <summary>
/// Exposes host-owned texture registration for backend-neutral bitmap data.
/// </summary>
public interface ITextureBitmapRegistry
{
    /// <summary>
    /// Returns whether the host has already registered a texture for the supplied key.
    /// </summary>
    /// <param name="textureKey">The backend-neutral texture key.</param>
    /// <returns><see langword="true"/> when the texture key is already registered.</returns>
    bool HasTexture(string textureKey);

    /// <summary>
    /// Registers a backend-neutral bitmap as a host texture resource.
    /// </summary>
    /// <param name="textureKey">The backend-neutral texture key.</param>
    /// <param name="bitmap">The bitmap data to upload.</param>
    void RegisterTextureBitmap(string textureKey, TextureBitmapRgba32 bitmap);
}
