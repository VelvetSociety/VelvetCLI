using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Spectre.Console;

namespace VelvetCLI.Commands;

internal sealed class ModCommand : VelvetCommand
{
    public override string Name => "mod";
    public override string Description => "Manage mods (clone <url>, open [[mod-name]])";

    protected override void ExecuteCore(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }

        string subCommand = args[0].ToLower();

        switch (subCommand)
        {
            case "clone":
                ExecuteClone(args);
                break;
            case "open":
                ExecuteOpen(args);
                break;
            default:
                AnsiConsole.MarkupLine($"[red]Unknown subcommand:[/] {subCommand}");
                ShowUsage();
                break;
        }
    }

    private void ShowUsage()
    {
        AnsiConsole.MarkupLine("[bold blue]Usage:[/]");
        AnsiConsole.MarkupLine("  mod clone <github-link>");
        AnsiConsole.MarkupLine("  mod open [[mod-name]]");
    }

    private void ExecuteClone(string[] args)
    {
        if (args.Length < 2)
        {
            AnsiConsole.MarkupLine("[red]Usage:[/] mod clone <github-link>");
            return;
        }

        string githubLink = args[1];
        const string modsFolder = "mods";

        if (!Directory.Exists(modsFolder))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] The 'mods' folder does not exist. This is not a valid workspace.");
            AnsiConsole.MarkupLine("[grey]Tip: Use 'init' to initialize a workspace.[/]");
            return;
        }

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start($"[bold blue]Cloning repository:[/] {githubLink}...", ctx =>
            {
                try
                {
                    var process = new Process();
                    process.StartInfo.FileName = "git";
                    process.StartInfo.Arguments = $"clone {githubLink}";
                    process.StartInfo.WorkingDirectory = modsFolder;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        AnsiConsole.MarkupLine("[green]Successfully cloned repository into the mods folder.[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Failed to clone repository.[/]");
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            AnsiConsole.MarkupLine($"[grey]{error}[/]");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error executing git clone:[/] {ex.Message}");
                }
            });
    }

    private void ExecuteOpen(string[] args)
    {
        const string modsFolder = "mods";

        if (!Directory.Exists(modsFolder))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] The 'mods' folder does not exist. Use 'init' to create it.");
            return;
        }

        string? selectedMod = null;

        if (args.Length >= 2)
        {
            selectedMod = args[1];
            if (!Directory.Exists(Path.Combine(modsFolder, selectedMod)))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Mod '[yellow]{selectedMod}[/]' not found in [blue]{modsFolder}[/].");
                selectedMod = null; // Fallback to list if explicit name fails? Or just return?
                // Let's just return to be safe, or show the list. I'll show the list.
            }
        }

        if (selectedMod == null)
        {
            var mods = Directory.GetDirectories(modsFolder)
                .Select(Path.GetFileName)
                .ToList();

            if (mods.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No mods found in the 'mods' folder.[/]");
                return;
            }

            selectedMod = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]mod[/] to open:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more mods)[/]")
                    .AddChoices(mods));
        }

        if (!string.IsNullOrEmpty(selectedMod))
        {
            string modPath = Path.GetFullPath(Path.Combine(modsFolder, selectedMod));
            AnsiConsole.MarkupLine($"[bold blue]Opening mod:[/] [yellow]{selectedMod}[/]");

            try
            {
                var process = new Process();
                process.StartInfo.FileName = "antigravity";
                // We pass the path as the argument. 
                // Using . in powershell "antigravity ." opened root. 
                // So "antigravity path" should open that path.
                process.StartInfo.Arguments = $"\"{modPath}\"";
                process.StartInfo.UseShellExecute = true; // Use shell to find 'antigravity' in PATH
                process.Start();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error opening mod with 'antigravity':[/] {ex.Message}");
            }
        }
    }
}
