using PrioritizationApp.Models;
using PrioritizationApp.Services;

namespace PrioritizationApp.UI;

public class ConsoleApp
{
    private readonly JsonListRepository _repository;
    private readonly PrioritizationService _prioritizationService;
    private AppData _data = new();

    public ConsoleApp(JsonListRepository repository, PrioritizationService prioritizationService)
    {
        _repository = repository;
        _prioritizationService = prioritizationService;
    }

    public void Run()
    {
        _data = _repository.Load();

        Console.WriteLine("=== Prioritization App ===");
        Console.WriteLine();

        while (true)
        {
            Console.WriteLine("Main Menu");
            Console.WriteLine("  1. Select / create list");
            Console.WriteLine("  2. Settings");
            Console.WriteLine("  3. Exit");
            Console.Write("> ");

            var choice = ReadIntOrNull();
            Console.WriteLine();

            switch (choice)
            {
                case 1:
                    SelectOrCreateList();
                    break;
                case 2:
                    RunSettingsMenu();
                    break;
                case 3:
                    Console.WriteLine("Goodbye.");
                    return;
                default:
                    Console.WriteLine("Invalid choice. Try again.");
                    Console.WriteLine();
                    break;
            }
        }
    }

    private void RunSettingsMenu()
    {
        while (true)
        {
            var autoLabel = _data.Settings.AutoPrioritizeOnAdd ? "ON" : "OFF";
            Console.WriteLine("Settings");
            Console.WriteLine($"  1. Auto prioritize on add: {autoLabel}");
            Console.WriteLine("  2. Back");
            Console.Write("> ");

            var choice = ReadIntOrNull();
            Console.WriteLine();

            switch (choice)
            {
                case 1:
                    _data.Settings.AutoPrioritizeOnAdd = !_data.Settings.AutoPrioritizeOnAdd;
                    Save();
                    Console.WriteLine(
                        $"Auto prioritize on add is now {(_data.Settings.AutoPrioritizeOnAdd ? "ON" : "OFF")}.");
                    Console.WriteLine();
                    break;
                case 2:
                    return;
                default:
                    Console.WriteLine("Invalid choice. Try again.");
                    Console.WriteLine();
                    break;
            }
        }
    }

    private void SelectOrCreateList()
    {
        Console.WriteLine("Lists:");
        if (_data.Lists.Count == 0)
        {
            Console.WriteLine("  (none)");
        }
        else
        {
            for (var i = 0; i < _data.Lists.Count; i++)
                Console.WriteLine($"  {i + 1}. {_data.Lists[i].Name}");
        }

        Console.WriteLine($"  {_data.Lists.Count + 1}. Create new list");
        Console.Write("> ");

        var choice = ReadIntOrNull();
        Console.WriteLine();

        if (choice is null or < 1)
        {
            Console.WriteLine("Invalid choice.");
            Console.WriteLine();
            return;
        }

        if (choice == _data.Lists.Count + 1)
        {
            Console.Write("Enter list name: ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Name cannot be empty.");
                Console.WriteLine();
                return;
            }

            var list = new PriorityList
            {
                Id = Guid.NewGuid(),
                Name = name
            };
            _data.Lists.Add(list);
            Save();
            Console.WriteLine($"Created list \"{name}\".");
            Console.WriteLine();
            RunListMenu(list);
            return;
        }

        if (choice > _data.Lists.Count)
        {
            Console.WriteLine("Invalid choice.");
            Console.WriteLine();
            return;
        }

