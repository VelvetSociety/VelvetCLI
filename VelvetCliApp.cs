using System;
using Spectre.Console;
using VelvetCLI.Commands;

namespace VelvetCLI;

internal sealed class VelvetCliApp
{
    private readonly CommandRegistry _commandRegistry;

    public VelvetCliApp()
        : this(CommandRegistry.Default)
    {
    }

    internal VelvetCliApp(CommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry;
    }

    public void Run()
    {
        while (true)
        {
            HeaderRenderer.Render();
            //AnsiConsole.MarkupLine("[grey]Type '/' to open the command palette, or press Enter to show the full menu.[/]");
            //Console.WriteLine();
            Console.Write("> ");

            string? input = InputPalette.ReadLineWithPalette(_commandRegistry.Commands);

            if (string.IsNullOrWhiteSpace(input))
            {
                VelvetCommand chosen = MenuView.Show(_commandRegistry.Commands);
                if (ExecuteCommand(chosen, Array.Empty<string>()))
                {
                    return;
                }

                continue;
            }

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && CommandResolver.TryResolveTopLevel(_commandRegistry.Commands, parts, out VelvetCommand? matched, out string[] args))
            {
                if (ExecuteCommand(matched, args))
                {
                    return;
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Unrecognized command:[/] {0}", input);
            }
        }
    }

    private static bool ExecuteCommand(VelvetCommand command, string[] args)
    {
        command.Execute(args);

        if (command.ShouldExitAfterRun)
        {
            return true;
        }

        // Do not pause; immediately return to the prompt loop.
        return false;
    }
}
