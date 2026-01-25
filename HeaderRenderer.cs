using System.Text;
using Spectre.Console;

namespace VelvetCLI;

internal static class HeaderRenderer
{   

    // Normalize TitleArt (no leading/trailing blank lines)
    private static readonly string TitleArt = @"'||'  '|'         '||                     .        ..|'''.| '||'      '||' 
 '|.  .'    ....   ||  .... ...   ....  .||.     .|'     '   ||        ||  
  ||  |   .|...||  ||   '|.  |  .|...||  ||      ||          ||        ||  
   |||    ||       ||    '|.|   ||       ||      '|.      .  ||        ||  
    |      '|...' .||.    '|     '|...'  '|.'     ''|....'  .||.....| .||.";

    public static void Render()
    {


        //AnsiConsole.Clear();
        //var rawLines = TitleArt.Trim('\r', '\n');
        ////var panel = MakePanel(rawLines);
        //var title = new Align(new Markup($"[bold red]{rawLines}[/]"), HorizontalAlignment.Center);
        ////var panel = new Panel(title)
        //// .Padding(0,1)
        //// .BorderColor(Color.DarkRed)
        //// .RoundedBorder()
        //// .Expand()
        //// .Header("Ver.0.0.1", Justify.Center);
        //var panel = MakePanel(TitleArt);

        //AnsiConsole.Write(panel);
        Console.WriteLine();
    }

    /// <summary>
    /// Play the startup sequence:
    /// 1) Unfold the ASCII art line-by-line (but keep panel height constant).
    /// 2) Play a left-to-right shine across the completed logo.
    /// Uses Spectre.Console.Live to update in-place and avoid flicker.
    /// Call this once before entering the main loop.
    /// </summary>
    public static void RenderStartupSequence(int unfoldDelayMs = 40, int shineDelayMs = 30, int shineWidth = 10)
    {
        // Trim both ends to ensure no leading/trailing blank lines
        var rawLines = TitleArt.Trim('\r', '\n').Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        int totalLines = rawLines.Length;
        int maxLen = 0;
        foreach (var l in rawLines) maxLen = Math.Max(maxLen, l.Length);

        // Ensure console is cleared once before the live update.
        AnsiConsole.Clear();

        // Start a Live display of the panel and update its target in-place.
        // Use an initial empty target that has the same number of lines (padded).
        var initialMarkup = BuildEmptyMarkup(totalLines, maxLen);
        AnsiConsole.Live(MakePanel(initialMarkup)).Start(ctx =>
        {
            // 1) Unfold line-by-line (but always produce totalLines number of lines)
            for (int visible = 1; visible <= totalLines; visible++)
            {
                var unfoldMarkup = BuildUnfoldMarkup(rawLines, visible, maxLen);
                ctx.UpdateTarget(MakePanel(unfoldMarkup));
                ctx.Refresh();
                Thread.Sleep(unfoldDelayMs);
            }

            // 2) Shine across the completed logo (BuildMarkupForPosition returns full-lines markup)
            int start = -shineWidth;
            int end = maxLen + shineWidth;
            for (int pos = start; pos <= end; pos++)
            {
                string markup = BuildMarkupForPosition(rawLines, pos, shineWidth, maxLen);
                ctx.UpdateTarget(MakePanel(markup));
                ctx.Refresh();
                Thread.Sleep(shineDelayMs);
            }
        });
    }

    private static Panel MakePanel(string markupContent)
    {
        // Avoid null/empty Markup issues; Spectre.Markup tolerates empty strings.
        var title = new Align(new Markup(markupContent ?? string.Empty), HorizontalAlignment.Center);
        return new Panel(title)
            .Padding(0, 1)
            .BorderColor(Color.DarkRed)
            .RoundedBorder()
            .Expand()
            .Header("Ver.0.0.1", Justify.Center);
    }

    // Build markup for unfold: first 'visibleCount' lines are the real text (red),
    // the remaining lines are blank (spaces) so total lines remain constant.
    // Trim trailing newline so every frame has identical baseline.
    private static string BuildUnfoldMarkup(string[] lines, int visibleCount, int maxLen)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            if (i < visibleCount)
            {
                // Render visible line in red, padded to maxLen
                sb.Append("[bold red]");
                sb.Append(lines[i].PadRight(maxLen));
                sb.Append("[/]");
            }
            else
            {
                // Render a blank padded line (no coloring)
                sb.Append(' ', maxLen);
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    // Build a Spectre.Markup string where characters under the shine window are brighter.
    // Always returns the same number of lines (each padded to maxLen).
    // Trim trailing newline so final markup exactly matches other frames.
    private static string BuildMarkupForPosition(string[] lines, int shineCenter, int shineWidth, int maxLen)
    {
        var sb = new StringBuilder();
        int half = Math.Max(1, shineWidth / 2);

        foreach (var rawLine in lines)
        {
            var line = rawLine.PadRight(maxLen);
            for (int col = 0; col < line.Length; col++)
            {
                char c = line[col];

                int distance = Math.Abs(col - shineCenter);
                bool inWindow = distance <= half;

                if (char.IsWhiteSpace(c))
                {
                    sb.Append(' ');
                    continue;
                }

                if (inWindow)
                {
                    double norm = 1.0 - (distance / (double)half); // 1.0 at center, 0 at edge
                    string color = norm > 0.66 ? "white" : norm > 0.33 ? "yellow" : "grey93";
                    sb.Append($"[bold {color}]");
                    sb.Append(c);
                    sb.Append("[/]");
                }
                else
                {
                    sb.Append("[bold red]");
                    sb.Append(c);
                    sb.Append("[/]");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    private static string BuildEmptyMarkup(int totalLines, int width)
    {
        var sb = new StringBuilder();
        string blank = new string(' ', width);
        for (int i = 0; i < totalLines; i++)
        {
            sb.AppendLine(blank);
        }
        return sb.ToString().TrimEnd('\r', '\n');
    }
}
