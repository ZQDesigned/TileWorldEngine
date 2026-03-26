using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using TileWorld.Engine.Core.Math;
using TileWorld.Engine.Input;

namespace TileWorld.Engine.Hosting.MonoGame;

internal sealed class MonoGameFrameInputBuilder
{
    private static readonly (Keys MonoGameKey, InputKey InputKey)[] KeyMappings =
    [
        (Keys.D1, InputKey.D1),
        (Keys.D2, InputKey.D2),
        (Keys.D3, InputKey.D3),
        (Keys.F1, InputKey.F1),
        (Keys.F5, InputKey.F5),
        (Keys.Enter, InputKey.Enter),
        (Keys.Escape, InputKey.Escape),
        (Keys.W, InputKey.W),
        (Keys.A, InputKey.A),
        (Keys.S, InputKey.S),
        (Keys.D, InputKey.D),
        (Keys.Up, InputKey.Up),
        (Keys.Down, InputKey.Down),
        (Keys.Left, InputKey.Left),
        (Keys.Right, InputKey.Right),
        (Keys.LeftShift, InputKey.LeftShift),
        (Keys.RightShift, InputKey.RightShift)
    ];

    private bool _hasPreviousState;
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;

    public FrameInput Build(Int2 viewportSizePixels)
    {
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        var input = new FrameInput(
            new Int2(mouseState.X, mouseState.Y),
            IsInsideViewport(mouseState, viewportSizePixels),
            MouseButtonSnapshot.FromStates(
                mouseState.LeftButton == ButtonState.Pressed,
                _hasPreviousState && _previousMouseState.LeftButton == ButtonState.Pressed),
            MouseButtonSnapshot.FromStates(
                mouseState.MiddleButton == ButtonState.Pressed,
                _hasPreviousState && _previousMouseState.MiddleButton == ButtonState.Pressed),
            MouseButtonSnapshot.FromStates(
                mouseState.RightButton == ButtonState.Pressed,
                _hasPreviousState && _previousMouseState.RightButton == ButtonState.Pressed),
            _hasPreviousState ? mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue : 0,
            EnumerateKeys(key => keyboardState.IsKeyDown(key)),
            EnumerateKeys(key => keyboardState.IsKeyDown(key) && (!_hasPreviousState || !_previousKeyboardState.IsKeyDown(key))),
            EnumerateKeys(key => !keyboardState.IsKeyDown(key) && _hasPreviousState && _previousKeyboardState.IsKeyDown(key)));

        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;
        _hasPreviousState = true;
        return input;
    }

    private static bool IsInsideViewport(MouseState mouseState, Int2 viewportSizePixels)
    {
        return mouseState.X >= 0 &&
               mouseState.X < viewportSizePixels.X &&
               mouseState.Y >= 0 &&
               mouseState.Y < viewportSizePixels.Y;
    }

    private static IEnumerable<InputKey> EnumerateKeys(Func<Keys, bool> predicate)
    {
        foreach (var (monoGameKey, inputKey) in KeyMappings)
        {
            if (predicate(monoGameKey))
            {
                yield return inputKey;
            }
        }
    }
}
