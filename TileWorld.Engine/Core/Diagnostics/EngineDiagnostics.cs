namespace TileWorld.Engine.Core.Diagnostics;

/// <summary>
/// Provides a global diagnostic entry point used by the engine and host layers.
/// </summary>
public static class EngineDiagnostics
{
    private static IDiagnosticSink _sink = NullDiagnosticSink.Instance;

    /// <summary>
    /// Replaces the active diagnostic sink.
    /// </summary>
    /// <param name="sink">The sink that should receive future diagnostic messages.</param>
    public static void Configure(IDiagnosticSink sink)
    {
        _sink = sink ?? NullDiagnosticSink.Instance;
    }

    /// <summary>
    /// Writes a trace-level diagnostic message.
    /// </summary>
    /// <param name="message">The diagnostic message to emit.</param>
    public static void Trace(string message)
    {
        Write(DiagnosticLevel.Trace, message);
    }

    /// <summary>
    /// Writes an info-level diagnostic message.
    /// </summary>
    /// <param name="message">The diagnostic message to emit.</param>
    public static void Info(string message)
    {
        Write(DiagnosticLevel.Info, message);
    }

    /// <summary>
    /// Writes a warning-level diagnostic message.
    /// </summary>
    /// <param name="message">The diagnostic message to emit.</param>
    public static void Warn(string message)
    {
        Write(DiagnosticLevel.Warn, message);
    }

    /// <summary>
    /// Writes an error-level diagnostic message.
    /// </summary>
    /// <param name="message">The diagnostic message to emit.</param>
    public static void Error(string message)
    {
        Write(DiagnosticLevel.Error, message);
    }

    /// <summary>
    /// Writes a diagnostic message with an explicit level.
    /// </summary>
    /// <param name="level">The diagnostic severity.</param>
    /// <param name="message">The diagnostic message to emit.</param>
    public static void Write(DiagnosticLevel level, string message)
    {
        _sink.Write(level, message);
    }
}
