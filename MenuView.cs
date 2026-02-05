using Spectre.Console;
using VelvetCLI.Commands;

namespace VelvetCLI;

internal static class MenuView
{
    public static VelvetCommand Show(IReadOnlyList<VelvetCommand> commands)
    {
        int selectedIndex = 0;

        while (true)
        {
            AnsiConsole.Clear();
            Console.WriteLine("=== Command Menu (Use ↑ ↓ and Enter) ===\n");

            for (int i = 0; i < commands.Count; i++)
            {
                VelvetCommand command = commands[i];
                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.White;
                }

                if (command.Aliases.Any())
                {
                    Console.WriteLine($"{command.Name} ({string.Join(", ", command.Aliases)})");
                }
                else
                {
                    Console.WriteLine(command.Name);
                }
                Console.ResetColor();
            }

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = (selectedIndex == 0) ? commands.Count - 1 : selectedIndex - 1;
                    break;
                case ConsoleKey.DownArrow:
                    selectedIndex = (selectedIndex + 1) % commands.Count;
                    break;
                case ConsoleKey.Enter:
                    return commands[selectedIndex];
            }
        }
    }
}
