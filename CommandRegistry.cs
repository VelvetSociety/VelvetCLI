using GreetingApp.Commands;
using VelvetCLI.Commands;

namespace VelvetCLI;

internal sealed class CommandRegistry
{
    public IReadOnlyList<VelvetCommand> Commands { get; }

    private CommandRegistry(IReadOnlyList<VelvetCommand> commands)
    {
        Commands = commands;
    }

    public static CommandRegistry Default => new(new CommandExecutor());

    private static IReadOnlyList<VelvetCommand> CreateCommands(CommandExecutor executor)
    {
        return new VelvetCommand[]
        {
            //new ListFilesCommand(executor),
            new InitialiseWorkspaceCommand(),
            //new ShowDateTimeCommand(executor),
            //new SystemInfoCommand(executor),
            new ExitCommand()
        };
    }

    private CommandRegistry(CommandExecutor executor)
        : this(CreateCommands(executor))
    {
    }
}
