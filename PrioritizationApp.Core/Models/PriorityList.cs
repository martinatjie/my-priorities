namespace PrioritizationApp.Models;

public class PriorityList
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<Item> Items { get; set; } = [];
    public List<Guid>? RankedItemIds { get; set; }
}
