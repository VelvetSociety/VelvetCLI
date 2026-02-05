using System.Text;
using VelvetCLI.Commands;

namespace VelvetCLI;

internal static class InputPalette
{
    private const string PaletteHint = "Type '/' to open the command palette, or press Enter to show the full menu.";

    public static string ReadLineWithPalette(IReadOnlyList<VelvetCommand> commands)
    {
        var buffer = new StringBuilder();
        string ghostText = "";

        // Show initial prompt (hint when buffer is empty)
        RepaintPrompt(buffer.ToString(), ghostText, commands);

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                // If user typed a slash-command manually (e.g. "/list"), show the executed-command bar
                if (buffer.Length > 0 && buffer[0] == '/')
                {
                    ShowExecutedCommand(buffer.ToString().Trim());
                    return buffer.ToString();
                }

                Console.WriteLine();
                return buffer.ToString();
            }

            if (key.Key == ConsoleKey.Tab)
            {
                if (!string.IsNullOrEmpty(ghostText))
                {
                    buffer.Append(ghostText);
                    // Add a space if it's a command name and doesn't have one yet
                    string current = buffer.ToString();
                    if (!current.Contains(" ") && CommandResolver.IsKnownTopLevelToken(commands, current))
                    {
                        buffer.Append(" ");
                    }
                    
                    ghostText = GetGhostText(buffer.ToString(), commands);
                    RepaintPrompt(buffer.ToString(), ghostText, commands);
                }
                continue;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                    ghostText = GetGhostText(buffer.ToString(), commands);
                    RepaintPrompt(buffer.ToString(), ghostText, commands);
                }
                continue;
            }

            if (key.KeyChar == '/' && buffer.Length == 0)
            {
                var selected = ShowCommandPalette(commands);

                if (selected == "__BACKSPACE_CANCELLED__")
                {
                    ghostText = GetGhostText(buffer.ToString(), commands);
                    RepaintPrompt(buffer.ToString(), ghostText, commands);
                    continue;
                }

                if (selected != null)
                {
                    ShowExecutedCommand(selected);
                    return selected;
                }

                ghostText = GetGhostText(buffer.ToString(), commands);
                RepaintPrompt(buffer.ToString(), ghostText, commands);
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                buffer.Append(key.KeyChar);
                ghostText = GetGhostText(buffer.ToString(), commands);
                RepaintPrompt(buffer.ToString(), ghostText, commands);
            }
        }
    }

    private static string GetGhostText(string input, IReadOnlyList<VelvetCommand> commands)
    {
        if (string.IsNullOrEmpty(input)) return "";

        string[] parts = input.Split(' ');
        if (parts.Length == 1)
        {
            // Command name completion
            string partial = parts[0];
            var matched = CommandResolver.GetTopLevelTokens(commands)
                .FirstOrDefault(t => t.StartsWith(partial, StringComparison.OrdinalIgnoreCase));
            if (matched != null && !matched.Equals(partial, StringComparison.OrdinalIgnoreCase))
            {
                return matched.Substring(partial.Length);
            }
        }
        else
        {
            // Subcommand / Argument completion
            var cmdName = parts[0];
            if (CommandResolver.TryResolveTopLevel(commands, new[] { cmdName }, out var command, out var resolvedArgs) && command != null)
            {
                string lastPart = parts.Last();
                string[] rawArgsSoFar = parts.Skip(1).Take(parts.Length - (string.IsNullOrEmpty(lastPart) ? 1 : 2)).ToArray();
                string[] argsSoFar = resolvedArgs.Concat(rawArgsSoFar).ToArray();
                
                // We want completions for the "lastPart"
                var completions = command.GetCompletions(argsSoFar).ToList();
                var bestMatch = completions.FirstOrDefault(s => s.StartsWith(lastPart, StringComparison.OrdinalIgnoreCase));
                
                if (bestMatch != null && !bestMatch.Equals(lastPart, StringComparison.OrdinalIgnoreCase))
                {
                    return bestMatch.Substring(lastPart.Length);
                }
            }
        }

        return "";
    }

    private static string? ShowCommandPalette(IReadOnlyList<VelvetCommand> commands)
    {
        var filtered = commands.ToList();
        int selected = 0;
        var query = new StringBuilder();

        int baseLeft = Console.CursorLeft;
        int baseTop = Console.CursorTop;

        // Palette sizing
        const int paletteItemCount = 8;
        int paletteHeight = 1 + paletteItemCount; // prompt line + items

        // If the palette would overflow the console buffer, render it higher so it fits.
        int maxTopAllowed = Math.Max(0, Console.BufferHeight - paletteHeight);
        int renderTop = Math.Min(baseTop, maxTopAllowed);

        void Render()
        {
            bool originalCursorVisible = Console.CursorVisible;
            Console.CursorVisible = false;

            int maxDisplay = Math.Min(filtered.Count, paletteItemCount);
            int windowWidth = Math.Max(0, Console.WindowWidth);

            // Render the prompt line in-place (do not push a new line)
            Console.SetCursorPosition(0, renderTop);
            // Write the prompt and the command-palette query inline (">/" + query)
            string promptLine = $">/{query}";
            int used = promptLine.Length;
            Console.Write(promptLine);
            // Pad the rest of the line so previous content is cleared
            Console.Write(new string(' ', Math.Max(0, windowWidth - used)));
            
            // Render the items below the prompt line
            for (int i = 0; i < maxDisplay; i++)
            {
                VelvetCommand item = filtered[i];
                string displayText = $"{item.Name} - {item.Description}";
                if (item.Aliases.Any())
                {
                    displayText += $" (aliases: {string.Join(", ", item.Aliases)})";
                }

                Console.SetCursorPosition(0, renderTop + 1 + i);
                Console.BackgroundColor = i == selected ? ConsoleColor.DarkGray : ConsoleColor.Black;

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(item.Name);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" - {item.Description}");
                if (item.Aliases.Any())
                {
                    Console.Write($" (aliases: {string.Join(", ", item.Aliases)})");
                }

                int padding = Math.Max(0, windowWidth - displayText.Length);
                Console.Write(new string(' ', padding));
                Console.ResetColor();
            }

            // Clear any remaining lines in the palette area
            for (int i = maxDisplay; i < paletteItemCount; i++)
            {
                Console.SetCursorPosition(0, renderTop + 1 + i);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write(new string(' ', windowWidth));
                Console.ResetColor();
            }

            // Place caret after the query part (after ">/" which is 2 chars)
            int caretPosition = 2 + query.Length;
            Console.SetCursorPosition(caretPosition, renderTop);

            Console.CursorVisible = originalCursorVisible;
        }

        void ClearPaletteRender()
        {
            bool originalCursorVisible = Console.CursorVisible;
            Console.CursorVisible = false;

            int windowWidth = Math.Max(0, Console.WindowWidth);
            // Clear the prompt line + paletteItemCount palette lines
            for (int i = 0; i < paletteHeight; i++)
            {
                Console.SetCursorPosition(0, renderTop + i);
                Console.Write(new string(' ', windowWidth));
            }

            // Restore cursor to the prompt row (collapse the gap if we rendered above the original)
            Console.SetCursorPosition(baseLeft, renderTop);

            Console.CursorVisible = originalCursorVisible;
        }

        filtered = commands.ToList();
        selected = 0;
        Render();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Escape)
            {
                ClearPaletteRender();
                return null;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (query.Length > 0)
                {
                    query.Length--;
                    filtered = Filter(commands, query.ToString());
                    selected = Math.Min(selected, Math.Max(0, filtered.Count - 1));
                    Render();
                    continue;
                }

                ClearPaletteRender();
                return "__BACKSPACE_CANCELLED__";
            }

            if (key.Key == ConsoleKey.Enter)
            {
                if (filtered.Count > 0)
                {
                    string chosen = filtered[selected].Name;
                    ClearPaletteRender();
                    return chosen;
                }

                ClearPaletteRender();
                return null;
            }

            if (key.Key == ConsoleKey.UpArrow)
            {
                selected = (selected <= 0) ? Math.Max(0, filtered.Count - 1) : selected - 1;
                Render();
                continue;
            }

            if (key.Key == ConsoleKey.DownArrow)
            {
                selected = (selected + 1) % Math.Max(1, filtered.Count);
                Render();
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                query.Append(key.KeyChar);
                filtered = Filter(commands, query.ToString());
                selected = 0;
                Render();
            }
        }
    }

    private static void ShowExecutedCommand(string command)
    {
        // Normalise command and ensure it's visible as a single padded line with a gray background.
        string text = $"> {command}".TrimEnd();
        int windowWidth = Math.Max(0, Console.WindowWidth);

        // Write the bar at current cursor row (overwriting whatever is there), then move to the next row.
        int currentTop = Console.CursorTop;
        Console.SetCursorPosition(0, currentTop);

        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.ForegroundColor = ConsoleColor.White;

        if (text.Length >= windowWidth)
        {
            // Truncate if necessary to avoid exceptions from SetCursorPosition later.
            Console.Write(text.Substring(0, windowWidth));
        }
        else
        {
            Console.Write(text);
            Console.Write(new string(' ', windowWidth - text.Length));
        }

        Console.ResetColor();

        // Move cursor to the next line for command output / further input.
        if (currentTop + 1 < Console.BufferHeight)
        {
            Console.SetCursorPosition(0, currentTop + 1);
        }
        else
        {
            // If at the bottom of buffer, just write a newline.
            Console.WriteLine();
        }
    }

    private static void RepaintPrompt(string current, string ghostText, IReadOnlyList<VelvetCommand> commands)
    {
        bool originalCursorVisible = Console.CursorVisible;
        Console.CursorVisible = false;

        int cursorColumn = 2;
        int top = Console.CursorTop;
        int windowWidth = Math.Max(0, Console.WindowWidth);
        int contentWidth = Math.Max(0, windowWidth - cursorColumn);

        Console.SetCursorPosition(0, top);
        Console.Write("> ");

        if (string.IsNullOrEmpty(current))
        {
            // Show hint text in dim color when there's no user input.
            Console.ForegroundColor = ConsoleColor.DarkGray;
            string hintToShow = PaletteHint;
            if (hintToShow.Length > contentWidth)
            {
                Console.Write(hintToShow.Substring(0, contentWidth));
            }
            else
            {
                Console.Write(hintToShow);
                Console.Write(new string(' ', contentWidth - hintToShow.Length));
            }
            Console.ResetColor();

            // Place caret right after the prompt (ready for input)
            Console.SetCursorPosition(cursorColumn, top);
        }
        else
        {
            // Render user input
            // Basic syntax highlighting: if first part is a valid command, color it green
            string[] parts = current.Split(' ');
            if (parts.Length > 0)
            {
                bool isValidCmd = commands.Any(c => c.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
                isValidCmd = isValidCmd || CommandResolver.IsKnownTopLevelToken(commands, parts[0]);
                if (isValidCmd)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(parts[0]);
                    Console.ResetColor();
                    if (current.Length > parts[0].Length)
                    {
                        Console.Write(current.Substring(parts[0].Length));
                    }
                }
                else
                {
                    Console.Write(current);
                }
            }

            // Render ghost text
            if (!string.IsNullOrEmpty(ghostText))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(ghostText);
                Console.ResetColor();
            }

            // Pad the remainder of the line
            int writtenLength = current.Length + (ghostText?.Length ?? 0);
            if (contentWidth > writtenLength)
            {
                Console.Write(new string(' ', contentWidth - writtenLength));
            }

            // Place caret after user's text (NOT after ghost text)
            int caretCol = Math.Min(cursorColumn + current.Length, Math.Max(0, windowWidth - 1));
            Console.SetCursorPosition(caretCol, top);
        }

        Console.CursorVisible = originalCursorVisible;
    }

    private static List<VelvetCommand> Filter(IReadOnlyList<VelvetCommand> all, string q)
    {
        if (string.IsNullOrEmpty(q))
        {
            return all.ToList();
        }

        return all
            .Where(cmd => cmd.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                          cmd.Aliases.Any(alias => alias.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                          cmd.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
