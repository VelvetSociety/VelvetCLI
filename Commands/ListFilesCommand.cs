namespace VelvetCLI.Commands;

internal sealed class ListFilesCommand : VelvetCommand
{
    private readonly CommandExecutor _executor;

    public ListFilesCommand(CommandExecutor executor)
    {
        _executor = executor;
    }

    public override string Name => "list";
    public override string Description => "List files in current directory";

    protected override void ExecuteCore() => _executor.RunCommand("dir");
}
