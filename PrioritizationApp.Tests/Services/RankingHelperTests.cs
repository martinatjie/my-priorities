using PrioritizationApp.Models;
using PrioritizationApp.Services;

namespace PrioritizationApp.Tests.Services;

public class RankingHelperTests
{
    private static Item Item(string text, Guid? id = null) => new(id ?? Guid.NewGuid(), text);

    [Fact]
    public void IsFullyPrioritized_EmptyListWithNullRanking_ReturnsFalse()
    {
        var list = new PriorityList { RankedItemIds = null };
        Assert.False(RankingHelper.IsFullyPrioritized(list));
    }

    [Fact]
    public void IsFullyPrioritized_EmptyListWithEmptyRanking_ReturnsTrue()
    {
        var list = new PriorityList { RankedItemIds = [] };
        Assert.True(RankingHelper.IsFullyPrioritized(list));
    }

    [Fact]
    public void IsFullyPrioritized_AllItemsRanked_ReturnsTrue()
    {
        var a = Item("A");
        var b = Item("B");
        var list = new PriorityList
        {
            Items = [a, b],
            RankedItemIds = [a.Id, b.Id]
        };

        Assert.True(RankingHelper.IsFullyPrioritized(list));
    }

    [Fact]
    public void IsFullyPrioritized_PartialRanking_ReturnsFalse()
    {
        var a = Item("A");
        var b = Item("B");
        var list = new PriorityList
        {
            Items = [a, b],
            RankedItemIds = [a.Id]
        };

        Assert.False(RankingHelper.IsFullyPrioritized(list));
    }

    [Fact]
    public void IsFullyPrioritized_OrphanIdsInRanking_IgnoresOrphans()
    {
        var a = Item("A");
        var list = new PriorityList
        {
            Items = [a],
            RankedItemIds = [a.Id, Guid.NewGuid()]
        };

        Assert.True(RankingHelper.IsFullyPrioritized(list));
    }

    [Fact]
    public void GetUnprioritizedItems_NullRanking_ReturnsAllItems()
    {
        var a = Item("A");
        var b = Item("B");
        var list = new PriorityList { Items = [a, b], RankedItemIds = null };

        var unprioritized = RankingHelper.GetUnprioritizedItems(list);

        Assert.Equal(2, unprioritized.Count);
        Assert.Contains(a, unprioritized);
        Assert.Contains(b, unprioritized);
    }

    [Fact]
    public void GetUnprioritizedItems_PartialRanking_ReturnsOnlyMissing()
    {
        var a = Item("A");
        var b = Item("B");
        var c = Item("C");
        var list = new PriorityList
        {
            Items = [a, b, c],
            RankedItemIds = [a.Id, b.Id]
        };

        var unprioritized = RankingHelper.GetUnprioritizedItems(list);

        Assert.Single(unprioritized);
        Assert.Equal(c.Id, unprioritized[0].Id);
    }

    [Fact]
    public void GetRankedItems_PreservesOrder()
    {
        var a = Item("A");
        var b = Item("B");
        var list = new PriorityList
        {
            Items = [a, b],
            RankedItemIds = [b.Id, a.Id]
        };

        var ranked = RankingHelper.GetRankedItems(list);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(b.Id, ranked[0].Id);
        Assert.Equal(a.Id, ranked[1].Id);
    }

    [Fact]
    public void GetRankedItems_NullRanking_ReturnsEmpty()
    {
        var list = new PriorityList { Items = [Item("A")], RankedItemIds = null };
        Assert.Empty(RankingHelper.GetRankedItems(list));
    }
}
