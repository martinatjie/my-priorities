# My Priorities

A simple .NET 8 console app for ranking items through pairwise comparisons. Create multiple independent lists, add or edit items, and prioritize them by repeatedly choosing which of two items matters more.

## Features

- Multiple independent priority lists
- Add, edit, and delete items
- Insertion-style ranking with binary-search comparisons (fewer prompts than comparing every pair)
- Rankings persisted to a JSON file outside the repository

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run

```bash
cd PrioritizationApp
dotnet run
```

On startup the app prints the data file path in use.

## Usage

**Main menu**

| Option | Action |
|--------|--------|
| 1 | Select or create a list |
| 2 | Exit |

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

**Incremental ranking** (when a list is already prioritized):

- **Add** — you are prompted immediately to place the new item via binary-search comparisons; existing order is preserved
- **Edit** — text changes only; ranking is unchanged
- **Delete** — item is removed from the list and ranking without re-prompting

Use **Prioritize** (option 5) for a full re-rank of all items, or when a list has never been prioritized.

## Data storage

List data is **not** committed to git. By default, `dotnet run` uses Development settings and stores data at:

```
%LOCALAPPDATA%\PrioritizationApp\prioritization-data.json
```

### Configuration

Settings are loaded in this order (later wins):

1. `PrioritizationApp/appsettings.json`
2. `PrioritizationApp/appsettings.Development.json` (when `DOTNET_ENVIRONMENT=Development`)
3. User secrets (machine-local, not in the repo)
4. Environment variable `PRIORITIZATION_DATA__FILEPATH`

**User secrets example**

```bash
cd PrioritizationApp
dotnet user-secrets set "Data:FilePath" "%LOCALAPPDATA%\PrioritizationApp\prioritization-data.json"
```

See `PrioritizationApp/user-secrets.example.json` for the key format.

**Environment variable example**

```bash
set PRIORITIZATION_DATA__FILEPATH=C:\path\to\prioritization-data.json
dotnet run
```

## Project layout

```
PrioritizationApp/
  Models/           Item, PriorityList, AppData
  Services/         JSON persistence, prioritization logic
  UI/               Console menus
  Configuration/    Data path resolution
```

## License

Personal project — use and modify as you like.
