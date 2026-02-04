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
            case "select":
                ExecuteSelect(args);
                break;
            case "ide":
                ExecuteIde(args);
                break;
            case "rebuild":
                ExecuteRebuild(args);
                break;
            default:
                AnsiConsole.MarkupLine($"[red]Unknown subcommand:[/] {subCommand}");
                ShowUsage();
                break;
        }
    }

    public override void ShowHelp()
    {
        base.ShowHelp();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]Subcommands:[/]");
        AnsiConsole.MarkupLine("  [green]clone <url>[/]      Clones a mod repository into the 'mods' folder.");
        AnsiConsole.MarkupLine("  [green]open [[mod-name]][/] Opens a specific mod (or lists all) in your IDE.");
        AnsiConsole.MarkupLine("  [green]select[/]            Opens a menu to toggle which mods are active.");
        AnsiConsole.MarkupLine("  [green]ide [[command]][/]   Sets your preferred IDE command (e.g., 'code').");
        AnsiConsole.MarkupLine("  [green]ide clear[/]         Resets IDE preferences to defaults.");
        AnsiConsole.MarkupLine("  [green]rebuild [[mod-name]][/] Rebuilds a specific mod or shows a selection menu.");
    }

    private void ShowUsage()
    {
        AnsiConsole.MarkupLine("[bold blue]Usage:[/]");
        AnsiConsole.MarkupLine("  mod clone <github-link>");
        AnsiConsole.MarkupLine("  mod open [[mod-name]]");
        AnsiConsole.MarkupLine("  mod select");
        AnsiConsole.MarkupLine("  mod ide [[command|clear]]");
        AnsiConsole.MarkupLine("  mod rebuild [[mod-name]]");
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

            string? ideCommand = GetConfiguredIde();
            
            if (ideCommand != null)
            {
                TryOpenWithIde(ideCommand, modPath);
            }
            else
            {
                // Try defaults
                string[] defaults = { "antigravity", "code", "idea64.exe" };
                bool success = false;
                foreach (var defaultIde in defaults)
                {
                    if (TryOpenWithIde(defaultIde, modPath, silent: true))
                    {
                        success = true;
                        break;
                    }
                }

                if (!success)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] Could not find a suitable IDE to open the mod.");
                    AnsiConsole.MarkupLine("[grey]Tip: Use 'mod ide <command>' to set your preferred editor.[/]");
                }
            }
        }
    }

    private string? GetConfiguredIde()
    {
        const string settingsFile = "settings.json";
        if (File.Exists(settingsFile))
        {
            try
            {
                string json = File.ReadAllText(settingsFile);
                var settings = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json);
                if (settings != null && settings.TryGetValue("IdeCommand", out string? command))
                {
                    return command;
                }
            }
            catch { }
        }
        return null;
    }

    private bool TryOpenWithIde(string ide, string path, bool silent = false)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C {ide} \"{path}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            
            // For a quick check, we wait a very short time or check if it immediate failed (e.g. command not found)
            if (silent)
            {
                if (process.WaitForExit(500))
                {
                    if (process.ExitCode != 0) return false;
                }
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ExecuteIde(string[] args)
    {
        const string settingsFile = "settings.json";

        var settings = new System.Collections.Generic.Dictionary<string, string>();

        if (args.Length < 2)
        {
            string? current = GetConfiguredIde();
            if (current != null)
                AnsiConsole.MarkupLine($"[bold blue]Current IDE Command:[/] [yellow]{current}[/]");
            else
                AnsiConsole.MarkupLine("[yellow]No custom IDE command configured. Using defaults (antigravity, code, idea64.exe).[/]");
            
            AnsiConsole.MarkupLine("[grey]Usage: mod ide <command>[/]");
            return;
        }

        if (args.Length >= 2 && string.Equals(args[1], "clear", StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(settingsFile))
            {
                try
                {
                    string json = File.ReadAllText(settingsFile);
                    settings = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json) ?? settings;
                    if (settings.Remove("IdeCommand"))
                    {
                        File.WriteAllText(settingsFile, System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                        AnsiConsole.MarkupLine("[green]Custom IDE setting cleared. Now using defaults.[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]No custom IDE setting was found to clear.[/]");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error clearing settings:[/] {ex.Message}");
                }
            }
            return;
        }

        string newIde = string.Join(" ", args.Skip(1));
        
        if (File.Exists(settingsFile))
        {
            try
            {
                string json = File.ReadAllText(settingsFile);
                settings = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json) ?? settings;
            }
            catch { }
        }

        settings["IdeCommand"] = newIde;
        
        try
        {
            string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, json);
            AnsiConsole.MarkupLine($"[green]IDE command updated to:[/] [yellow]{newIde}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error saving settings:[/] {ex.Message}");
        }
    }

    private void ExecuteSelect(string[] args)
    {
        const string modsFolder = "mods";
        const string selectionFile = "mods/selected_mods.json";

        if (!Directory.Exists(modsFolder))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] The 'mods' folder does not exist. Use 'init' to create it.");
            return;
        }

        var availableMods = Directory.GetDirectories(modsFolder)
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .Cast<string>()
            .ToList();

        if (availableMods.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No mods found in the 'mods' folder.[/]");
            return;
        }

        List<string> selectedIndices = new();
        if (File.Exists(selectionFile))
        {
            try
            {
                string json = File.ReadAllText(selectionFile);
                selectedIndices = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new();
            }
            catch { /* Ignore corrupt file */ }
        }

        var prompt = new MultiSelectionPrompt<string>()
            .Title("Select [green]mods[/] to run with the server:")
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle a mod, [green]<enter>[/] to accept)[/]")
            .PageSize(10)
            .AddChoices(availableMods);

        foreach (var mod in selectedIndices)
        {
            if (availableMods.Contains(mod))
            {
                prompt.Select(mod);
            }
        }

        var selected = AnsiConsole.Prompt(prompt);

        try
        {
            string json = System.Text.Json.JsonSerializer.Serialize(selected);
            File.WriteAllText(selectionFile, json);
            AnsiConsole.MarkupLine($"[green]Selected {selected.Count} mods.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error saving selection:[/] {ex.Message}");
        }
    }

    private void ExecuteRebuild(string[] args)
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
                selectedMod = null;
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
                    .Title("Select a [green]mod[/] to rebuild:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more mods)[/]")
                    .AddChoices(mods));
        }

        if (!string.IsNullOrEmpty(selectedMod))
        {
            string modPath = Path.Combine(modsFolder, selectedMod);
            BuildUtility.BuildMod(selectedMod, modPath);
        }
    }

    public override IEnumerable<string> GetCompletions(string[] args)
    {
        if (args.Length == 0)
        {
            return new[] { "clone", "open", "select", "ide", "rebuild" };
        }

        if (args.Length == 1)
        {
            string sub = args[0].ToLowerInvariant();
            var subs = new[] { "clone", "open", "select", "ide", "rebuild" };
            
            if (subs.Contains(sub))
            {
                if (sub == "open" || sub == "rebuild")
                {
                    const string modsFolder = "mods";
                    if (Directory.Exists(modsFolder))
                    {
                        return Directory.GetDirectories(modsFolder)
                            .Select(Path.GetFileName)
                            .Where(n => n != null)
                            .Cast<string>();
                    }
                }
                return Enumerable.Empty<string>();
            }

            return subs.Where(s => s.StartsWith(sub, StringComparison.OrdinalIgnoreCase));
        }

        if (args.Length == 2 && (args[0].Equals("open", StringComparison.OrdinalIgnoreCase) || args[0].Equals("rebuild", StringComparison.OrdinalIgnoreCase)))
        {
            const string modsFolder = "mods";
            if (Directory.Exists(modsFolder))
            {
                string query = args[1].ToLowerInvariant();
                return Directory.GetDirectories(modsFolder)
                    .Select(Path.GetFileName)
                    .Where(n => n != null)
                    .Cast<string>()
                    .Where(n => n.StartsWith(query, StringComparison.OrdinalIgnoreCase));
            }
        }

        return Enumerable.Empty<string>();
    }
}
