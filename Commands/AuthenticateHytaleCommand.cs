using System;
using System.Diagnostics;
using System.IO;
using Spectre.Console;

namespace VelvetCLI.Commands;

internal sealed class AuthenticateHytaleCommand : VelvetCommand
{
    public override string Name => "hytale-auth";
    public override string Description => "Authenticates the Hytale server and saves credentials";

    protected override void ExecuteCore(string[] args)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string hytaleHome = Path.Combine(appData, "Hytale");
        string patchline = "release"; // Default to release as per gradle.properties

        string serverJar = Path.Combine(hytaleHome, "install", patchline, "package", "game", "latest", "Server", "HytaleServer.jar");
        string assetsZip = Path.Combine(hytaleHome, "install", patchline, "package", "game", "latest", "Assets.zip");

        // This command assumes it's being run from the context of the Hytale-Example-Project-plugin
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

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("[bold blue]Preparing Hytale Server...[/]", ctx =>
            {
                var process = new Process();
                process.StartInfo.FileName = "java";

                // Include the auth commands here
                var launchArgs = $"-cp \"{serverJar}\" com.hypixel.hytale.Main --allow-op --disable-sentry --assets=\"{assetsZip}\" --boot-command \"auth login device,auth persistence Encrypted\"";

                if (!string.IsNullOrWhiteSpace(pluginPath))
                {
                    launchArgs += $" --mods=\"{pluginPath}\"";
                }
                process.StartInfo.Arguments = launchArgs;
                process.StartInfo.WorkingDirectory = workingDir;

                // Redirect output to capture the auth code
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        CheckForAuthOutput(e.Data, process, ctx);
                    }
                };

                ctx.Status("[bold blue]Starting Hytale Server for Authentication...[/]");

                try
                {
                    if (process.Start())
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        ctx.Status("[yellow]Waiting for authentication code from server...[/]");
                        process.WaitForExit();
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
            });
    }

    private void CheckForAuthOutput(string line, Process process, StatusContext ctx)
    {
        // Check for Auth Code
        var match = System.Text.RegularExpressions.Regex.Match(line, @"Enter code:\s*([A-Za-z0-9]+)");
        if (match.Success)
        {
            var code = match.Groups[1].Value;
            var url = $"https://oauth.accounts.hytale.com/oauth2/device/verify?user_code={code}";

            AnsiConsole.MarkupLine($"[bold green] Detected Auth Code:[/] [bold yellow]{code}[/]");

            ctx.Status($"[bold blue]Waiting for browser verification...[/] (Code: [yellow]{code}[/])");

            AnsiConsole.MarkupLine("[bold blue]Opening browser...[/]");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine("[red]Failed to open browser automatically. Please open the link manually:[/]");
                AnsiConsole.MarkupLine($"[underline blue]{url}[/]");
                Console.WriteLine(); // Add a newline for clarity
            }
        }

        // Check for Auth Success
        if (line.Contains("Authentication successful! Use '/auth status' to view details."))
        {
            AnsiConsole.MarkupLine("[bold green]Authentication successful![/] Saving configuration...");
            try
            {
                process.StandardInput.WriteLine("stop");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to send stop command:[/] {ex.Message}");
            }
        }
    }
}
