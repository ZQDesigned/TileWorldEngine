namespace TileWorld.Engine.Core.Diagnostics;

/// <summary>
/// Discards all diagnostic messages.
/// </summary>
public sealed class NullDiagnosticSink : IDiagnosticSink
{
    /// <summary>
    /// Gets the singleton sink instance.
    /// </summary>
    public static NullDiagnosticSink Instance { get; } = new();

    private NullDiagnosticSink()
    {
    }

    /// <summary>
    /// Discards the supplied diagnostic message.
    /// </summary>
    /// <param name="level">The severity attached to the message.</param>
    /// <param name="message">The diagnostic message.</param>
    public void Write(DiagnosticLevel level, string message)
    {
    }
}
