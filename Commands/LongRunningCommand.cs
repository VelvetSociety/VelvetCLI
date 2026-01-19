namespace VelvetCLI.Commands;

using Spectre.Console;

internal abstract class LongRunningCommand : VelvetCommand
{
    protected abstract string StatusDescription { get; }

    public override void Execute()
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("green"))
            .Start(StatusDescription, _ => ExecuteCore());
    }
}
