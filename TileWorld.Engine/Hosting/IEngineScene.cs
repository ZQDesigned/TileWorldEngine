using TileWorld.Engine.Input;
using TileWorld.Engine.Render;

namespace TileWorld.Engine.Hosting;

/// <summary>
/// Represents a lightweight application scene owned by <see cref="SceneHostApplication"/>.
/// </summary>
public interface IEngineScene
{
    /// <summary>
    /// Called when the scene becomes active.
    /// </summary>
    /// <param name="sceneHost">The scene host that now owns the scene.</param>
    void OnEnter(SceneHostApplication sceneHost);

    /// <summary>
    /// Called before the scene is deactivated.
    /// </summary>
    void OnExit();

    /// <summary>
    /// Updates the active scene for the current frame.
    /// </summary>
    /// <param name="frameTime">The current frame timing snapshot.</param>
    /// <param name="input">The current frame input snapshot.</param>
    void Update(FrameTime frameTime, FrameInput input);

    /// <summary>
    /// Renders the active scene into the supplied render context.
    /// </summary>
    /// <param name="renderContext">The render context for the current frame.</param>
    void Render(IRenderContext renderContext);
}
