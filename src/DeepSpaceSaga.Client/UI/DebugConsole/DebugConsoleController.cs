using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

/// <summary>
/// Render-free console state: open flag, input line, retained output and one-at-a-time dispatch.
/// Closing the console does not cancel an in-flight command; <see cref="Lifetime"/> ends only on <see cref="Dispose"/>.
/// </summary>
internal sealed class DebugConsoleController : IDisposable
{
    internal const int MaxInputLength = 160;
    internal const int MaxOutputLines = 2000;
    internal const string AlreadyExecutingText = "Команда уже выполняется";
    internal const string StaleResultText = "Результат устарел: сессия сменилась";

    private readonly DebugCommandRegistry _registry;
    private readonly IDebugConsoleHost _host;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _gate = new();
    private readonly List<string> _output = [];
    private string? _outputText;
    private int _outputVersion;
    private int _executing;
    private volatile bool _isOpen;

    public DebugConsoleController(DebugCommandRegistry registry, IDebugConsoleHost host)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        OutputPanel.HasFocus = true;
    }

    public static bool IsToggleChord(Key key, KeyModifiers modifiers) =>
        key == Key.P && modifiers.HasFlag(KeyModifiers.Control) && modifiers.HasFlag(KeyModifiers.Alt);

    /// <summary>Name is the first whitespace-delimited token; arguments are the trimmed rest with inner spaces kept.</summary>
    public static bool TryParse(string line, out string name, out string arguments)
    {
        string trimmed = (line ?? "").Trim();
        if (trimmed.Length == 0)
        {
            name = arguments = "";
            return false;
        }
        int split = 0;
        while (split < trimmed.Length && !char.IsWhiteSpace(trimmed[split])) split++;
        name = trimmed[..split];
        arguments = trimmed[split..].Trim();
        return true;
    }

    public bool IsOpen => _isOpen;
    public bool IsExecuting => Volatile.Read(ref _executing) != 0;
    public TextInputBox Input { get; } = new(MaxInputLength);
    /// <summary>Scroll state for the output; the overlay lays it out and draws it.</summary>
    internal ScrollTextPanel OutputPanel { get; } = new();
    public CancellationToken Lifetime => _lifetime.Token;

    public IReadOnlyList<string> Output
    {
        get { lock (_gate) return _output.ToArray(); }
    }

    /// <summary>Changes whenever <see cref="Output"/> changes; lets the overlay skip re-wrapping unchanged text.</summary>
    internal int OutputVersion => Volatile.Read(ref _outputVersion);

    internal string OutputText
    {
        get { lock (_gate) return _outputText ??= string.Join('\n', _output); }
    }

    public void Open() => _isOpen = true;
    public void Close() => _isOpen = false;
    public void Toggle() => _isOpen = !_isOpen;

    public async Task SubmitAsync()
    {
        string line = Input.Text;
        if (!TryParse(line, out string name, out string arguments)) return;
        if (Interlocked.CompareExchange(ref _executing, 1, 0) != 0)
        {
            Append([AlreadyExecutingText]);
            return;
        }

        DebugCommandResult result;
        DebugCommandContext? context = null;
        try
        {
            Input.Clear();
            Append(["> " + line.Trim()]);
            if (!_registry.TryGet(name, out var command))
            {
                Append([$"Неизвестная команда: {name}. Введите help"]);
                return;
            }
            context = new DebugCommandContext(_registry, _host);
            try
            {
                result = await command.ExecuteAsync(context, arguments, Lifetime);
            }
            catch (OperationCanceledException) when (Lifetime.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                result = DebugCommandResult.Error("Ошибка: " + ex.Message);
            }
        }
        finally
        {
            Volatile.Write(ref _executing, 0);
        }

        if (Lifetime.IsCancellationRequested) return;
        if (!context.IsCurrent)
        {
            Append([StaleResultText]);
            return;
        }
        Append(result.Lines);
        if (result.CloseConsole) Close();
    }

    /// <summary>Consumes every key while open: Esc closes, Enter submits, Backspace edits, Up/Down/Home/End scroll output.</summary>
    public bool OnKeyDown(Key key, KeyModifiers modifiers, bool repeat)
    {
        switch (key)
        {
            case Key.Escape:
                Close();
                break;
            case Key.Enter or Key.KeypadEnter when !repeat:
                _ = SubmitAsync();
                break;
            case Key.Backspace:
                Input.Backspace();
                break;
            case Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown:
                OutputPanel.OnKeyDown(key);
                break;
        }
        return true;
    }

    public void OnTextInput(char c) => Input.OnTextInput(c);

    private void Append(IEnumerable<string> lines)
    {
        lock (_gate)
        {
            foreach (string line in lines)
                foreach (string part in (line ?? "").Replace("\r\n", "\n").Split('\n'))
                    _output.Add(part);
            if (_output.Count > MaxOutputLines) _output.RemoveRange(0, _output.Count - MaxOutputLines);
            _outputText = null;
            _outputVersion++;
        }
    }

    // The source owns no timer, so cancelling is enough; leaving it undisposed keeps Lifetime readable by late continuations.
    public void Dispose()
    {
        _isOpen = false;
        _lifetime.Cancel();
    }
}
