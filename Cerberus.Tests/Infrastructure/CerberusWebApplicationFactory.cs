using Cerberus.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cerberus.Tests.Infrastructure;

public class CerberusWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlTestContainer _dbContainer = new();

    public string ConnectionString => _dbContainer.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration with test database connection string
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CerberusDatabase"] = ConnectionString,
                ["Cerberus:BootstrapToken"] = "TEST_BOOTSTRAP_TOKEN_FOR_INTEGRATION_TESTS"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Additional test-specific service configuration can go here
        });
    }


   public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    public ValueTask InitializeAsync()
    {
       return new ValueTask(  _dbContainer.InitializeAsync(TestContext.Current.CancellationToken));
    }
}
