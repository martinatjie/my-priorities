using PrioritizationApp.Models;

namespace PrioritizationApp.Services;

public static class RankingHelper
{
    public static bool IsFullyPrioritized(PriorityList list)
    {
        if (list.Items.Count == 0)
            return list.RankedItemIds is not null;

        if (list.RankedItemIds is null)
            return false;

        var itemIds = list.Items.Select(item => item.Id).ToHashSet();
        var rankedIds = list.RankedItemIds.Where(itemIds.Contains).ToList();

        return rankedIds.Count == list.Items.Count;
    }

    public static IReadOnlyList<Item> GetUnprioritizedItems(PriorityList list)
    {
        if (list.RankedItemIds is null)
            return list.Items.ToList();

        var rankedSet = list.RankedItemIds.ToHashSet();
        return list.Items.Where(item => !rankedSet.Contains(item.Id)).ToList();
    }

    public static IReadOnlyList<Item> GetRankedItems(PriorityList list)
    {
        if (list.RankedItemIds is null)
            return [];

        var itemsById = list.Items.ToDictionary(item => item.Id);
        return list.RankedItemIds
            .Where(itemsById.ContainsKey)
            .Select(id => itemsById[id])
            .ToList();
    }
}
