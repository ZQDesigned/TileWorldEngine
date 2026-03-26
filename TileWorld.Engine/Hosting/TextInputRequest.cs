using System;

namespace TileWorld.Engine.Hosting;

/// <summary>
/// Describes a host-native text input request.
/// </summary>
public sealed class TextInputRequest
{
    /// <summary>
    /// Gets the descriptive title shown by the caller for the request.
    /// </summary>
    public string Title { get; init; } = "Text Input";

    /// <summary>
    /// Gets the optional hint shown by the caller while input is active.
    /// </summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>
    /// Gets the initial committed text buffer.
    /// </summary>
    public string InitialText { get; init; } = string.Empty;

    /// <summary>
    /// Gets the maximum number of committed characters accepted by the request.
    /// </summary>
    public int MaxLength { get; init; } = 64;

    /// <summary>
    /// Gets an optional character filter applied by the host while text input is active.
    /// </summary>
    /// <remarks>
    /// When specified, characters for which this delegate returns <see langword="false"/> are ignored by the host.
    /// Hosts should still preserve control keys such as backspace, enter, and escape.
    /// </remarks>
    public Func<char, bool> CharacterFilter { get; init; }
}
