namespace TileWorld.Engine.Core.Diagnostics;

/// <summary>
/// Defines a destination for diagnostic messages emitted by the engine.
/// </summary>
public interface IDiagnosticSink
{
    void Write(DiagnosticLevel level, string message);
}
