using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PrioritizationApp.E2E;

public sealed class PrioritiesWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dataDirectory = Path.Combine(
        Path.GetTempPath(),
        "priorities-e2e",
        Guid.NewGuid().ToString("N"));

    private IHost? _kestrelHost;

    public string ServerAddress { get; private set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_dataDirectory);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataDirectory"] = _dataDirectory
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();

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
}
