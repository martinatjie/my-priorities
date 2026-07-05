# My Priorities

A simple .NET 8 console app for ranking items through pairwise comparisons. Create multiple independent lists, add or edit items, and prioritize them by repeatedly choosing which of two items matters more.

## Features

- Multiple independent priority lists
- Add, edit, and delete items
- Insertion-style ranking with binary-search comparisons (fewer prompts than comparing every pair)
- Rankings persisted to a JSON file outside the repository
- Partial ranking support — prioritize only new items without re-ranking the full list
- Configurable auto-prioritize-on-add behaviour
- Cancel and skip during comparison sessions

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run

```bash
cd PrioritizationApp.Console
dotnet run
```

On startup the app prints the data file path in use.

## Usage

**Main menu**

| Option | Action |
|--------|--------|
| 1 | Select or create a list |
| 2 | Settings |
| 3 | Exit |

**Settings**

| Option | Action |
|--------|--------|
| 1 | Toggle auto prioritize on add (ON / OFF) |
| 2 | Back |

**List menu**

| Option | Action |
|--------|--------|
| 1 | View items |
| 2 | Add item |
| 3 | Edit item |
| 4 | Delete item |
| 5 | Prioritize (pairwise comparisons) |
| 6 | View ranking |
| 7 | Back |

During prioritization you pick the higher-priority item each time.

**Comparison controls** (shown at the start of each session):

| Input | Action |
|-------|--------|
| `1` / `2` | Pick higher-priority item |
| Enter | Skip — leave current item unprioritized, continue with next |
| `x` | Cancel — save progress so far and return to menu |

**Incremental ranking** (when a list is already partially or fully prioritized):

- **Add** (auto ON) — prompted immediately to place the new item; existing order is preserved
- **Add** (auto OFF) — item is added without prompts; run Prioritize later to rank only unprioritized items
- **Edit** — text changes only; ranking is unchanged
- **Delete** — item is removed from the list and ranking without re-prompting

**Prioritize** (option 5):

- Never ranked — ranks all items
- Partially ranked — ranks only unprioritized items into the existing order
- Fully ranked — warns that all items will be re-ranked; asks for confirmation before proceeding

## Data storage

List data is **not** committed to git. By default, `dotnet run` uses Development settings and stores data at:

```
%LOCALAPPDATA%\PrioritizationApp\prioritization-data.json
```

### Configuration

Settings are loaded in this order (later wins):

1. `PrioritizationApp.Console/appsettings.json`
2. `PrioritizationApp.Console/appsettings.Development.json` (when `DOTNET_ENVIRONMENT=Development`)
3. User secrets (machine-local, not in the repo)
4. Environment variable `PRIORITIZATION_DATA__FILEPATH`

**User secrets example**

```bash
cd PrioritizationApp.Console
dotnet user-secrets set "Data:FilePath" "%LOCALAPPDATA%\PrioritizationApp\prioritization-data.json"
```

See `PrioritizationApp.Console/user-secrets.example.json` for the key format.

**Environment variable example**

```bash
set PRIORITIZATION_DATA__FILEPATH=C:\path\to\prioritization-data.json
dotnet run
```

## Tests

```bash
dotnet test PrioritizationApp.sln
```

### Mutation tests (Stryker)

```bash
dotnet tool install -g dotnet-stryker
dotnet-stryker --config-file stryker-config.json
```

## Project layout

```
PrioritizationApp.Core/     Models, services, configuration, JSON repository
PrioritizationApp.Console/  Console host and menus
PrioritizationApp.Tests/    xUnit tests for Core
```

## License

Personal project — use and modify as you like.
