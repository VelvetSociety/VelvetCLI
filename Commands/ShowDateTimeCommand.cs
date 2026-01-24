using VelvetCLI;

namespace VelvetCLI.Commands;

internal sealed class ShowDateTimeCommand : VelvetCommand
{
    private readonly CommandExecutor _executor;

    public ShowDateTimeCommand(CommandExecutor executor)
    {
        _executor = executor;
    }

    public override string Name => "time";
    public override string Description => "Show current date and time";

    protected override void ExecuteCore(string[] args) => _executor.RunCommand("date /T && time /T");
}
