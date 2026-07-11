using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PrioritizationApp.Models;
using PrioritizationApp.Web.Components;
using PrioritizationApp.Web.Services;

namespace PrioritizationApp.Web.Tests.Components;

public class ComparisonModalTests : BunitContext
{
    private ComparisonSessionHost Register()
    {
        var host = new ComparisonSessionHost();
        Services.AddSingleton(host);
        return host;
    }

    [Fact]
    public void Inactive_RendersNothing()
    {
        Register();

        var cut = Render<ComparisonModal>();

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public async Task ActiveSession_ShowsBothOptions_AndResolvesOnChoice()
    {
        var host = Register();
        var cut = Render<ComparisonModal>();

        var apple = new Item(Guid.NewGuid(), "Apple");
        var banana = new Item(Guid.NewGuid(), "Banana");

        ValueTask<ComparisonOutcome> pick = default;
        await cut.InvokeAsync(() => pick = host.PickAsync(apple, banana));

        cut.WaitForAssertion(() =>
            Assert.Equal(2, cut.FindAll("button.comparison-card").Count));
        Assert.Contains("Apple", cut.Markup);
        Assert.Contains("Banana", cut.Markup);

        cut.FindAll("button.comparison-card")[0].Click();

        var outcome = await pick;
        Assert.Equal(ComparisonOutcome.PreferFirst, outcome);
    }

    [Fact]
    public async Task Cancel_ResolvesWithCancelOutcome()
    {
        var host = Register();
        var cut = Render<ComparisonModal>();

        ValueTask<ComparisonOutcome> pick = default;
        await cut.InvokeAsync(() => pick = host.PickAsync(
            new Item(Guid.NewGuid(), "A"),
            new Item(Guid.NewGuid(), "B")));

        cut.WaitForAssertion(() =>
            Assert.NotEmpty(cut.FindAll("button.comparison-card")));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Cancel").Click();

        Assert.Equal(ComparisonOutcome.Cancel, await pick);
    }
}
