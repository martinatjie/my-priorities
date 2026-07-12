using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PrioritizationApp.E2E;

public sealed class PrioritiesWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string DataDirectoryVariable = "Storage__DataDirectory";

    private readonly string _dataDirectory = Path.Combine(
        Path.GetTempPath(),
        "priorities-e2e",
        Guid.NewGuid().ToString("N"));

    private IHost? _kestrelHost;
    private string? _previousDataDirectory;

    public string ServerAddress { get; private set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_dataDirectory);

        builder.UseEnvironment("Development");
        OverrideDataDirectory(_dataDirectory);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.Sources.Add(new MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string?>
                {
                    ["Storage:DataDirectory"] = _dataDirectory
                }
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var infrastructureDirectory = Path.Combine(_dataDirectory, "infrastructure");
        Directory.CreateDirectory(infrastructureDirectory);

        OverrideDataDirectory(infrastructureDirectory);
        var testHost = builder.Build();

        OverrideDataDirectory(_dataDirectory);
        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var server = _kestrelHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        ServerAddress = addresses!.Addresses.First().TrimEnd('/');

        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_kestrelHost is not null)
            {
                _kestrelHost.StopAsync().GetAwaiter().GetResult();
                _kestrelHost.Dispose();
            }

            RestoreDataDirectory();

            if (Directory.Exists(_dataDirectory))
            {
                try
                {
                    Directory.Delete(_dataDirectory, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup for temp E2E data.
                }
            }
        }

        base.Dispose(disposing);
    }

    private void OverrideDataDirectory(string path)
    {
        _previousDataDirectory ??= Environment.GetEnvironmentVariable(DataDirectoryVariable);
        Environment.SetEnvironmentVariable(DataDirectoryVariable, path);
    }

    private void RestoreDataDirectory()
    {
        Environment.SetEnvironmentVariable(DataDirectoryVariable, _previousDataDirectory);
    }
}
