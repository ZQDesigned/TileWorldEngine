using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Input;

namespace TileWorld.Engine.Tests.Input;

public sealed class FrameInputTests
{
    [Fact]
    public void MouseButtonSnapshot_FromStatesTracksEdgeTransitions()
    {
        var pressed = MouseButtonSnapshot.FromStates(isDown: true, wasDown: false);
        var released = MouseButtonSnapshot.FromStates(isDown: false, wasDown: true);

        Assert.True(pressed.IsDown);
        Assert.True(pressed.WentDown);
        Assert.False(pressed.WentUp);
        Assert.False(released.IsDown);
        Assert.False(released.WentDown);
        Assert.True(released.WentUp);
    }

    [Fact]
    public void FrameInput_KeyQueriesReturnStableResults()
    {
        var input = new FrameInput(
            new Int2(10, 20),
            isMouseInsideViewport: true,
            leftButton: new MouseButtonSnapshot(true, true, false),
            middleButton: default,
            rightButton: default,
            mouseWheelDelta: 120,
            keysDown: [InputKey.D1, InputKey.W, InputKey.Enter],
            keysWentDown: [InputKey.D1, InputKey.Escape],
            keysWentUp: [InputKey.F1]);

        Assert.Equal(new Int2(10, 20), input.MouseScreenPositionPixels);
        Assert.True(input.IsMouseInsideViewport);
        Assert.True(input.LeftButton.WentDown);
        Assert.Equal(120, input.MouseWheelDelta);
        Assert.True(input.IsKeyDown(InputKey.W));
        Assert.True(input.IsKeyDown(InputKey.Enter));
        Assert.True(input.KeyWentDown(InputKey.D1));
        Assert.True(input.KeyWentDown(InputKey.Escape));
        Assert.True(input.KeyWentUp(InputKey.F1));
        Assert.False(input.IsKeyDown(InputKey.D3));
    }
}
