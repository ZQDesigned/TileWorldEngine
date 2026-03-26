using TileWorld.Engine.Input;
using TileWorld.Engine.Render;

namespace TileWorld.Engine.Hosting;

/// <summary>
/// Defines the host-facing lifecycle contract for an engine application.
/// </summary>
public interface IEngineApplication
{
    /// <summary>
    /// Initializes the application and any owned engine systems.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Advances application state for one frame.
    /// </summary>
    /// <param name="frameTime">The frame timing snapshot supplied by the active host.</param>
    /// <param name="input">The input snapshot for the current frame.</param>
    void Update(FrameTime frameTime, FrameInput input);

    /// <summary>
    /// Draws the current frame into the supplied render context.
    /// </summary>
    /// <param name="renderContext">The render context exposed by the active host backend.</param>
    void Render(IRenderContext renderContext);

    /// <summary>
    /// Shuts the application down and releases owned resources.
    /// </summary>
    void Shutdown();
}
