using Spectre.Console;

namespace VelvetCLI.Commands;

internal abstract class VelvetCommand
{
    public abstract string Name { get; }
    public virtual string Description => Name;
    public virtual bool ShouldExitAfterRun => false;

    public virtual void Execute(string[] args) => ExecuteCore(args);

    protected abstract void ExecuteCore(string[] args);
}
