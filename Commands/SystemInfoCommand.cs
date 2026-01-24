using Spectre.Console;
using VelvetCLI;

namespace VelvetCLI.Commands;

internal sealed class SystemInfoCommand : LongRunningCommand
{
    private readonly CommandExecutor _executor;

    public SystemInfoCommand(CommandExecutor executor)
    {
        _executor = executor;
    }

    public override string Name => "sysinfo";
    public override string Description => "Display system information";

    protected override string StatusDescription => "Gathering system information...";

    protected override void ExecuteCore(string[] args) => _executor.RunCommand("systeminfo");

    public override void ShowHelp()
    {
        base.ShowHelp();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]Usage:[/] sysinfo");
    }
}
