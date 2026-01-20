using System;
using System.Diagnostics;
using System.IO;
using Spectre.Console;

namespace VelvetCLI.Commands;

internal sealed class AuthenticateHytaleCommand : VelvetCommand
{
    public override string Name => "hytale-auth";
    public override string Description => "Authenticates the Hytale server and saves credentials";

    protected override void ExecuteCore()
    {
        AnsiConsole.MarkupLine("[bold blue]Starting Hytale Server for Authentication...[/]");

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

        var process = new Process();
        process.StartInfo.FileName = "java";
        
        // Include the auth commands here
        var args = $"-cp \"{serverJar}\" com.hypixel.hytale.Main --allow-op --disable-sentry --assets=\"{assetsZip}\" --boot-command \"auth login device,auth persistence Encrypted\"";
        
        if (!string.IsNullOrWhiteSpace(pluginPath))
        {
            args += $" --mods=\"{pluginPath}\"";
        }
        process.StartInfo.Arguments = args;
        process.StartInfo.WorkingDirectory = workingDir;
        
        // Redirect output to capture the auth code
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true; // Enable input redirection

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                Console.WriteLine(e.Data);
                CheckForAuthOutput(e.Data, process);
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                Console.WriteLine(e.Data);
            }
        };

        try
        {
            if (process.Start())
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                AnsiConsole.MarkupLine("[green]Hytale server started for auth...[/]");
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
    }

    private void CheckForAuthOutput(string line, Process process)
    {
        // Check for Auth Code
        var match = System.Text.RegularExpressions.Regex.Match(line, @"Enter code:\s*([A-Za-z0-9]+)");
        if (match.Success)
        {
            var code = match.Groups[1].Value;
            var url = $"https://oauth.accounts.hytale.com/oauth2/device/verify?user_code={code}";
            
            AnsiConsole.MarkupLine($"[bold yellow]Detected Auth Code:[/] {code}");
            AnsiConsole.MarkupLine($"[dim]Opening browser to:[/] {url}");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to open browser:[/] {ex.Message}");
            }
        }

        // Check for Auth Success
        if (line.Contains("Authentication successful! Use '/auth status' to view details."))
        {
            AnsiConsole.MarkupLine("[bold green]Authentication successful![/] Sending stop signal to save config...");
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
