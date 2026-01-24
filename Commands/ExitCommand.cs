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
}
