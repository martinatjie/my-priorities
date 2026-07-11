using System.Text.Json;
using PrioritizationApp.Models;

namespace PrioritizationApp.Services;

public class JsonListRepository : IAppRepository
{
    private readonly string _path;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public JsonListRepository(string path)
    {
        _path = path;
    }

    public AppData Load()
    {
        if (!File.Exists(_path))
            return new AppData();

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<AppData>(json, _options) ?? new AppData();
    }

    public void Save(AppData data)
    {
        var json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(_path, json);
    }
}
