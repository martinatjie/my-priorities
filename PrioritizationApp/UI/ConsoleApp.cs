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
            Console.WriteLine("  2. Exit");
            Console.Write("> ");

            var choice = ReadIntOrNull();
            Console.WriteLine();

            switch (choice)
            {
                case 1:
                    SelectOrCreateList();
                    break;
                case 2:
                    Console.WriteLine("Goodbye.");
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

        if (list.RankedItemIds is null)
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

        Console.WriteLine("Placing new item in existing ranking...");
        Console.WriteLine("For each pair, pick the item with HIGHER priority.");
        Console.WriteLine();

        var itemsById = list.Items.ToDictionary(item => item.Id);
        var existingRanking = list.RankedItemIds
            .Where(itemsById.ContainsKey)
            .Select(id => itemsById[id])
            .ToList();

        list.RankedItemIds = _prioritizationService.InsertIntoRanking(
            existingRanking,
            newItem,
            PickHigherPriority);

        Save();
        Console.WriteLine();
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

        Console.WriteLine("Prioritization session started.");
        Console.WriteLine("For each pair, pick the item with HIGHER priority.");
        Console.WriteLine();

        var rankedIds = _prioritizationService.RankItems(
            list.Items,
            (a, b) => PickHigherPriority(a, b),
            (current, total) => Console.WriteLine($"Ranking item {current} of {total}..."));

        list.RankedItemIds = rankedIds;
        Save();
        Console.WriteLine();
        Console.WriteLine("Prioritization complete.");
        ViewRanking(list);
    }

    private static Item PickHigherPriority(Item a, Item b)
    {
        while (true)
        {
            Console.WriteLine($"Comparing:");
            Console.WriteLine($"  1. {a.Text}");
            Console.WriteLine($"  2. {b.Text}");
            Console.WriteLine("Which is HIGHER priority?");
            Console.Write("> ");

            var choice = ReadIntOrNull();
            Console.WriteLine();

            if (choice == 1)
                return a;
            if (choice == 2)
                return b;

            Console.WriteLine("Please enter 1 or 2.");
        }
    }

    private static void ViewRanking(PriorityList list)
    {
        if (list.RankedItemIds is null)
        {
            Console.WriteLine("List changed — run Prioritize again.");
            Console.WriteLine();
            return;
        }

        if (list.RankedItemIds.Count == 0)
        {
            Console.WriteLine("No ranking available.");
            Console.WriteLine();
            return;
        }

        var itemsById = list.Items.ToDictionary(item => item.Id);
        Console.WriteLine("Ranking (highest to lowest priority):");

        for (var i = 0; i < list.RankedItemIds.Count; i++)
        {
            var id = list.RankedItemIds[i];
            if (itemsById.TryGetValue(id, out var item))
                Console.WriteLine($"  {i + 1}. {item.Text}");
            else
                Console.WriteLine($"  {i + 1}. (missing item)");
        }

        Console.WriteLine();
    }

    private void Save() => _repository.Save(_data);

    private static int? ReadIntOrNull()
    {
        var input = Console.ReadLine()?.Trim();
        return int.TryParse(input, out var value) ? value : null;
    }
}
