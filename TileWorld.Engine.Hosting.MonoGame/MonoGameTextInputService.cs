using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TileWorld.Engine.Hosting;

namespace TileWorld.Engine.Hosting.MonoGame;

/// <summary>
/// Collects committed text through the active MonoGame window text-input event stream.
/// </summary>
public sealed class MonoGameTextInputService : ITextInputService, IDisposable
{
    private readonly StringBuilder _buffer = new();
    private readonly GameWindow _window;
    private CancellationTokenRegistration _cancellationRegistration;
    private Func<char, bool> _characterFilter;
    private int _maxLength;
    private TaskCompletionSource<string> _pendingRequest;

    /// <summary>
    /// Creates a text input service bound to a MonoGame game window.
    /// </summary>
    /// <param name="window">The game window that will publish committed text input events.</param>
    public MonoGameTextInputService(GameWindow window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _window.TextInput += HandleTextInput;
    }

    /// <inheritdoc />
    public bool IsRequestActive => _pendingRequest is not null;

    /// <inheritdoc />
    public string CurrentText => _buffer.ToString();

    /// <inheritdoc />
    public Task<string> RequestTextAsync(TextInputRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_pendingRequest is not null)
        {
            throw new InvalidOperationException("A MonoGame text input request is already active.");
        }

        if (request.MaxLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.MaxLength, "Text input max length must be positive.");
        }

        _buffer.Clear();
        if (!string.IsNullOrEmpty(request.InitialText))
        {
            foreach (var character in request.InitialText[..Math.Min(request.InitialText.Length, request.MaxLength)])
            {
                if (request.CharacterFilter is null || request.CharacterFilter(character))
                {
                    _buffer.Append(character);
                }
            }
        }

        _maxLength = request.MaxLength;
        _characterFilter = request.CharacterFilter;
        _pendingRequest = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _cancellationRegistration = cancellationToken.CanBeCanceled
            ? cancellationToken.Register(CancelActiveRequest)
            : default;
        return _pendingRequest.Task;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _window.TextInput -= HandleTextInput;
        CompleteActiveRequest(CurrentText);
        _cancellationRegistration.Dispose();
    }

    private void HandleTextInput(object sender, TextInputEventArgs args)
    {
        if (_pendingRequest is null)
        {
            return;
        }

        switch (args.Character)
        {
            case '\b':
                if (_buffer.Length > 0)
                {
                    _buffer.Length--;
                }

                break;

            case '\r':
            case '\n':
                CompleteActiveRequest(CurrentText);
                break;

            case (char)27:
                CompleteActiveRequest(null);
                break;

            default:
                if (!char.IsControl(args.Character) &&
                    _buffer.Length < _maxLength &&
                    (_characterFilter is null || _characterFilter(args.Character)))
                {
                    _buffer.Append(args.Character);
                }

                break;
        }
    }

    private void CancelActiveRequest()
    {
        CompleteActiveRequest(null);
    }

    private void CompleteActiveRequest(string result)
    {
        var pendingRequest = _pendingRequest;
        if (pendingRequest is null)
        {
            return;
        }

        _pendingRequest = null;
        _cancellationRegistration.Dispose();
        _cancellationRegistration = default;
        _characterFilter = null;
        _maxLength = 0;
        pendingRequest.TrySetResult(result);
    }
}
