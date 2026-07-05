using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PrioritizationApp.Models;
using PrioritizationApp.Services;

namespace PrioritizationApp.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppSettingsRecord> Settings => Set<AppSettingsRecord>();
    public DbSet<PriorityListRecord> Lists => Set<PriorityListRecord>();
    public DbSet<ItemRecord> Items => Set<ItemRecord>();
}

public class AppSettingsRecord
{
    public int Id { get; set; } = 1;
    public bool AutoPrioritizeOnAdd { get; set; } = true;
}

public class PriorityListRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? RankedItemIdsJson { get; set; }
    public List<ItemRecord> Items { get; set; } = [];
}

public class ItemRecord
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public string Text { get; set; } = "";
    public int OrderIndex { get; set; }
    public PriorityListRecord? List { get; set; }
}

public class SqliteAppRepository : IAppRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public SqliteAppRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public AppData Load()
    {
        using var db = _contextFactory.CreateDbContext();
        var settings = db.Settings.FirstOrDefault() ?? new AppSettingsRecord();
        var lists = db.Lists
            .Include(list => list.Items.OrderBy(item => item.OrderIndex))
            .OrderBy(list => list.Name)
            .ToList();

        return new AppData
        {
            Settings = new AppSettings { AutoPrioritizeOnAdd = settings.AutoPrioritizeOnAdd },
            Lists = lists.Select(MapList).ToList()
        };
    }

    public void Save(AppData data)
    {
        using var db = _contextFactory.CreateDbContext();

        var settings = db.Settings.FirstOrDefault();
        if (settings is null)
        {
            settings = new AppSettingsRecord();
            db.Settings.Add(settings);
        }

        settings.AutoPrioritizeOnAdd = data.Settings.AutoPrioritizeOnAdd;

        var existingLists = db.Lists.Include(list => list.Items).ToList();
        db.Lists.RemoveRange(existingLists);
        db.SaveChanges();

        foreach (var list in data.Lists)
        {
            var record = new PriorityListRecord
            {
                Id = list.Id,
                Name = list.Name,
                RankedItemIdsJson = list.RankedItemIds is null
                    ? null
                    : JsonSerializer.Serialize(list.RankedItemIds, JsonOptions),
                Items = list.Items.Select((item, index) => new ItemRecord
                {
                    Id = item.Id,
                    ListId = list.Id,
                    Text = item.Text,
                    OrderIndex = index
                }).ToList()
            };
            db.Lists.Add(record);
        }

        db.SaveChanges();
    }

    private static PriorityList MapList(PriorityListRecord record) =>
        new()
        {
            Id = record.Id,
            Name = record.Name,
            Items = record.Items.OrderBy(item => item.OrderIndex).Select(item => new Item(item.Id, item.Text)).ToList(),
            RankedItemIds = string.IsNullOrWhiteSpace(record.RankedItemIdsJson)
                ? null
                : JsonSerializer.Deserialize<List<Guid>>(record.RankedItemIdsJson) ?? null
        };
}
