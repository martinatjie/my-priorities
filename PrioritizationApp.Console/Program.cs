using Microsoft.Extensions.Configuration;
using PrioritizationApp.Configuration;
using PrioritizationApp.Services;
using PrioritizationApp.UI;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .AddEnvironmentVariables(prefix: "PRIORITIZATION_")
    .Build();

var dataOptions = configuration.GetSection(DataOptions.SectionName).Get<DataOptions>() ?? new DataOptions();
var dataPath = DataPathResolver.Resolve(dataOptions.FilePath);

Console.WriteLine($"Using data file: {dataPath}");
Console.WriteLine();

var repository = new JsonListRepository(dataPath);
var prioritizationService = new PrioritizationService();
var app = new ConsoleApp(repository, prioritizationService);

app.Run();
