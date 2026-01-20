using System;
using System.Diagnostics;
using System.IO;
using Spectre.Console;

namespace VelvetCLI.Commands;

internal sealed class RunHytaleServerCommand : VelvetCommand
{
    public override string Name => "hytale-server";
    public override string Description => "Runs the Hytale server with the example plugin";

    protected override void ExecuteCore()
    {
        AnsiConsole.MarkupLine("[bold blue]Starting Hytale Server...[/]");

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string hytaleHome = Path.Combine(appData, "Hytale");
        string patchline = "release"; // Default to release as per gradle.properties

        string serverJar = Path.Combine(hytaleHome, "install", patchline, "package", "game", "latest", "Server", "HytaleServer.jar");
        string assetsZip = Path.Combine(hytaleHome, "install", patchline, "package", "game", "latest", "Assets.zip");

        // This command assumes it's being run from the context of the Hytale-Example-Project-plugin
        // For a generic implementation, we might want to ask for the plugin path or detect it.
        // Assuming the plugin path is the one from the research.
        string pluginPath = @"";
        string workingDir = Path.Combine(pluginPath, "run");

        if (!File.Exists(serverJar))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not find Hytale server JAR at: [yellow]{serverJar}[/]");
            return;
        }

        if (!Directory.Exists(workingDir))
        {
            Directory.CreateDirectory(workingDir);
        }

        var process = new Process();
        process.StartInfo.FileName = "java";
        process.StartInfo.Arguments = $"-cp \"{serverJar}\" com.hypixel.hytale.Main --allow-op --disable-sentry --assets=\"{assetsZip}\" --mods=\"{pluginPath}\"";
        process.StartInfo.WorkingDirectory = workingDir;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.StartInfo.CreateNoWindow = false;

        try
        {
            if (process.Start())
            {
                AnsiConsole.MarkupLine("[green]Hytale server started successfully![/]");
                // We don't wait for exit here because we want the CLI to remain interactive if needed,
                // or we could block if that's the desired behavior.
                // In VelvetCLI, commands seem to run and finish.
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
}
