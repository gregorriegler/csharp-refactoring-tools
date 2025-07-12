# C# Refactoring Tools

A collection of Roslyn-based tools for analyzing and refactoring C# code, designed to be used in a simple, non-interactive way. For example on the CLI. It's written by agents for agents.

## Build the Project

```bash
dotnet build refactoring-tools.sln
```

### Build and Run Tests

```bash
./test.sh
```

## Usage

### RoslynAnalysis Tool

List available analysis tools:

```bash
dotnet run --project RoslynAnalysis/RoslynAnalysis.csproj --list-tools
```

Analyze a C# project or solution:

```bash
dotnet run --project RoslynAnalysis/RoslynAnalysis.csproj <analysis-name> <project-path> [file-name] [analysis-args...]
```

### Basic Scripts

The project includes several utility scripts in the root folder:

#### `approve.sh` - Approve Test Results

Approves test results by copying `.received.txt` files to `.verified.txt` files (used with Verify testing framework):

```bash
# Approve all test results
./approve.sh

# Approve specific test results by pattern
./approve.sh RenameSymbol
./approve.sh CanRenameUnusedLocal

# Show help
./approve.sh --help
```

#### `revert.sh` - Revert Changes

Reverts all uncommitted changes and cleans the working directory:

```bash
# Revert changes in current directory
./revert.sh

# Revert changes in specific directory
./revert.sh /path/to/directory
```
