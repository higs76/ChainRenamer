**English** | [Français](README.fr.md)

# ChainRenamer

A batch file renamer built on a **chain of modules**.

> **Status:** active development (0.x), Windows. Preview, renaming with undo, saved chains and the built-in help work; many modules are still to come.

## The idea

Classic batch renamers show all their rules at once, in panels applied **in a fixed order**, each **only once**, and they only show **the final result**. When the result is wrong, you have to guess which rule caused it, and you cannot, say, replace a text *after* changing the case.

ChainRenamer replaces the panels with a **chain**:

- **Each module does one simple thing**: sort, replace a text, change the case… The power comes from chaining them.
- **Each module works on the result of the previous one**, like an assembly line. Stack them in any order, reorder them by drag and drop, and use the same module several times.
- **You see the result of every step.** Click a step to see each name before and after it. A rule that goes wrong is spotted at once.
- **Nothing touches the disk before "Apply renaming".** Conflicts (duplicates, forbidden characters, Windows reserved names, paths too long) are flagged at every step, and an applied renaming can be undone.
- **A chain is a recipe.** Save it in a library and replay it on another folder: the same clean-up for every new season of a series, for example.

### An example

Downloaded episodes are named `speedy.01x03.my.first.time.on.the.circuit!.mkv`. The chain:

| # | Module | Setting | Name after the step |
|---|---|---|---|
| 1 | Source | `\\nas\video\Speedy` | `speedy.01x03.my.first.time.on.the.circuit!.mkv` |
| 2 | Sort | natural order (`1x2` before `1x10`) | *(same name, files in the right order)* |
| 3 | Replace | `.` → space | `speedy 01x03 my first time on the circuit!.mkv` |
| 4 | Replace | `!` → nothing, at the end of the name | `speedy 01x03 my first time on the circuit.mkv` |
| 5 | Replace (advanced) | `(\d+)x(\d+)` → `S$1E$2` | `speedy S01E03 my first time on the circuit.mkv` |

Replace never touches the extension: only an Extension module will. If a rule suits every file but a few, uncheck those files in the step's grid: the step leaves them as they are.

## What works today

- **Source**: local folder or UNC network share, subfolders; unchecking files removes them from the whole chain. Fast selection: header check box, Shift+click, mouse rectangle, Space, right click.
- **Sort**: natural order, ascending or descending, or a manual order by drag and drop.
- **Filter**: keep or remove files by name (text, `*.mkv` pattern, regular expression), extension, size, modification date or name length; the following steps only see the files kept.
- **Replace**: a text by another, matching case or not; all occurrences, the first, the last, at the start or at the end of the name.
- **Replace (advanced)**: regular expressions with groups (`$1`, `$2`…).
- **Case**: lowercase, UPPERCASE, Title or Sentence; small words of English or French titles stay in lowercase; exceptions (`iPhone`, `TV`), Roman numerals, acronyms, glued words.
- **Remove**: the first or last characters, a range of positions, before or after a text, every part between two texts (`[1080p]`).
- **Insert**: a text at the start, at the end, at a position, before or after a text.
- **Numbering**: digits (`001`), letters or Roman numerals, anywhere in the name, starting again in each folder if wanted.
- **Clean**: extra spaces, accents, digits, symbols, emojis, leading dots.
- **Extension**: case, replace, add, remove: the only module that touches the extension.
- **Preview**: Original | Before the step | After the step columns, conflicts in red.
- **Safe renaming**: two phases (handles A↔B swaps, cycles and case-only changes on NTFS and SMB), undoable thanks to a journal.
- **Chain library**: save, save as, import and export of one chain or the whole library, configurable location (a NAS or a synced folder, for example).
- **Favorites**, as in a browser: sorted into folders that become submenus of the Chains menu (Series ▸ Anime); Ctrl+D adds the open chain.
- **Built-in help** (F1, or the **?** of a step): how a chain works, then a page per module with its settings and examples computed by the module itself. Searchable, in English and French.
- **Interface** in English and French, following the Windows language or chosen in **Tools > Settings**.
- **Command line**: `ChainRenamer.Cli` replays a saved chain on a folder, for scripts and scheduled tasks. Preview by default, renaming with `--apply`, undo with `--undo`.

## Planned

- **Modules**: Pad numbers, Dates, Template, Replacement table, Capture (named groups reused as variables).
- **Variables** usable in every module: `{folder}`, `{date}`, later EXIF and audio tags.
- **Drop lists**: paste or drop a list of lines (episode titles…), line N goes to file N.
- **Name segment**: limit a step to part of the name ("after the last ` - `").

## Command line

```
ChainRenamer.Cli --chain <chain> [--subfolders] [--apply] [--library <folder>] <folder>
ChainRenamer.Cli --undo <journal>
```

`<chain>` is a `.chain` file or the name of a chain of the library (the one chosen in the App by default). Without `--apply`, the command only lists what would be renamed, with the conflicts. With it, the files are renamed and the command prints the journal to give to `--undo`. Exit code: 0 done, 1 conflicts or renaming error, 2 wrong arguments, chain or folder.

```
ChainRenamer.Cli --chain Anime --subfolders "\\nas\video\Speedy"
```

## Download

Versions are published in the [Releases](../../releases): a single `ChainRenamer.exe` for Windows x64, .NET included, nothing to install.

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
```

## Project layout

```
src/ChainRenamer.Engine/       the engine: pipeline, modules, chains, renaming (no UI dependency)
src/ChainRenamer.Cli/          command line
src/ChainRenamer.App/          desktop app (Avalonia)
tests/ChainRenamer.Engine.Tests/  engine tests
tests/ChainRenamer.App.Tests/     app tests that need no screen (help, favorites, translations)
docs/etude.md                  the design study (in French)
```

## License

[MIT](LICENSE)
