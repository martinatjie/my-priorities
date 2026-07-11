using PrioritizationApp.Models;

namespace PrioritizationApp.Services;

public class PrioritizationService
{
    public Task<PrioritizationSessionResult> RankItemsAsync(
        IReadOnlyList<Item> items,
        Func<Item, Item, ValueTask<ComparisonOutcome>> pickAsync,
        Action<int, int>? onProgress = null,
        CancellationToken cancellationToken = default) =>
        RankItemsInternal(items, pickAsync, onProgress, cancellationToken);

    public Task<PrioritizationSessionResult> RankUnprioritizedItemsAsync(
        IReadOnlyList<Item> existingRanking,
        IReadOnlyList<Item> itemsToInsert,
        Func<Item, Item, ValueTask<ComparisonOutcome>> pickAsync,
        Action<int, int>? onProgress = null,
        CancellationToken cancellationToken = default) =>
        RankUnprioritizedInternal(existingRanking, itemsToInsert, pickAsync, onProgress, cancellationToken);

    public Task<InsertResult> InsertIntoRankingAsync(
        IReadOnlyList<Item> existingRanking,
        Item newItem,
        Func<Item, Item, ValueTask<ComparisonOutcome>> pickAsync,
        CancellationToken cancellationToken = default) =>
        InsertIntoRankingInternal(existingRanking, newItem, pickAsync, cancellationToken);

    private static async Task<PrioritizationSessionResult> RankItemsInternal(
        IReadOnlyList<Item> items,
        Func<Item, Item, ValueTask<ComparisonOutcome>> pickAsync,
        Action<int, int>? onProgress,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return new PrioritizationSessionResult([], false);

        if (items.Count == 1)
            return new PrioritizationSessionResult([items[0].Id], false);

        var ranking = new List<Item> { items[0] };

        for (var i = 1; i < items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onProgress?.Invoke(i + 1, items.Count);
            var insertResult = await InsertByComparisonAsync(ranking, items[i], pickAsync, cancellationToken);

            if (insertResult.Cancelled)
                return new PrioritizationSessionResult(
                    ranking.Select(item => item.Id).ToList(),
                    true);
        }

        return new PrioritizationSessionResult(
            ranking.Select(item => item.Id).ToList(),
            false);
    }

    private static async Task<PrioritizationSessionResult> RankUnprioritizedInternal(
        IReadOnlyList<Item> existingRanking,
        IReadOnlyList<Item> itemsToInsert,
        Func<Item, Item, ValueTask<ComparisonOutcome>> pickAsync,
        Action<int, int>? onProgress,
        CancellationToken cancellationToken)
    {
        var ranking = existingRanking.ToList();

        for (var i = 0; i < itemsToInsert.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onProgress?.Invoke(i + 1, itemsToInsert.Count);
            var insertResult = await InsertByComparisonAsync(ranking, itemsToInsert[i], pickAsync, cancellationToken);

            if (insertResult.Cancelled)
                return new PrioritizationSessionResult(
                    ranking.Select(item => item.Id).ToList(),
                    true);
        }

        return new PrioritizationSessionResult(
            ranking.Select(item => item.Id).ToList(),
            false);
    }

    private static async Task<InsertResult> InsertIntoRankingInternal(
        IReadOnlyList<Item> existingRanking,
        Item newItem,
        Func<Item, Item, ValueTask<ComparisonOutcome>> pickAsync,
        CancellationToken cancellationToken)
    {
        var ranking = existingRanking.ToList();
        var insertResult = await InsertByComparisonAsync(ranking, newItem, pickAsync, cancellationToken);
        return new InsertResult(
            ranking.Select(item => item.Id).ToList(),
            insertResult.Skipped,
            insertResult.Cancelled);
    }

    private static async Task<InsertResult> InsertByComparisonAsync(
        List<Item> ranking,
        Item newItem,
        Func<Item, Item, ValueTask<ComparisonOutcome>> pickAsync,
        CancellationToken cancellationToken)
    {
        var low = 0;
        var high = ranking.Count;

        while (low < high)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mid = low + (high - low) / 2;
            var compared = ranking[mid];
            var outcome = await pickAsync(newItem, compared);

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
