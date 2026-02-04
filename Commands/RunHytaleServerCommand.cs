using System;
using System.Diagnostics;
using System.IO;
using Spectre.Console;

namespace VelvetCLI.Commands;

internal sealed class RunHytaleServerCommand : VelvetCommand
{
    public override string Name => "server";
    public override string Description => "Runs the Hytale server with the example plugin";

    protected override void ExecuteCore(string[] args)
    {
        bool forceRebuild = args.Length > 0 && string.Equals(args[0], "rebuild", StringComparison.OrdinalIgnoreCase);
        AnsiConsole.MarkupLine("[bold blue]Starting Hytale Server...[/]");

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string hytaleHome = Path.Combine(appData, "Hytale");
        string patchline = "release"; // Default to release as per gradle.properties

        string serverJar = Path.Combine(hytaleHome, "install", patchline, "package", "game", "latest", "Server", "HytaleServer.jar");
        string assetsZip = Path.Combine(hytaleHome, "install", patchline, "package", "game", "latest", "Assets.zip");

        const string selectionFile = "mods/selected_mods.json";
        var selectedMods = new List<string>();

        if (File.Exists(selectionFile))
        {
            try
            {
                string json = File.ReadAllText(selectionFile);
                selectedMods = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new();
            }
            catch { /* Ignore corrupt file */ }
        }

        var modDirs = new List<string>();
        foreach (var modName in selectedMods)
        {
            string? jarPath = EnsureModBuilt(modName, forceRebuild);
            if (jarPath != null)
            {
                string? dirPath = Path.GetDirectoryName(Path.GetFullPath(jarPath));
                if (dirPath != null)
                {
                    modDirs.Add(dirPath);
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Warning:[/] Skipping mod '[yellow]{modName}[/]' because it could not be built.");
            }
        }

        string hytaleModsArg = string.Join(",", modDirs.Distinct());
        string workingDir = "server";

        if (!File.Exists(serverJar))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not find Hytale server JAR at: [yellow]{serverJar}[/]");
            return;
        }

        if (!Directory.Exists(workingDir))
        {
            Directory.CreateDirectory(workingDir);
        }

        var launchArgs = $"-jar \"{serverJar}\" --allow-op --disable-sentry --assets=\"{assetsZip}\"";
        if (!string.IsNullOrWhiteSpace(hytaleModsArg))
        {
            launchArgs += $" --mods=\"{hytaleModsArg}\"";
        }

        var process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        // /K keeps the window open after the command finishes.
        process.StartInfo.Arguments = $"/K \"java {launchArgs}\"";
        process.StartInfo.WorkingDirectory = workingDir;
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.StartInfo.CreateNoWindow = false;

        try
        {
            if (process.Start())
            {
                AnsiConsole.MarkupLine("[green]Hytale server started successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Failed to start Hytale server process.[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error starting server:[/] {ex.Message}");
        }
    }

    private string? EnsureModBuilt(string modName, bool forceRebuild)
    {
        string modPath = Path.Combine("mods", modName);
        string libsDir = Path.Combine(modPath, "build", "libs");

        string? FindMainJar()
        {
            if (!Directory.Exists(libsDir)) return null;
            return Directory.GetFiles(libsDir, "*.jar")
                .FirstOrDefault(f => !f.EndsWith("-sources.jar") && !f.EndsWith("-javadoc.jar"));
        }

        string? IsolateJar(string jarPath)
        {
            // Isolate the JAR into a 'dist' folder to avoid Hytale loading -sources and -javadoc
            string distDir = Path.Combine(modPath, "build", "dist");
            if (!Directory.Exists(distDir)) Directory.CreateDirectory(distDir);

            // Clean dist dir
            foreach (var file in Directory.GetFiles(distDir, "*.jar")) File.Delete(file);

            string destPath = Path.Combine(distDir, Path.GetFileName(jarPath));
            File.Copy(jarPath, destPath, true);
            return destPath;
        }

        var existingJar = FindMainJar();
        if (existingJar != null && !forceRebuild) return IsolateJar(existingJar);

        // Build
        if (BuildUtility.BuildMod(modName, modPath))
        {
            var builtJar = FindMainJar();
            return builtJar != null ? IsolateJar(builtJar) : null;
        }

        return null;
    }
    
    public override IEnumerable<string> GetCompletions(string[] args)
    {
        if (args.Length == 0)
        {
            return new[] { "rebuild" };
        }
        return Enumerable.Empty<string>();
    }

    public override void ShowHelp()
    {
        base.ShowHelp();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]Usage:[/] server [[rebuild]]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Note: This command will automatically build any selected mods that are missing compiled JARs before starting the server. Use 'rebuild' to force a rebuild of all selected mods.[/]");
    }
}
