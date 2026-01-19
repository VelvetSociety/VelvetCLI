using System;
using System.IO;

namespace VelvetCLI.Commands
{
    internal class InitialiseWorkspaceCommand : VelvetCommand
    {
        public override string Name => "init";
        public override string Description => "Initialise MODS workspace";

        protected override void ExecuteCore()
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
    }
}
