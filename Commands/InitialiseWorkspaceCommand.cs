using System;
using System.IO;
using Spectre.Console;

namespace VelvetCLI.Commands
{
    internal class InitialiseWorkspaceCommand : VelvetCommand
    {
        public override string Name => "init";
        public override IEnumerable<string> Aliases => new[] { "i" };
        public override string Description => "Initialise MODS workspace";

        protected override void ExecuteCore(string[] args)
        {
            const string modsFolder = "mods";

            try
            {
                if (Directory.Exists(modsFolder))
                {
                    Console.WriteLine("Initilisation already exists in this folder");
                }
                else
                {
                    Directory.CreateDirectory(modsFolder);
                    Console.WriteLine($"Created '{modsFolder}' directory.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during initialisation: " + ex.Message);
            }
        }
        public override void ShowHelp()
        {
            base.ShowHelp();
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold yellow]Usage:[/] init");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Note: This command creates the 'mods' folder if it doesn't already exist, initializing the current directory as a Velvet workspace.[/]");
        }
    }
}
