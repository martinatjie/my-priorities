using Bunit;
using PrioritizationApp.Models;
using PrioritizationApp.Web.Components.Pages;

namespace PrioritizationApp.Web.Tests.Components;

public class ListDetailTests : ComponentTestBase
{
    private static Item Item(string text) => new(Guid.NewGuid(), text);

    private static (AppData Data, PriorityList List) SeedWithRanking(bool autoPrioritize)
    {
        var a = Item("Apple");
        var b = Item("Banana");
        var loose = Item("Cherry");
        var list = new PriorityList
        {
            Id = Guid.NewGuid(),
            Name = "Fruit",
            Items = [a, b, loose],
            RankedItemIds = [a.Id, b.Id]
        };
        var data = new AppData
        {
            Settings = new AppSettings { AutoPrioritizeOnAdd = autoPrioritize },
            Lists = [list]
        };
        return (data, list);
    }

    [Fact]
    public void RankedItems_RenderInOrder_InRankedList()
    {
        var (data, list) = SeedWithRanking(autoPrioritize: false);
        RegisterServices(data);

        var cut = Render<ListDetail>(p => p.Add(c => c.ListId, list.Id));

        var ranked = cut.FindAll("ol.ranked-list li");
        Assert.Equal(2, ranked.Count);
        Assert.Contains("Apple", ranked[0].TextContent);
        Assert.Contains("Banana", ranked[1].TextContent);
    }

    [Fact]
    public void UnprioritizedItems_ShownSeparately()
    {
        var (data, list) = SeedWithRanking(autoPrioritize: false);
        RegisterServices(data);

        var cut = Render<ListDetail>(p => p.Add(c => c.ListId, list.Id));

        Assert.Contains("Not yet prioritized", cut.Markup);
        Assert.Contains("Cherry", cut.Markup);
    }

    [Fact]
    public void PrioritizeButton_HasExplanatoryTooltip()
    {
        var (data, list) = SeedWithRanking(autoPrioritize: false);
        RegisterServices(data);

        var cut = Render<ListDetail>(p => p.Add(c => c.ListId, list.Id));

        var prioritize = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Prioritize");
        var title = prioritize.GetAttribute("title");
        Assert.False(string.IsNullOrWhiteSpace(title));
        Assert.Contains("Two scenarios", title);
    }

    [Fact]
    public void PrioritizeButton_DisabledForEmptyList()
    {
        var list = new PriorityList { Id = Guid.NewGuid(), Name = "Empty" };
        var data = new AppData { Lists = [list] };
        RegisterServices(data);

        var cut = Render<ListDetail>(p => p.Add(c => c.ListId, list.Id));

        var prioritize = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Prioritize");
        Assert.True(prioritize.HasAttribute("disabled"));
    }

    [Fact]
    public void AddItem_AutoPrioritizeOff_AppearsUnprioritized_InOneClick()
    {
        var list = new PriorityList { Id = Guid.NewGuid(), Name = "Empty" };
        var data = new AppData
        {
            Settings = new AppSettings { AutoPrioritizeOnAdd = false },
            Lists = [list]
        };
        RegisterServices(data);
        var cut = Render<ListDetail>(p => p.Add(c => c.ListId, list.Id));

        cut.Find("input.form-control").Input("Kiwi");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Not yet prioritized", cut.Markup);
            Assert.Contains("Kiwi", cut.Markup);
        });
    }

    [Fact]
    public void AddItem_AutoPrioritizeOn_EmptyList_ItemBecomesRanked()
    {
        // With auto-prioritize ON, the first item on a never-ranked list is placed
        // directly into the ranking (no comparison needed) and renders in the ranked list.
        var list = new PriorityList { Id = Guid.NewGuid(), Name = "Empty" };
        var data = new AppData
        {
            Settings = new AppSettings { AutoPrioritizeOnAdd = true },
            Lists = [list]
        };
        RegisterServices(data);
        var cut = Render<ListDetail>(p => p.Add(c => c.ListId, list.Id));

        cut.Find("input.form-control").Input("Kiwi");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            var ranked = cut.FindAll("ol.ranked-list li");
            Assert.Single(ranked);
            Assert.Contains("Kiwi", ranked[0].TextContent);
        });
    }

    [Fact]
    public void DeleteItem_RemovesFromRanking()
    {
        var (data, list) = SeedWithRanking(autoPrioritize: false);
        RegisterServices(data);
        var cut = Render<ListDetail>(p => p.Add(c => c.ListId, list.Id));

        var appleRow = cut.FindAll("ol.ranked-list li")
            .Single(li => li.TextContent.Contains("Apple"));
        appleRow.QuerySelector("button.btn-outline-danger")!.Click();

        cut.WaitForAssertion(() =>
        {
            var ranked = cut.FindAll("ol.ranked-list li");
            Assert.Single(ranked);
            Assert.Contains("Banana", ranked[0].TextContent);
        });
    }
}
