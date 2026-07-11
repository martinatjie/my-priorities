using PrioritizationApp.Models;
using PrioritizationApp.Services;

namespace PrioritizationApp.Tests.Services;

public class PrioritizationServiceTests
{
    private readonly PrioritizationService _service = new();

    private static Item Item(string text, Guid? id = null) => new(id ?? Guid.NewGuid(), text);

    private static ValueTask<ComparisonOutcome> PreferFirstAsync(Item a, Item b) =>
        ValueTask.FromResult(ComparisonOutcome.PreferFirst);

    private static ValueTask<ComparisonOutcome> PreferSecondAsync(Item a, Item b) =>
        ValueTask.FromResult(ComparisonOutcome.PreferSecond);

    [Fact]
    public async Task RankItems_EmptyList_ReturnsEmpty()
    {
        var result = await _service.RankItemsAsync([], PreferFirstAsync);
        Assert.Empty(result.RankedItemIds);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public async Task RankItems_SingleItem_ReturnsThatItem()
    {
        var a = Item("A");
        var result = await _service.RankItemsAsync([a], PreferFirstAsync);

        Assert.Equal([a.Id], result.RankedItemIds);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public async Task RankItems_AlwaysPreferFirst_OrdersByInsertionWithFirstWinning()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");

        var result = await _service.RankItemsAsync([a, b, c], PreferFirstAsync);

        Assert.Equal([c.Id, b.Id, a.Id], result.RankedItemIds);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public async Task RankItems_AlwaysPreferSecond_KeepsOriginalOrder()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");

        var result = await _service.RankItemsAsync([a, b, c], PreferSecondAsync);

        Assert.Equal([a.Id, b.Id, c.Id], result.RankedItemIds);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public async Task RankItems_CancelMidBatch_ReturnsPartialRanking()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");
        var callCount = 0;

        ValueTask<ComparisonOutcome> CancelOnSecondInsert(Item x, Item y)
        {
            callCount++;
            if (callCount > 1)
                return ValueTask.FromResult(ComparisonOutcome.Cancel);
            return ValueTask.FromResult(ComparisonOutcome.PreferSecond);
        }

        var result = await _service.RankItemsAsync([a, b, c], CancelOnSecondInsert);

        Assert.True(result.Cancelled);
        Assert.Equal([a.Id, b.Id], result.RankedItemIds);
    }

    [Fact]
    public async Task RankItems_SkipDuringInsert_LeavesItemUnprioritized()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");
        var insertCount = 0;

        ValueTask<ComparisonOutcome> SkipSecondItem(Item x, Item y)
        {
            insertCount++;
            if (insertCount == 1)
                return ValueTask.FromResult(ComparisonOutcome.Skip);
            return ValueTask.FromResult(ComparisonOutcome.PreferSecond);
        }

        var result = await _service.RankItemsAsync([a, b, c], SkipSecondItem);

        Assert.False(result.Cancelled);
        Assert.Equal([a.Id, c.Id], result.RankedItemIds);
        Assert.DoesNotContain(b.Id, result.RankedItemIds);
    }

    [Fact]
    public async Task InsertIntoRanking_PrefersNewItem_InsertsAtFront()
    {
        var a = Item("A");
        var b = Item("B");
        var newItem = Item("New");

        var result = await _service.InsertIntoRankingAsync([a, b], newItem, PreferFirstAsync);

        Assert.False(result.Skipped);
        Assert.False(result.Cancelled);
        Assert.Equal([newItem.Id, a.Id, b.Id], result.RankedItemIds);
    }

    [Fact]
    public async Task InsertIntoRanking_Skip_DoesNotInsertItem()
    {
        var a = Item("A");
        var newItem = Item("New");

        var result = await _service.InsertIntoRankingAsync(
            [a],
            newItem,
            (_, _) => ValueTask.FromResult(ComparisonOutcome.Skip));

        Assert.True(result.Skipped);
        Assert.False(result.Cancelled);
        Assert.Equal([a.Id], result.RankedItemIds);
    }

    [Fact]
    public async Task InsertIntoRanking_Cancel_PreservesExistingRanking()
    {
        var a = Item("A");
        var b = Item("B");
        var newItem = Item("New");

        var result = await _service.InsertIntoRankingAsync(
            [a, b],
            newItem,
            (_, _) => ValueTask.FromResult(ComparisonOutcome.Cancel));

        Assert.True(result.Cancelled);
        Assert.Equal([a.Id, b.Id], result.RankedItemIds);
    }

    [Fact]
    public async Task RankUnprioritizedItems_InsertsIntoExistingRanking_PreservesOrder()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");

        var result = await _service.RankUnprioritizedItemsAsync(
            [a, b],
            [c],
            PreferSecondAsync);

        Assert.False(result.Cancelled);
        Assert.Equal([a.Id, b.Id, c.Id], result.RankedItemIds);
    }

    [Fact]
    public async Task RankUnprioritizedItems_CancelMidSession_PreservesProgress()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");
        var callCount = 0;

        ValueTask<ComparisonOutcome> CancelAfterFirstComparison(Item x, Item y)
        {
            callCount++;
            if (callCount >= 2)
                return ValueTask.FromResult(ComparisonOutcome.Cancel);
            return ValueTask.FromResult(ComparisonOutcome.PreferSecond);
        }

        var result = await _service.RankUnprioritizedItemsAsync(
            [a],
            [b, c],
            CancelAfterFirstComparison);

        Assert.True(result.Cancelled);
        Assert.Equal([a.Id, b.Id], result.RankedItemIds);
    }

    [Fact]
    public async Task RankUnprioritizedItems_Skip_LeavesItemOut()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");

        var result = await _service.RankUnprioritizedItemsAsync(
            [a],
            [b, c],
            (newItem, _) => ValueTask.FromResult(
                newItem.Text == "B" ? ComparisonOutcome.Skip : ComparisonOutcome.PreferSecond));

        Assert.False(result.Cancelled);
        Assert.Equal([a.Id, c.Id], result.RankedItemIds);
    }
}
