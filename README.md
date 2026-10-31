**English** | [Français](README.fr.md)

# ChainRenamer

A batch file renamer built on a **chain of modules**.

Classic batch renamers apply their rules in a fixed order, each rule once, and only show the final result. ChainRenamer lets you stack modules in any order, use the same module several times, and preview the output of **every step** before anything touches the disk. A chain can be saved and replayed later on new files, from the app or the command line.

> **Status:** early development. Nothing usable yet.

## Planned features

- **List modules** decide which files are included and in what order: Source (local folders and UNC network shares), Filter, Sort (natural sort, date, size…).
- **Name modules** change the name: Replace, Replace (regex), Case, Remove, Insert, Numbering, Extension, Capture (regex named groups reused as variables).
- **Drop lists**: paste a list of lines (for example episode titles), and line N goes to file N.
- **Step-by-step preview**: Original | Before | After, with changes highlighted and conflicts flagged (duplicates, forbidden characters, reserved names, path length).
- **Safe renaming**: two-phase renaming (handles swaps, cycles and case-only changes), and an undo log.
- **Command line**: replay a saved `.chain` file on a folder, for scripts and scheduled tasks.
- **Interface** in English and French.

## Built with

- C# / .NET 10
- [Avalonia UI](https://avaloniaui.net/) 12 with the Fluent theme
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- xUnit

## Build and run

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build
dotnet test
dotnet run --project src/ChainRenamer.App
dotnet run --project src/ChainRenamer.Cli -- --chain example.chain <folder>
```

## Project layout

```
src/ChainRenamer.Engine/       the engine: pipeline, modules, renaming (no UI dependency)
src/ChainRenamer.Cli/          command line
src/ChainRenamer.App/          desktop app (Avalonia)
tests/ChainRenamer.Engine.Tests/
```

## License

[MIT](LICENSE)
