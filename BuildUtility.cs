using System;
using System.Diagnostics;
using Spectre.Console;

namespace VelvetCLI;

internal static class BuildUtility
{
    public static bool BuildMod(string modName, string modPath)
    {
        AnsiConsole.MarkupLine($"[yellow]Building mod:[/] {modName}...");

        // Try gradlew first
        bool success = TryExecuteBuild(modPath, "gradlew.bat", "build");
        
        if (!success)
        {
            AnsiConsole.MarkupLine($"[yellow]Wrapper build failed, trying system 'gradle' fallback for [bold]{modName}[/]...[/]");
            success = TryExecuteBuild(modPath, "gradle", "build");
        }

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Successfully built mod:[/] {modName}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Failed to build mod:[/] {modName} (both wrapper and system gradle failed)");
        }

        return success;
    }

    private static bool TryExecuteBuild(string workingDir, string fileName, string arguments)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C {fileName} {arguments}";
            process.StartInfo.WorkingDirectory = workingDir;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    AnsiConsole.MarkupLine($"[grey]{error.Trim()}[/]");
                }
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[grey]Error executing {fileName}: {ex.Message}[/]");
            return false;
        }
    }
}
