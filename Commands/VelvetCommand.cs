using Spectre.Console;

namespace VelvetCLI.Commands;

internal abstract class VelvetCommand
{
    public abstract string Name { get; }
    public virtual string Description => Name;
    public virtual bool ShouldExitAfterRun => false;

    public virtual void Execute(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "-help", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase)))
        {
            ShowHelp();
            return;
        }
        ExecuteCore(args);
    }

    protected abstract void ExecuteCore(string[] args);

    public virtual void ShowHelp()
    {
        AnsiConsole.MarkupLine($"[bold blue]Command:[/] {Name}");
        AnsiConsole.MarkupLine($"[bold blue]Description:[/] {Description}");
    }
}
