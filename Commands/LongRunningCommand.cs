namespace VelvetCLI.Commands;

using Spectre.Console;

internal abstract class LongRunningCommand : VelvetCommand
{
    protected abstract string StatusDescription { get; }

    public override void Execute(string[] args)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green"))
            .Start(StatusDescription, _ => ExecuteCore(args));
    }

    protected abstract override void ExecuteCore(string[] args);
}
