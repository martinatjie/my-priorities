using PrioritizationApp.Models;
using PrioritizationApp.Services;
using PrioritizationApp.Web.Services;
using PrioritizationApp.Web.Tests.TestDoubles;

namespace PrioritizationApp.Web.Tests.Services;

public class PriorityAppServiceTests
{
    private static (PriorityAppService Service, InMemoryAppRepository Repo, ComparisonSessionHost Host) CreateService(AppData? seed = null)
    {
        var repo = new InMemoryAppRepository(seed);
        var host = new ComparisonSessionHost();
        var service = new PriorityAppService(repo, new PrioritizationService(), host);
        return (service, repo, host);
    }

    private static AppData DataWith(bool autoPrioritize, PriorityList list) => new()
    {
        Settings = new AppSettings { AutoPrioritizeOnAdd = autoPrioritize },
        Lists = [list]
    };

    private static PriorityList NewList(string name = "List", params Item[] items) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Items = items.ToList()
    };

    private static Item NewItem(string text) => new(Guid.NewGuid(), text);

    [Fact]
    public void CreateList_TrimsNameAndPersists()
    {
        var (service, repo, _) = CreateService();
        var data = service.Load();

        var list = service.CreateList(data, "  Groceries  ");

        Assert.Equal("Groceries", list.Name);
        Assert.Single(repo.Load().Lists);
        Assert.Equal("Groceries", repo.Load().Lists[0].Name);
    }

    [Fact]
    public async Task AddItem_AutoPrioritizeOff_AddsItemWithoutRanking()
    {
        var list = NewList();
        var data = DataWith(autoPrioritize: false, list);
        var (service, repo, _) = CreateService(data);

        await service.AddItemAsync(data, list, "Milk");

        Assert.Single(list.Items);
        Assert.Null(list.RankedItemIds);
        Assert.Null(repo.Load().Lists[0].RankedItemIds);
    }

    [Fact]
    public async Task AddItem_AutoPrioritizeOn_FirstItemOnNeverRankedList_BecomesRankedWithoutComparison()
    {
        // Regression: a brand-new list has a null ranking. Previously the null guard
        // short-circuited auto-prioritize, so the ON setting appeared to be ignored.
        var list = NewList();
        var data = DataWith(autoPrioritize: true, list);
        var (service, repo, host) = CreateService(data);

        var task = service.AddItemAsync(data, list, "First");

        Assert.True(task.IsCompleted, "First item on an empty ranking should not require a comparison.");
        Assert.False(host.IsActive);
        await task;

        Assert.NotNull(list.RankedItemIds);
        Assert.Equal(new[] { list.Items[0].Id }, list.RankedItemIds!);
        Assert.NotNull(repo.Load().Lists[0].RankedItemIds);
    }

    [Fact]
    public async Task AddItem_AutoPrioritizeOn_SecondItem_TriggersComparisonAndInserts()
    {
        var first = NewItem("First");
        var list = NewList("List", first);
        list.RankedItemIds = [first.Id];
        var data = DataWith(autoPrioritize: true, list);
        var (service, _, host) = CreateService(data);

        var task = service.AddItemAsync(data, list, "Second");

        // A ranked list should now prompt a comparison for the newly added item.
        Assert.True(host.IsActive);
        Assert.NotNull(host.FirstItem);
        Assert.Equal("Second", host.FirstItem!.Text);

        host.Choose(ComparisonOutcome.PreferFirst); // new item wins -> goes to the top
        await task;

        var second = list.Items.Single(i => i.Text == "Second");
        Assert.Equal(new[] { second.Id, first.Id }, list.RankedItemIds!);
    }

    [Fact]
    public async Task AddItem_AutoPrioritizeOn_PreferExisting_InsertsAfter()
    {
        var first = NewItem("First");
        var list = NewList("List", first);
        list.RankedItemIds = [first.Id];
        var data = DataWith(autoPrioritize: true, list);
        var (service, _, host) = CreateService(data);

        var task = service.AddItemAsync(data, list, "Second");
        Assert.True(host.IsActive);
        host.Choose(ComparisonOutcome.PreferSecond); // existing item wins -> new goes below
        await task;

        var second = list.Items.Single(i => i.Text == "Second");
        Assert.Equal(new[] { first.Id, second.Id }, list.RankedItemIds!);
    }

    [Fact]
    public void UpdateItem_ChangesTextAndPersists()
    {
        var item = NewItem("Old");
        var list = NewList("List", item);
        var data = DataWith(autoPrioritize: false, list);
        var (service, repo, _) = CreateService(data);

        service.UpdateItem(data, list, item.Id, "  New  ");

        Assert.Equal("New", list.Items[0].Text);
        Assert.Equal("New", repo.Load().Lists[0].Items[0].Text);
    }

    [Fact]
    public void DeleteItem_RemovesFromRanking()
    {
        var a = NewItem("A");
        var b = NewItem("B");
        var list = NewList("List", a, b);
        list.RankedItemIds = [a.Id, b.Id];
        var data = DataWith(autoPrioritize: false, list);
        var (service, _, _) = CreateService(data);

        service.DeleteItem(data, list, a.Id);

        Assert.Single(list.Items);
        Assert.Equal(new[] { b.Id }, list.RankedItemIds!);
    }

    [Fact]
    public void DeleteItem_LastRankedItem_ResetsRankingToNull()
    {
        var a = NewItem("A");
        var list = NewList("List", a);
        list.RankedItemIds = [a.Id];
        var data = DataWith(autoPrioritize: false, list);
        var (service, _, _) = CreateService(data);

        service.DeleteItem(data, list, a.Id);

        Assert.Empty(list.Items);
        Assert.Null(list.RankedItemIds);
    }

    [Fact]
    public async Task Prioritize_EmptyList_ReturnsMessage()
    {
        var list = NewList();
        var data = DataWith(autoPrioritize: false, list);
        var (service, _, _) = CreateService(data);

        var result = await service.PrioritizeAsync(data, list, confirmFullRerank: false);

        Assert.Equal("No items to prioritize.", result);
    }

    [Fact]
    public async Task Prioritize_SingleItem_IsTrivial()
    {
        var a = NewItem("A");
        var list = NewList("List", a);
        var data = DataWith(autoPrioritize: false, list);
        var (service, _, _) = CreateService(data);

        var result = await service.PrioritizeAsync(data, list, confirmFullRerank: false);

        Assert.Equal("Only one item — ranking is trivial.", result);
        Assert.Equal(new[] { a.Id }, list.RankedItemIds!);
    }

    [Fact]
    public async Task Prioritize_FullyRanked_WithoutConfirm_ReturnsNull()
    {
        var a = NewItem("A");
        var b = NewItem("B");
        var list = NewList("List", a, b);
        list.RankedItemIds = [a.Id, b.Id];
        var data = DataWith(autoPrioritize: false, list);
        var (service, _, _) = CreateService(data);

        var result = await service.PrioritizeAsync(data, list, confirmFullRerank: false);

        Assert.Null(result); // signals the UI to show the re-rank confirmation
    }

    [Fact]
    public void ToggleAutoPrioritize_FlipsSetting()
    {
        var data = new AppData { Settings = new AppSettings { AutoPrioritizeOnAdd = true } };
        var (service, _, _) = CreateService();

        service.ToggleAutoPrioritize(data);

        Assert.False(data.Settings.AutoPrioritizeOnAdd);
    }
}
