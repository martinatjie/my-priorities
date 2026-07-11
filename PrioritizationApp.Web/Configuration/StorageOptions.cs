namespace PrioritizationApp.Web.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string DataDirectory { get; set; } = "/data";
}
