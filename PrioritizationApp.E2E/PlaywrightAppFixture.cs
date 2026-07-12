using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;

namespace PrioritizationApp.E2E;

public sealed class PlaywrightAppFixture : IAsyncLifetime
{
    public PrioritiesWebApplicationFactory Factory { get; private set; } = null!;
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new PrioritiesWebApplicationFactory();
        _ = Factory.CreateClient();
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.DisposeAsync();
        Playwright?.Dispose();
        if (Factory is not null)
            await Factory.DisposeAsync();
    }

    public string BaseUrl => Factory.ServerAddress;
}
