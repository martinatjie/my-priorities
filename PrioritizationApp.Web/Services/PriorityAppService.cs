using PrioritizationApp.Models;
using PrioritizationApp.Services;

namespace PrioritizationApp.Web.Services;

public class PriorityAppService
{
    private readonly IAppRepository _repository;
    private readonly PrioritizationService _prioritizationService;
    private readonly ComparisonSessionHost _comparisonSession;

    public PriorityAppService(
        IAppRepository repository,
        PrioritizationService prioritizationService,
        ComparisonSessionHost comparisonSession)
    {
        _repository = repository;
        _prioritizationService = prioritizationService;
        _comparisonSession = comparisonSession;
    }

    public AppData Load() => _repository.Load();

    public void Save(AppData data) => _repository.Save(data);

    public PriorityList? GetList(AppData data, Guid listId) =>
        data.Lists.FirstOrDefault(list => list.Id == listId);

    public PriorityList CreateList(AppData data, string name)
    {
        var list = new PriorityList
        {
            Id = Guid.NewGuid(),
            Name = name.Trim()
        };
        data.Lists.Add(list);
        Save(data);
        return list;
    }

    public async Task AddItemAsync(AppData data, PriorityList list, string text)
    {
        var newItem = new Item(Guid.NewGuid(), text.Trim());
        list.Items.Add(newItem);

        if (!data.Settings.AutoPrioritizeOnAdd)
        {
            Save(data);
            return;
        }

        // Auto-prioritize: place the new item into the current ranking.
        // A never-prioritized list has a null ranking; GetRankedItems treats that as empty.
        var existingRanking = RankingHelper.GetRankedItems(list).ToList();

        if (existingRanking.Count == 0)
        {
            // Nothing ranked yet, so the new item becomes the first ranked item (no comparison needed).
            list.RankedItemIds = [newItem.Id];
            Save(data);
            return;
        }

        var result = await _prioritizationService.InsertIntoRankingAsync(
            existingRanking,
            newItem,
            _comparisonSession.PickAsync);

        list.RankedItemIds = result.RankedItemIds;
        Save(data);
    }

    public async Task<string?> PrioritizeAsync(AppData data, PriorityList list, bool confirmFullRerank)
    {
        if (list.Items.Count == 0)
            return "No items to prioritize.";

        if (list.Items.Count == 1)
        {
            list.RankedItemIds = [list.Items[0].Id];
            Save(data);
            return "Only one item — ranking is trivial.";
        }

        if (RankingHelper.IsFullyPrioritized(list))
        {
            if (!confirmFullRerank)
                return null;

            var fullResult = await _prioritizationService.RankItemsAsync(
                list.Items,
                _comparisonSession.PickAsync,
                (current, total) => _comparisonSession.SetProgress($"Ranking item {current} of {total}..."));

            list.RankedItemIds = fullResult.RankedItemIds;
            Save(data);
            return fullResult.Cancelled
                ? "Prioritization cancelled. Progress saved."
                : "Prioritization complete.";
        }

        var unprioritized = RankingHelper.GetUnprioritizedItems(list);
        if (unprioritized.Count == 0)
            return "All items are already prioritized.";

        if (list.RankedItemIds is null)
        {
            var initialResult = await _prioritizationService.RankItemsAsync(
                list.Items,
                _comparisonSession.PickAsync,
                (current, total) => _comparisonSession.SetProgress($"Ranking item {current} of {total}..."));

            list.RankedItemIds = initialResult.RankedItemIds;
            Save(data);
            return initialResult.Cancelled
                ? "Prioritization cancelled. Progress saved."
                : "Prioritization complete.";
        }

        var existingRanking = RankingHelper.GetRankedItems(list).ToList();
        var partialResult = await _prioritizationService.RankUnprioritizedItemsAsync(
            existingRanking,
            unprioritized,
            _comparisonSession.PickAsync,
            (current, total) => _comparisonSession.SetProgress($"Ranking item {current} of {total}..."));

        list.RankedItemIds = partialResult.RankedItemIds;
        Save(data);
        return partialResult.Cancelled
            ? "Prioritization cancelled. Progress saved."
            : "Prioritization complete.";
    }

    public void UpdateItem(AppData data, PriorityList list, Guid itemId, string text)
    {
        var index = list.Items.FindIndex(item => item.Id == itemId);
        if (index < 0)
            return;

        list.Items[index] = list.Items[index] with { Text = text.Trim() };
        Save(data);
    }

    public void DeleteItem(AppData data, PriorityList list, Guid itemId)
    {
        var item = list.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return;

        list.Items.RemoveAll(i => i.Id == itemId);
        if (list.RankedItemIds is not null)
        {
            list.RankedItemIds.Remove(itemId);
            if (list.RankedItemIds.Count == 0)
                list.RankedItemIds = null;
        }

        Save(data);
    }

    public void ToggleAutoPrioritize(AppData data) =>
        data.Settings.AutoPrioritizeOnAdd = !data.Settings.AutoPrioritizeOnAdd;
}
