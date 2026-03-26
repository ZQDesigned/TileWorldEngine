using System.Threading;
using System.Threading.Tasks;

namespace TileWorld.Engine.Hosting;

/// <summary>
/// Provides host-native text input collection for lightweight application flows such as world naming.
/// </summary>
public interface ITextInputService
{
    /// <summary>
    /// Gets a value indicating whether a text input request is currently active.
    /// </summary>
    bool IsRequestActive { get; }

    /// <summary>
    /// Gets the current committed text buffer for the active request.
    /// </summary>
    string CurrentText { get; }

    /// <summary>
    /// Begins collecting text input and completes when the request is confirmed or canceled.
    /// </summary>
    /// <param name="request">The parameters that describe the desired input session.</param>
    /// <param name="cancellationToken">A cancellation token that aborts the request when triggered.</param>
    /// <returns>
    /// A task that resolves to the confirmed text, or <see langword="null"/> when the request is canceled.
    /// </returns>
    Task<string> RequestTextAsync(TextInputRequest request, CancellationToken cancellationToken = default);
}
