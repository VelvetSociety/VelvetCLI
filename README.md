# VelvetCLI

VelvetCLI is a command-line tool designed to streamline Hytale server management and mod development.

## Features

- **Hytale Server Management**: Easily run and authenticate your Hytale server.
- **Mod Management**: Clone and manage Hytale mods directly from GitHub.
- **Automatic Builds**: Automatically builds selected mods before starting the server.

## Installation

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Java Runtime Environment (JRE)](https://www.java.com/en/download/) (for running the Hytale server)
- [Git](https://git-scm.com/) (for mod cloning)

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/VelvetSociety/VelvetCLI.git
   cd VelvetCLI
   ```
2. Build and run:
   ```bash
   dotnet run -- --help
   ```

## Usage

### Hytale Server Commands

- `auth`: Authenticates the Hytale server and saves credentials.
- `server`: Runs the Hytale server with the example plugin and selected mods. Provide `rebuild` to force a rebuild of selected mods.

### Mod Management Commands

- `mod clone <github-url>`: Clones a mod from GitHub into the `mods` directory.
- `mod list`: Lists all mods currently in the `mods` directory.
- `mod add <mod-name>`: Selects a mod to be included when starting the server.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.