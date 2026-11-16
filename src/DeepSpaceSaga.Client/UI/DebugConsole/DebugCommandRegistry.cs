using System.Diagnostics.CodeAnalysis;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

/// <summary>Single source for dispatch and help: commands keyed by exact (Ordinal) name.</summary>
internal sealed class DebugCommandRegistry
{
    private readonly Dictionary<string, IDebugCommand> _commands = new(StringComparer.Ordinal);

    public void Register(IDebugCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        string name = command.Descriptor.Name;
        if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsWhiteSpace))
            throw new ArgumentException($"Invalid debug command name '{name}'.", nameof(command));
        if (!_commands.TryAdd(name, command))
            throw new InvalidOperationException($"Debug command '{name}' is already registered.");
    }

    public bool TryGet(string name, [NotNullWhen(true)] out IDebugCommand? command) =>
        _commands.TryGetValue(name, out command);

    public IReadOnlyList<DebugCommandDescriptor> DescribeAll() =>
        _commands.Values.Select(c => c.Descriptor).OrderBy(d => d.Name, StringComparer.Ordinal).ToArray();
}
