using PrioritizationApp.Models;
using PrioritizationApp.Services;

namespace PrioritizationApp.Web.Tests.TestDoubles;

/// <summary>
/// In-memory <see cref="IAppRepository"/> that returns an independent deep copy on each
/// <see cref="Load"/>, mirroring how the real per-request SQLite repository hands out fresh
/// object graphs. This ensures tests exercise the same reload-after-save flow as the app.
/// </summary>
public sealed class InMemoryAppRepository : IAppRepository
{
    private AppData _data;

    public InMemoryAppRepository(AppData? seed = null) => _data = Clone(seed ?? new AppData());

    public AppData Load() => Clone(_data);

    public void Save(AppData data) => _data = Clone(data);

    private static AppData Clone(AppData source) => new()
    {
        Settings = new AppSettings { AutoPrioritizeOnAdd = source.Settings.AutoPrioritizeOnAdd },
        Lists = source.Lists
            .Select(list => new PriorityList
            {
                Id = list.Id,
                Name = list.Name,
                Items = list.Items.Select(item => item with { }).ToList(),
                RankedItemIds = list.RankedItemIds is null ? null : new List<Guid>(list.RankedItemIds)
            })
            .ToList()
    };
}
