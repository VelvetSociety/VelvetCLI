using System.Text;
using VelvetCLI.Commands;

namespace VelvetCLI;

internal static class InputPalette
{
    private const string PaletteHint = "Type '/' to open the command palette, or press Enter to show the full menu.";

    public static string ReadLineWithPalette(IReadOnlyList<VelvetCommand> commands)
    {
        var buffer = new StringBuilder();

        // Show initial prompt (hint when buffer is empty)
        RepaintPrompt(buffer.ToString());

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

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                    RepaintPrompt(buffer.ToString());
                }
                continue;
            }

            if (key.KeyChar == '/' && buffer.Length == 0)
            {
                var selected = ShowCommandPalette(commands);

                if (selected == "__BACKSPACE_CANCELLED__")
                {
                    RepaintPrompt(buffer.ToString());
                    continue;
                }

                if (selected != null)
                {
                    // Show a gray background line indicating the command the user ran,
                    // then return the selected command so the caller can execute it.
                    ShowExecutedCommand(selected);
                    return selected;
                }

                RepaintPrompt(buffer.ToString());
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                buffer.Append(key.KeyChar);
                // Repaint the whole prompt so the hint disappears as soon as the user types.
                RepaintPrompt(buffer.ToString());
            }
        }
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

                Console.SetCursorPosition(0, renderTop + 1 + i);
                Console.BackgroundColor = i == selected ? ConsoleColor.DarkGray : ConsoleColor.Black;

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(item.Name);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" - {item.Description}");

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
        }

        void ClearPaletteRender()
        {
            int windowWidth = Math.Max(0, Console.WindowWidth);
            // Clear the prompt line + paletteItemCount palette lines
            for (int i = 0; i < paletteHeight; i++)
            {
                Console.SetCursorPosition(0, renderTop + i);
                Console.Write(new string(' ', windowWidth));
            }

            // Restore cursor to the prompt row (collapse the gap if we rendered above the original)
            Console.SetCursorPosition(baseLeft, renderTop);
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

    private static void RepaintPrompt(string current)
    {
        int promptColumn = 2;
        int left = promptColumn;
        int top = Console.CursorTop;
        int windowWidth = Math.Max(0, Console.WindowWidth);
        int contentWidth = Math.Max(0, windowWidth - promptColumn);

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
                Console.Write(new string(' ', Math.Max(0, contentWidth - Math.Min(contentWidth, hintToShow.Length))));
            }
            else
            {
                Console.Write(hintToShow);
                Console.Write(new string(' ', contentWidth - hintToShow.Length));
            }
            Console.ResetColor();

            // Place caret right after the prompt (ready for input)
            int caretCol = Math.Min(left, Math.Max(0, windowWidth - 1));
            Console.SetCursorPosition(caretCol, top);
        }
        else
        {
            // Render user input and pad the remainder of the line to clear previous content.
            if (current.Length > contentWidth)
            {
                Console.Write(current.Substring(0, contentWidth));
            }
            else
            {
                Console.Write(current);
                Console.Write(new string(' ', contentWidth - current.Length));
            }

            // Place caret after user's text
            int caretCol = Math.Min(left + current.Length, Math.Max(0, windowWidth - 1));
            Console.SetCursorPosition(caretCol, top);
        }
    }

    private static List<VelvetCommand> Filter(IReadOnlyList<VelvetCommand> all, string q)
    {
        if (string.IsNullOrEmpty(q))
        {
            return all.ToList();
        }

        return all
            .Where(cmd => cmd.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                          cmd.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
