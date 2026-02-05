using VelvetCLI.Commands;

namespace VelvetCLI;

internal static class CommandResolver
{
    public static bool TryResolveTopLevel(
        IReadOnlyList<VelvetCommand> commands,
        string[] parts,
        out VelvetCommand? command,
        out string[] args)
    {
        command = null;
        args = Array.Empty<string>();

        if (parts.Length == 0)
        {
            return false;
        }

        string top = parts[0];

        // Global shorthand: rb -> mod rebuild [mod-name]
        if (string.Equals(top, "rb", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryResolveCommand(commands, "mod", out command))
            {
                return false;
            }

            args = new[] { "rebuild" }.Concat(parts.Skip(1)).ToArray();
            return true;
        }

        if (!TryResolveCommand(commands, top, out command))
        {
            return false;
        }

        args = parts.Skip(1).ToArray();
        return true;
    }

    public static bool TryResolveCommand(IReadOnlyList<VelvetCommand> commands, string token, out VelvetCommand? command)
    {
        command = commands.FirstOrDefault(cmd =>
            string.Equals(cmd.Name, token, StringComparison.OrdinalIgnoreCase) ||
            cmd.Aliases.Any(alias => string.Equals(alias, token, StringComparison.OrdinalIgnoreCase)));
        return command is not null;
    }

    public static bool IsKnownTopLevelToken(IReadOnlyList<VelvetCommand> commands, string token)
    {
        if (string.Equals(token, "rb", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return TryResolveCommand(commands, token, out _);
    }

    public static IReadOnlyList<string> GetTopLevelTokens(IReadOnlyList<VelvetCommand> commands)
    {
        var tokens = new List<string>();
        foreach (var cmd in commands)
        {
            tokens.Add(cmd.Name);
            tokens.AddRange(cmd.Aliases);
        }

        tokens.Add("rb");
        return tokens
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