        RunListMenu(_data.Lists[choice.Value - 1]);
    }

    private void RunListMenu(PriorityList list)
    {
        while (true)
        {
            Console.WriteLine($"List: {list.Name}");
            Console.WriteLine("  1. View items");
            Console.WriteLine("  2. Add item");
            Console.WriteLine("  3. Edit item");
            Console.WriteLine("  4. Delete item");
            Console.WriteLine("  5. Prioritize");
            Console.WriteLine("  6. View ranking");
            Console.WriteLine("  7. Back");
            Console.Write("> ");

            var choice = ReadIntOrNull();
            Console.WriteLine();

            switch (choice)
            {
                case 1:
                    ViewItems(list);
                    break;
                case 2:
                    AddItem(list);
                    break;
                case 3:
                    EditItem(list);
                    break;
                case 4:
                    DeleteItem(list);
                    break;
                case 5:
                    Prioritize(list);
                    break;
                case 6:
                    ViewRanking(list);
                    break;
                case 7:
                    return;
                default:
                    Console.WriteLine("Invalid choice. Try again.");
                    Console.WriteLine();
                    break;
            }
        }
    }

    private static void ViewItems(PriorityList list)
    {
        if (list.Items.Count == 0)
        {
            Console.WriteLine("No items in this list.");
        }
        else
        {
            for (var i = 0; i < list.Items.Count; i++)
                Console.WriteLine($"  {i + 1}. {list.Items[i].Text}");
        }

        Console.WriteLine();
    }

    private void AddItem(PriorityList list)
    {
        Console.Write("Enter item text: ");
        var text = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("Text cannot be empty.");
            Console.WriteLine();
            return;
        }

        var newItem = new Item(Guid.NewGuid(), text);
        list.Items.Add(newItem);

        if (!_data.Settings.AutoPrioritizeOnAdd || list.RankedItemIds is null)
        {
            Save();
            Console.WriteLine("Item added.");
            Console.WriteLine();
            return;
        }

        if (list.RankedItemIds.Count == 0)
        {
            list.RankedItemIds = [newItem.Id];
            Save();
            Console.WriteLine("Item added.");
            Console.WriteLine();
            return;
        }

        PrintSessionHints();
        Console.WriteLine("Placing new item in existing ranking...");
        Console.WriteLine();

        var existingRanking = RankingHelper.GetRankedItems(list).ToList();
        var result = _prioritizationService.InsertIntoRanking(
            existingRanking,
            newItem,
            PickComparisonOutcome);

        list.RankedItemIds = result.RankedItemIds;
        Save();

        Console.WriteLine();
        if (result.Cancelled)
            Console.WriteLine("Prioritization cancelled. Progress saved.");
        else if (result.Skipped)
            Console.WriteLine("Skipped — item left unprioritized.");
        else
            Console.WriteLine("Item added and placed in ranking.");

        ViewRanking(list);
    }

    private void EditItem(PriorityList list)
    {
        if (list.Items.Count == 0)
        {
            Console.WriteLine("No items to edit.");
            Console.WriteLine();
            return;
        }

        ViewItems(list);
        Console.Write("Enter item number to edit: ");
        var choice = ReadIntOrNull();
        Console.WriteLine();

        if (choice is null || choice < 1 || choice > list.Items.Count)
        {
            Console.WriteLine("Invalid choice.");
            Console.WriteLine();
            return;
        }

        var item = list.Items[choice.Value - 1];
        Console.Write($"Enter new text (current: {item.Text}): ");
        var text = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("Text cannot be empty.");
            Console.WriteLine();
            return;
        }

        list.Items[choice.Value - 1] = item with { Text = text };
        Save();
        Console.WriteLine("Item updated.");
        Console.WriteLine();
    }

    private void DeleteItem(PriorityList list)
    {
        if (list.Items.Count == 0)
        {
            Console.WriteLine("No items to delete.");
            Console.WriteLine();
            return;
        }

        ViewItems(list);
        Console.Write("Enter item number to delete: ");
        var choice = ReadIntOrNull();
        Console.WriteLine();

        if (choice is null || choice < 1 || choice > list.Items.Count)
        {
            Console.WriteLine("Invalid choice.");
            Console.WriteLine();
            return;
        }

        var removed = list.Items[choice.Value - 1];
        list.Items.RemoveAt(choice.Value - 1);

        if (list.RankedItemIds is not null)
        {
            list.RankedItemIds.Remove(removed.Id);
            if (list.RankedItemIds.Count == 0)
                list.RankedItemIds = null;
        }

        Save();
        Console.WriteLine($"Deleted \"{removed.Text}\".");
        Console.WriteLine();
    }

    private void Prioritize(PriorityList list)
    {
        if (list.Items.Count == 0)
        {
            Console.WriteLine("No items to prioritize.");
            Console.WriteLine();
            return;
        }

        if (list.Items.Count == 1)
        {
            list.RankedItemIds = [list.Items[0].Id];
            Save();
            Console.WriteLine("Only one item — ranking is trivial.");
            Console.WriteLine();
            return;
        }

        if (RankingHelper.IsFullyPrioritized(list))
        {
            Console.WriteLine("This list is already fully prioritized.");
            Console.WriteLine("Running Prioritize again will re-rank ALL items from scratch.");
            Console.Write("Continue? (y/n) ");
            var confirm = Console.ReadLine()?.Trim().ToLowerInvariant();
            Console.WriteLine();

            if (confirm is not "y" and not "yes")
            {
                Console.WriteLine("Prioritization cancelled.");
                Console.WriteLine();
                return;
            }

            RunFullPrioritizationSession(list);
            return;
        }

        var unprioritized = RankingHelper.GetUnprioritizedItems(list);
        if (unprioritized.Count == 0)
        {
            Console.WriteLine("All items are already prioritized.");
            Console.WriteLine();
            return;
        }

        if (list.RankedItemIds is null)
        {
            RunFullPrioritizationSession(list);
            return;
        }

        RunPartialPrioritizationSession(list, unprioritized);
    }

    private void RunFullPrioritizationSession(PriorityList list)
    {
        PrintSessionHints();
        Console.WriteLine("Prioritization session started.");
        Console.WriteLine("For each pair, pick the item with HIGHER priority.");
        Console.WriteLine();

        var result = _prioritizationService.RankItems(
            list.Items,
            PickComparisonOutcome,
            (current, total) => Console.WriteLine($"Ranking item {current} of {total}..."));

        list.RankedItemIds = result.RankedItemIds;
        Save();
        Console.WriteLine();

        if (result.Cancelled)
            Console.WriteLine("Prioritization cancelled. Progress saved.");
        else
            Console.WriteLine("Prioritization complete.");

        ViewRanking(list);
    }

    private void RunPartialPrioritizationSession(PriorityList list, IReadOnlyList<Item> unprioritized)
    {
        PrintSessionHints();
        Console.WriteLine("Prioritization session started.");
        Console.WriteLine($"Placing {unprioritized.Count} unprioritized item(s) into existing ranking.");
        Console.WriteLine();

        var existingRanking = RankingHelper.GetRankedItems(list).ToList();
        var result = _prioritizationService.RankUnprioritizedItems(
            existingRanking,
            unprioritized,
            PickComparisonOutcome,
            (current, total) => Console.WriteLine($"Ranking item {current} of {total}..."));

        list.RankedItemIds = result.RankedItemIds;
        Save();
        Console.WriteLine();

        if (result.Cancelled)
            Console.WriteLine("Prioritization cancelled. Progress saved.");
        else
            Console.WriteLine("Prioritization complete.");

        ViewRanking(list);
    }

    private static void PrintSessionHints()
    {
        Console.WriteLine("Enter = skip current item   |   x = cancel (saves progress so far)");
        Console.WriteLine();
    }

    private static ComparisonOutcome PickComparisonOutcome(Item a, Item b)
    {
        while (true)
        {
            Console.WriteLine("Comparing:");
            Console.WriteLine($"  1. {a.Text}");
            Console.WriteLine($"  2. {b.Text}");
            Console.WriteLine("Which is HIGHER priority? (Enter = skip item, x = cancel)");
            Console.Write("> ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            if (string.IsNullOrEmpty(input))
                return ComparisonOutcome.Skip;

            if (input.Equals("x", StringComparison.OrdinalIgnoreCase))
                return ComparisonOutcome.Cancel;

            if (input == "1")
                return ComparisonOutcome.PreferFirst;

            if (input == "2")
                return ComparisonOutcome.PreferSecond;

            Console.WriteLine("Please enter 1, 2, Enter to skip, or x to cancel.");
        }
    }

    private static void ViewRanking(PriorityList list)
    {
        if (list.RankedItemIds is null)
        {
            Console.WriteLine("No ranking yet — run Prioritize.");
            Console.WriteLine();
            return;
        }

        var rankedItems = RankingHelper.GetRankedItems(list);
        var unprioritized = RankingHelper.GetUnprioritizedItems(list);

        if (rankedItems.Count == 0 && unprioritized.Count == 0)
        {
            Console.WriteLine("No ranking available.");
            Console.WriteLine();
            return;
        }

        if (rankedItems.Count > 0)
        {
            Console.WriteLine("Ranking (highest to lowest priority):");
            for (var i = 0; i < rankedItems.Count; i++)
                Console.WriteLine($"  {i + 1}. {rankedItems[i].Text}");
            Console.WriteLine();
        }

        if (unprioritized.Count > 0)
        {
            Console.WriteLine("Unprioritized items:");
            foreach (var item in unprioritized)
                Console.WriteLine($"  - {item.Text}");
            Console.WriteLine();
        }
    }

    private void Save() => _repository.Save(_data);

    private static int? ReadIntOrNull()
    {
        var input = Console.ReadLine()?.Trim();
        return int.TryParse(input, out var value) ? value : null;
    }
}
