using Bunit;
using PrioritizationApp.Models;
using PrioritizationApp.Web.Components.Pages;

namespace PrioritizationApp.Web.Tests.Components;

public class HomeTests : ComponentTestBase
{
    [Fact]
    public void EmptyState_ShowsPrompt()
    {
        RegisterServices();

        var cut = Render<Home>();

        Assert.Contains("No lists yet", cut.Markup);
    }

    [Fact]
    public void ExistingLists_AreRendered()
    {
        var seed = new AppData
        {
            Lists =
            [
                new PriorityList { Id = Guid.NewGuid(), Name = "Groceries" },
                new PriorityList { Id = Guid.NewGuid(), Name = "Movies" }
            ]
        };
        RegisterServices(seed);

        var cut = Render<Home>();

        Assert.Contains("Groceries", cut.Markup);
        Assert.Contains("Movies", cut.Markup);
    }

    [Fact]
    public void TypingName_EnablesCreateButton_WithSingleInteraction()
    {
        // Regression for the "have to click twice" bug: with @bind:event="oninput",
        // a single keystroke updates the bound value and enables the button immediately.
        RegisterServices();
        var cut = Render<Home>();

        var button = cut.Find("button.btn-primary");
        Assert.True(button.HasAttribute("disabled"));

        cut.Find("input.form-control").Input("Groceries");

        Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled"));
    }

    [Fact]
    public void CreateList_AddsListInOneClick()
    {
        RegisterServices();
        var cut = Render<Home>();

        cut.Find("input.form-control").Input("Groceries");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("Groceries", cut.Markup));
        Assert.Single(Repository.Load().Lists);
    }
}
