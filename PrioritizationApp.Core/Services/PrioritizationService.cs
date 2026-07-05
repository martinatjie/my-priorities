using PrioritizationApp.Models;

namespace PrioritizationApp.Services;

public class PrioritizationService
{
    public PrioritizationSessionResult RankItems(
        IReadOnlyList<Item> items,
        Func<Item, Item, ComparisonOutcome> pick,
        Action<int, int>? onProgress = null)
    {
        if (items.Count == 0)
            return new PrioritizationSessionResult([], false);

        if (items.Count == 1)
            return new PrioritizationSessionResult([items[0].Id], false);

        var ranking = new List<Item> { items[0] };

        for (var i = 1; i < items.Count; i++)
        {
            onProgress?.Invoke(i + 1, items.Count);
            var insertResult = InsertByComparison(ranking, items[i], pick);

            if (insertResult.Cancelled)
                return new PrioritizationSessionResult(
                    ranking.Select(item => item.Id).ToList(),
                    true);
        }

        return new PrioritizationSessionResult(
            ranking.Select(item => item.Id).ToList(),
            false);
    }

    public PrioritizationSessionResult RankUnprioritizedItems(
        IReadOnlyList<Item> existingRanking,
        IReadOnlyList<Item> itemsToInsert,
        Func<Item, Item, ComparisonOutcome> pick,
        Action<int, int>? onProgress = null)
    {
        var ranking = existingRanking.ToList();

        for (var i = 0; i < itemsToInsert.Count; i++)
        {
            onProgress?.Invoke(i + 1, itemsToInsert.Count);
            var insertResult = InsertByComparison(ranking, itemsToInsert[i], pick);

            if (insertResult.Cancelled)
                return new PrioritizationSessionResult(
                    ranking.Select(item => item.Id).ToList(),
                    true);

            // Skipped items are left out of ranking; continue to next
        }

        return new PrioritizationSessionResult(
            ranking.Select(item => item.Id).ToList(),
            false);
    }

    public InsertResult InsertIntoRanking(
        IReadOnlyList<Item> existingRanking,
        Item newItem,
        Func<Item, Item, ComparisonOutcome> pick)
    {
        var ranking = existingRanking.ToList();
        var insertResult = InsertByComparison(ranking, newItem, pick);
        return new InsertResult(
            ranking.Select(item => item.Id).ToList(),
            insertResult.Skipped,
            insertResult.Cancelled);
    }

    private static InsertResult InsertByComparison(
        List<Item> ranking,
        Item newItem,
        Func<Item, Item, ComparisonOutcome> pick)
    {
        var low = 0;
        var high = ranking.Count;

        while (low < high)
        {
            var mid = low + (high - low) / 2;
            var compared = ranking[mid];
            var outcome = pick(newItem, compared);

            switch (outcome)
            {
                case ComparisonOutcome.PreferFirst:
                    high = mid;
                    break;
                case ComparisonOutcome.PreferSecond:
                    low = mid + 1;
                    break;
                case ComparisonOutcome.Skip:
                    return new InsertResult(
                        ranking.Select(item => item.Id).ToList(),
                        Skipped: true,
                        Cancelled: false);
                case ComparisonOutcome.Cancel:
                    return new InsertResult(
                        ranking.Select(item => item.Id).ToList(),
                        Skipped: false,
                        Cancelled: true);
            }
        }

        ranking.Insert(low, newItem);
        return new InsertResult(
            ranking.Select(item => item.Id).ToList(),
            Skipped: false,
            Cancelled: false);
    }
}
