using Spectre.Console;

namespace VelvetCLI.Commands;

internal sealed class ExitCommand : VelvetCommand
{
    public override string Name => "exit";
    public override string Description => "Exit the application";
    public override bool ShouldExitAfterRun => true;

    protected override void ExecuteCore(string[] args)
    {
        Console.WriteLine("\nExiting...");
    }
    public override void ShowHelp()
    {
        base.ShowHelp();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]Usage:[/] exit");
    }
}
