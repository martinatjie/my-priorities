using PrioritizationApp.Models;

namespace PrioritizationApp.Services;

public class PrioritizationService
{
    public List<Guid> RankItems(
        IReadOnlyList<Item> items,
        Func<Item, Item, Item> pickHigherPriority,
        Action<int, int>? onProgress = null)
    {
        if (items.Count == 0)
            return [];

        if (items.Count == 1)
            return [items[0].Id];

        var ranking = new List<Item> { items[0] };

        for (var i = 1; i < items.Count; i++)
        {
            onProgress?.Invoke(i + 1, items.Count);
            InsertByComparison(ranking, items[i], pickHigherPriority);
        }

        return ranking.Select(item => item.Id).ToList();
    }

    public List<Guid> InsertIntoRanking(
        IReadOnlyList<Item> existingRanking,
        Item newItem,
        Func<Item, Item, Item> pickHigherPriority)
    {
        var ranking = existingRanking.ToList();
        InsertByComparison(ranking, newItem, pickHigherPriority);
        return ranking.Select(item => item.Id).ToList();
    }

    private static void InsertByComparison(
        List<Item> ranking,
        Item newItem,
        Func<Item, Item, Item> pickHigherPriority)
    {
        var low = 0;
        var high = ranking.Count;

        while (low < high)
        {
            var mid = low + (high - low) / 2;
            var compared = ranking[mid];
            var winner = pickHigherPriority(newItem, compared);

            if (winner.Id == newItem.Id)
                high = mid;
            else
                low = mid + 1;
        }

        ranking.Insert(low, newItem);
    }
}
