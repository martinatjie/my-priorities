using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PrioritizationApp.Models;
using PrioritizationApp.Services;
using PrioritizationApp.Web.Services;
using PrioritizationApp.Web.Tests.TestDoubles;

namespace PrioritizationApp.Web.Tests.Components;

public abstract class ComponentTestBase : BunitContext
{
    protected InMemoryAppRepository Repository { get; private set; } = new();
    protected ComparisonSessionHost ComparisonHost { get; } = new();

    protected void RegisterServices(AppData? seed = null)
    {
        Repository = new InMemoryAppRepository(seed);
        Services.AddSingleton<IAppRepository>(Repository);
        Services.AddSingleton(ComparisonHost);
        Services.AddSingleton<PrioritizationService>();
        Services.AddSingleton<PriorityAppService>();
    }
}
