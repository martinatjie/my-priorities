namespace PrioritizationApp.Models;

public class AppData
{
    public AppSettings Settings { get; set; } = new();
    public List<PriorityList> Lists { get; set; } = [];
}
