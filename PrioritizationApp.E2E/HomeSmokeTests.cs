using Microsoft.Playwright;

namespace PrioritizationApp.E2E;

public class HomeSmokeTests(PlaywrightAppFixture fixture) : IClassFixture<PlaywrightAppFixture>
{
    private static async Task WaitForCreateFormInteractiveAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => new Promise(resolve => {
                const input = document.querySelector('input[placeholder="List name"]');
                if (!input) {
                    resolve(false);
                    return;
                }

                const start = Date.now();
                const poll = async () => {
                    input.value = '__probe__';
                    input.dispatchEvent(new Event('input', { bubbles: true }));
                    await new Promise(r => setTimeout(r, 200));

                    const createButton = Array.from(document.querySelectorAll('button.btn-primary'))
                        .find(button => button.textContent?.trim() === 'Create');

                    if (createButton && !createButton.disabled) {
                        input.value = '';
                        input.dispatchEvent(new Event('input', { bubbles: true }));
                        resolve(true);
                        return;
                    }

                    input.value = '';
                    input.dispatchEvent(new Event('input', { bubbles: true }));

                    if (Date.now() - start > 20_000) {
                        resolve(false);
                        return;
                    }

                    setTimeout(poll, 250);
                };

                poll();
            })
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 25_000 });
    }

    [Fact]
    public async Task HomePage_Loads()
    {
        await using var context = await fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/");
        await page.Locator("h1.page-title").WaitForAsync();

        var title = await page.Locator("h1.page-title").TextContentAsync();
        Assert.Contains("My Priorities", title);
    }

    [Fact]
    public async Task CreateList_AppearsOnHome()
    {
        var listName = $"E2E Groceries {Guid.NewGuid():N}";

        await using var context = await fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{fixture.BaseUrl}/");
        await page.Locator("h1.page-title").WaitForAsync();
        await WaitForCreateFormInteractiveAsync(page);

        var input = page.Locator("input[placeholder='List name']");
        var createButton = page.GetByRole(AriaRole.Button, new() { Name = "Create" });

        await input.ClickAsync();
        await page.Keyboard.TypeAsync(listName, new KeyboardTypeOptions { Delay = 75 });
        await Assertions.Expect(createButton).ToBeEnabledAsync(new() { Timeout = 15_000 });
        await createButton.ClickAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = listName }).WaitForAsync(new() { Timeout = 15_000 });
        Assert.Contains(listName, await page.ContentAsync());
    }
}
