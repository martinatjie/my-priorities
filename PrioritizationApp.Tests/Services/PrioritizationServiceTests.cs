using PrioritizationApp.Models;
using PrioritizationApp.Services;

namespace PrioritizationApp.Tests.Services;

public class PrioritizationServiceTests
{
    private readonly PrioritizationService _service = new();

    private static Item Item(string text, Guid? id = null) => new(id ?? Guid.NewGuid(), text);

    private static ComparisonOutcome PreferFirst(Item a, Item b) => ComparisonOutcome.PreferFirst;

    private static ComparisonOutcome PreferSecond(Item a, Item b) => ComparisonOutcome.PreferSecond;

    [Fact]
    public void RankItems_EmptyList_ReturnsEmpty()
    {
        var result = _service.RankItems([], PreferFirst);
        Assert.Empty(result.RankedItemIds);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public void RankItems_SingleItem_ReturnsThatItem()
    {
        var a = Item("A");
        var result = _service.RankItems([a], PreferFirst);

        Assert.Equal([a.Id], result.RankedItemIds);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public void RankItems_AlwaysPreferFirst_OrdersByInsertionWithFirstWinning()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");

        var result = _service.RankItems([a, b, c], PreferFirst);

        Assert.Equal([c.Id, b.Id, a.Id], result.RankedItemIds);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public void RankItems_AlwaysPreferSecond_KeepsOriginalOrder()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");

        var result = _service.RankItems([a, b, c], PreferSecond);

        Assert.Equal([a.Id, b.Id, c.Id], result.RankedItemIds);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public void RankItems_CancelMidBatch_ReturnsPartialRanking()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");
        var callCount = 0;

        ComparisonOutcome CancelOnSecondInsert(Item x, Item y)
        {
            callCount++;
            if (callCount > 1)
                return ComparisonOutcome.Cancel;
            return ComparisonOutcome.PreferSecond;
        }

        var result = _service.RankItems([a, b, c], CancelOnSecondInsert);

        Assert.True(result.Cancelled);
        Assert.Equal([a.Id, b.Id], result.RankedItemIds);
    }

    [Fact]
    public void RankItems_SkipDuringInsert_LeavesItemUnprioritized()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");
        var insertCount = 0;

        ComparisonOutcome SkipSecondItem(Item x, Item y)
        {
            insertCount++;
            if (insertCount == 1)
                return ComparisonOutcome.Skip;
            return ComparisonOutcome.PreferSecond;
        }

        var result = _service.RankItems([a, b, c], SkipSecondItem);

        Assert.False(result.Cancelled);
        Assert.Equal([a.Id, c.Id], result.RankedItemIds);
        Assert.DoesNotContain(b.Id, result.RankedItemIds);
    }

    [Fact]
    public void InsertIntoRanking_PrefersNewItem_InsertsAtFront()
    {
        var a = Item("A");
        var b = Item("B");
        var newItem = Item("New");

        var result = _service.InsertIntoRanking([a, b], newItem, PreferFirst);

        Assert.False(result.Skipped);
        Assert.False(result.Cancelled);
        Assert.Equal([newItem.Id, a.Id, b.Id], result.RankedItemIds);
    }

    [Fact]
    public void InsertIntoRanking_Skip_DoesNotInsertItem()
    {
        var a = Item("A");
        var newItem = Item("New");

        var result = _service.InsertIntoRanking([a], newItem, (_, _) => ComparisonOutcome.Skip);

        Assert.True(result.Skipped);
        Assert.False(result.Cancelled);
        Assert.Equal([a.Id], result.RankedItemIds);
    }

    [Fact]
    public void InsertIntoRanking_Cancel_PreservesExistingRanking()
    {
        var a = Item("A");
        var b = Item("B");
        var newItem = Item("New");

        var result = _service.InsertIntoRanking([a, b], newItem, (_, _) => ComparisonOutcome.Cancel);

        Assert.True(result.Cancelled);
        Assert.Equal([a.Id, b.Id], result.RankedItemIds);
    }

    [Fact]
    public void RankUnprioritizedItems_InsertsIntoExistingRanking_PreservesOrder()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");

        var result = _service.RankUnprioritizedItems(
            [a, b],
            [c],
            PreferSecond);

        Assert.False(result.Cancelled);
        Assert.Equal([a.Id, b.Id, c.Id], result.RankedItemIds);
    }

    [Fact]
    public void RankUnprioritizedItems_CancelMidSession_PreservesProgress()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");
        var d = Item("D");
        var callCount = 0;

        ComparisonOutcome CancelAfterFirstComparison(Item x, Item y)
        {
            callCount++;
            if (callCount >= 2)
                return ComparisonOutcome.Cancel;
            return ComparisonOutcome.PreferSecond;
        }

        var result = _service.RankUnprioritizedItems(
            [a],
            [b, c],
            CancelAfterFirstComparison);

        Assert.True(result.Cancelled);
        Assert.Equal([a.Id, b.Id], result.RankedItemIds);
    }

    [Fact]
    public void RankUnprioritizedItems_Skip_LeavesItemOut()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");

        var result = _service.RankUnprioritizedItems(
            [a],
            [b, c],
            (newItem, _) => newItem.Text == "B"
                ? ComparisonOutcome.Skip
                : ComparisonOutcome.PreferSecond);

        Assert.False(result.Cancelled);
        Assert.Equal([a.Id, c.Id], result.RankedItemIds);
    }
}
