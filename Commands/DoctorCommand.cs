using System.Diagnostics;
using System.IO;
using Spectre.Console;

namespace VelvetCLI.Commands;

internal sealed class DoctorCommand : VelvetCommand
{
    public override string Name => "doctor";
    public override IEnumerable<string> Aliases => new[] { "d" };
    public override string Description => "Checks environment, permissions, and workspace health";

    protected override void ExecuteCore(string[] args)
    {
        var results = new List<DoctorResult>
        {
            CheckJava(),
            CheckGit(),
            CheckGradle(),
            CheckHytalePaths(),
        };

        results.AddRange(CheckPermissions());
        results.AddRange(CheckWorkspace());

        RenderResults(results);
    }

    public override void ShowHelp()
    {
        base.ShowHelp();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]Usage:[/] doctor");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Checks Java, Git, Gradle, Hytale server paths, local write permissions, and workspace structure.[/]");
    }

    private static DoctorResult CheckJava()
    {
        return TryRunCommand("java", "--version", out var output, out var error)
            ? DoctorResult.Pass("Java", FirstLine(output, "Java detected"))
            : DoctorResult.Fail("Java", $"Not available. {error}".Trim());
    }

    private static DoctorResult CheckGit()
    {
        return TryRunCommand("git", "--version", out var output, out var error)
            ? DoctorResult.Pass("Git", FirstLine(output, "Git detected"))
            : DoctorResult.Fail("Git", $"Not available. {error}".Trim());
    }

    private static DoctorResult CheckGradle()
    {
        bool hasWrapper = false;
        const string modsFolder = "mods";
        if (Directory.Exists(modsFolder))
        {
            hasWrapper = Directory.GetFiles(modsFolder, "gradlew.bat", SearchOption.AllDirectories).Length > 0;
        }

        bool hasSystemGradle = TryRunCommand("gradle", "--version", out var output, out _);

        if (hasSystemGradle && hasWrapper)
        {
            return DoctorResult.Pass("Gradle", $"{FirstLine(output, "gradle available")} + local gradlew.bat detected");
        }

        if (hasSystemGradle)
        {
            return DoctorResult.Pass("Gradle", FirstLine(output, "System gradle available"));
        }

        if (hasWrapper)
        {
            return DoctorResult.Warn("Gradle", "System gradle missing, but gradlew.bat found in mods.");
        }

        return DoctorResult.Fail("Gradle", "Neither system gradle nor any gradlew.bat wrapper detected.");
    }

    private static DoctorResult CheckHytalePaths()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string hytaleHome = Path.Combine(appData, "Hytale");
        const string patchline = "release";

        string serverJar = Path.Combine(hytaleHome, "install", patchline, "package", "game", "latest", "Server", "HytaleServer.jar");
        string assetsZip = Path.Combine(hytaleHome, "install", patchline, "package", "game", "latest", "Assets.zip");

        bool jarExists = File.Exists(serverJar);
        bool assetsExists = File.Exists(assetsZip);

        if (jarExists && assetsExists)
        {
            return DoctorResult.Pass("Hytale Paths", "HytaleServer.jar and Assets.zip found.");
        }

        if (jarExists || assetsExists)
        {
            return DoctorResult.Warn("Hytale Paths", $"Partial install found. Jar: {jarExists}, Assets: {assetsExists}.");
        }

        return DoctorResult.Fail("Hytale Paths", $"Missing files under expected location: {Path.GetDirectoryName(serverJar)}");
    }

    private static IEnumerable<DoctorResult> CheckPermissions()
    {
        var results = new List<DoctorResult>();
        string cwd = Directory.GetCurrentDirectory();
        results.Add(CheckWritePermission(cwd, "Permissions (workspace)"));

        const string modsFolder = "mods";
        if (Directory.Exists(modsFolder))
        {
            results.Add(CheckWritePermission(Path.GetFullPath(modsFolder), "Permissions (mods)"));
        }
        else
        {
            results.Add(DoctorResult.Warn("Permissions (mods)", "'mods' directory does not exist yet."));
        }

        return results;
    }

    private static IEnumerable<DoctorResult> CheckWorkspace()
    {
        var results = new List<DoctorResult>();
        const string modsFolder = "mods";
        const string selectionFile = "mods/selected_mods.json";

        if (!Directory.Exists(modsFolder))
        {
            results.Add(DoctorResult.Warn("Workspace", "No 'mods' folder found. Run 'init' in your workspace."));
            return results;
        }

        var modDirectories = Directory.GetDirectories(modsFolder).Select(Path.GetFileName).Where(x => x != null).Cast<string>().ToList();
        results.Add(DoctorResult.Pass("Workspace", $"'mods' found with {modDirectories.Count} mod folder(s)."));

        if (!File.Exists(selectionFile))
        {
            results.Add(DoctorResult.Warn("Selected Mods", "No 'mods/selected_mods.json' found. Use 'mod select'."));
            return results;
        }

        try
        {
            string json = File.ReadAllText(selectionFile);
            var selected = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

            var missing = selected.Where(mod => !Directory.Exists(Path.Combine(modsFolder, mod))).ToList();
            if (missing.Count == 0)
            {
                results.Add(DoctorResult.Pass("Selected Mods", $"{selected.Count} selected mod(s), all present."));
            }
            else
            {
                results.Add(DoctorResult.Warn("Selected Mods", $"Missing selected mods: {string.Join(", ", missing)}"));
            }
        }
        catch (Exception ex)
        {
            results.Add(DoctorResult.Fail("Selected Mods", $"Invalid 'selected_mods.json': {ex.Message}"));
        }

        return results;
    }

    private static DoctorResult CheckWritePermission(string directory, string checkName)
    {
        string probeFile = Path.Combine(directory, $".velvet-permission-{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(probeFile, "ok");
            File.Delete(probeFile);
            return DoctorResult.Pass(checkName, $"Writable: {directory}");
        }
        catch (Exception ex)
        {
            return DoctorResult.Fail(checkName, $"Not writable: {directory} ({ex.Message})");
        }
    }

    private static bool TryRunCommand(string fileName, string arguments, out string output, out string error)
    {
        output = "";
        error = "";

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            string stdOut = process.StandardOutput.ReadToEnd();
            string stdErr = process.StandardError.ReadToEnd();

            bool exited = process.WaitForExit(5000);
            if (!exited)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                error = "Timed out";
                return false;
            }

            output = string.IsNullOrWhiteSpace(stdOut) ? stdErr : stdOut;
            error = stdErr;
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string FirstLine(string? text, string fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fallback;
    }

    private static void RenderResults(List<DoctorResult> results)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[bold]Check[/]");
        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]Details[/]");

        foreach (var result in results)
        {
            table.AddRow(
                result.Check,
                result.Status switch
                {
                    DoctorStatus.Pass => "[green]PASS[/]",
                    DoctorStatus.Warn => "[yellow]WARN[/]",
                    _ => "[red]FAIL[/]"
                },
                result.Details);
        }

        AnsiConsole.Write(table);

        int passCount = results.Count(r => r.Status == DoctorStatus.Pass);
        int warnCount = results.Count(r => r.Status == DoctorStatus.Warn);
        int failCount = results.Count(r => r.Status == DoctorStatus.Fail);

        AnsiConsole.MarkupLine($"[bold]Summary:[/] [green]{passCount} pass[/], [yellow]{warnCount} warn[/], [red]{failCount} fail[/]");
    }

    private enum DoctorStatus
    {
        Pass,
        Warn,
        Fail
    }

    private sealed record DoctorResult(string Check, DoctorStatus Status, string Details)
    {
        public static DoctorResult Pass(string check, string details) => new(check, DoctorStatus.Pass, details);
        public static DoctorResult Warn(string check, string details) => new(check, DoctorStatus.Warn, details);
        public static DoctorResult Fail(string check, string details) => new(check, DoctorStatus.Fail, details);
    }
}
