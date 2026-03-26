using System;

namespace TileWorld.Engine.Hosting;

/// <summary>
/// Carries frame timing information from the host into engine applications.
/// </summary>
/// <param name="TotalTime">The total elapsed host time since application start.</param>
/// <param name="ElapsedTime">The delta time represented by the current frame.</param>
/// <param name="IsFixedStep">Whether the host is currently simulating in fixed-step mode.</param>
public readonly record struct FrameTime(TimeSpan TotalTime, TimeSpan ElapsedTime, bool IsFixedStep);
