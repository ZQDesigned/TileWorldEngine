namespace TileWorld.Engine.Input;

/// <summary>
/// Describes the current and edge-transition state of a mouse button.
/// </summary>
/// <param name="IsDown">Whether the button is currently pressed.</param>
/// <param name="WentDown">Whether the button transitioned to pressed this frame.</param>
/// <param name="WentUp">Whether the button transitioned to released this frame.</param>
public readonly record struct MouseButtonSnapshot(bool IsDown, bool WentDown, bool WentUp)
{
    /// <summary>
    /// Creates a button snapshot from current and previous button states.
    /// </summary>
    /// <param name="isDown">Whether the button is currently pressed.</param>
    /// <param name="wasDown">Whether the button was pressed on the previous frame.</param>
    /// <returns>A snapshot containing current-state and edge-transition information.</returns>
    public static MouseButtonSnapshot FromStates(bool isDown, bool wasDown)
    {
        return new MouseButtonSnapshot(
            IsDown: isDown,
            WentDown: isDown && !wasDown,
            WentUp: !isDown && wasDown);
    }
}
