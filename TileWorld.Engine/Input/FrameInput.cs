using System.Collections.Generic;
using TileWorld.Engine.Core.Math;

namespace TileWorld.Engine.Input;

/// <summary>
/// Represents the host input snapshot for a single frame.
/// </summary>
public sealed class FrameInput
{
    private readonly HashSet<InputKey> _keysDown;
    private readonly HashSet<InputKey> _keysWentDown;
    private readonly HashSet<InputKey> _keysWentUp;

    /// <summary>
    /// Creates a frame input snapshot.
    /// </summary>
    /// <param name="mouseScreenPositionPixels">The mouse position in screen pixels.</param>
    /// <param name="isMouseInsideViewport">Whether the mouse is currently inside the viewport.</param>
    /// <param name="leftButton">The left mouse button state.</param>
    /// <param name="middleButton">The middle mouse button state.</param>
    /// <param name="rightButton">The right mouse button state.</param>
    /// <param name="mouseWheelDelta">The wheel delta since the previous frame.</param>
    /// <param name="keysDown">The keys that are currently down.</param>
    /// <param name="keysWentDown">The keys that transitioned to down this frame.</param>
    /// <param name="keysWentUp">The keys that transitioned to up this frame.</param>
    public FrameInput(
        Int2 mouseScreenPositionPixels,
        bool isMouseInsideViewport,
        MouseButtonSnapshot leftButton,
        MouseButtonSnapshot middleButton,
        MouseButtonSnapshot rightButton,
        int mouseWheelDelta,
        IEnumerable<InputKey> keysDown = null,
        IEnumerable<InputKey> keysWentDown = null,
        IEnumerable<InputKey> keysWentUp = null)
    {
        MouseScreenPositionPixels = mouseScreenPositionPixels;
        IsMouseInsideViewport = isMouseInsideViewport;
        LeftButton = leftButton;
        MiddleButton = middleButton;
        RightButton = rightButton;
        MouseWheelDelta = mouseWheelDelta;
        _keysDown = CreateKeySet(keysDown);
        _keysWentDown = CreateKeySet(keysWentDown);
        _keysWentUp = CreateKeySet(keysWentUp);
    }

    /// <summary>
    /// Gets a reusable empty input snapshot.
    /// </summary>
    public static FrameInput Empty { get; } = new(
        Int2.Zero,
        false,
        default,
        default,
        default,
        0);

    /// <summary>
    /// Gets the mouse position in screen pixels.
    /// </summary>
    public Int2 MouseScreenPositionPixels { get; }

    /// <summary>
    /// Gets a value indicating whether the mouse is currently inside the viewport.
    /// </summary>
    public bool IsMouseInsideViewport { get; }

    /// <summary>
    /// Gets the left mouse-button state snapshot.
    /// </summary>
    public MouseButtonSnapshot LeftButton { get; }

    /// <summary>
    /// Gets the middle mouse-button state snapshot.
    /// </summary>
    public MouseButtonSnapshot MiddleButton { get; }

    /// <summary>
    /// Gets the right mouse-button state snapshot.
    /// </summary>
    public MouseButtonSnapshot RightButton { get; }

    /// <summary>
    /// Gets the mouse wheel delta since the previous frame.
    /// </summary>
    public int MouseWheelDelta { get; }

    /// <summary>
    /// Returns whether a key is currently pressed.
    /// </summary>
    /// <param name="key">The key to test.</param>
    /// <returns><see langword="true"/> when the key is currently pressed.</returns>
    public bool IsKeyDown(InputKey key)
    {
        return _keysDown.Contains(key);
    }

    /// <summary>
    /// Returns whether a key transitioned to down on the current frame.
    /// </summary>
    /// <param name="key">The key to test.</param>
    /// <returns><see langword="true"/> when the key transitioned to down on the current frame.</returns>
    public bool KeyWentDown(InputKey key)
    {
        return _keysWentDown.Contains(key);
    }

    /// <summary>
    /// Returns whether a key transitioned to up on the current frame.
    /// </summary>
    /// <param name="key">The key to test.</param>
    /// <returns><see langword="true"/> when the key transitioned to up on the current frame.</returns>
    public bool KeyWentUp(InputKey key)
    {
        return _keysWentUp.Contains(key);
    }

    private static HashSet<InputKey> CreateKeySet(IEnumerable<InputKey> keys)
    {
        return keys is null
            ? []
            : new HashSet<InputKey>(keys);
    }
}
