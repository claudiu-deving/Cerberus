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
        // Parse the connection string to get individual components
        var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString);

        Environment.SetEnvironmentVariable("DATABASE_NAME", connectionStringBuilder.Database);
        Environment.SetEnvironmentVariable("DATABASE_USER", connectionStringBuilder.Username);
        Environment.SetEnvironmentVariable("DATABASE_PASSWORD", connectionStringBuilder.Password);
        Environment.SetEnvironmentVariable("DATABASE_HOST", connectionStringBuilder.Host);
        Environment.SetEnvironmentVariable("DATABASE_PORT", connectionStringBuilder.Port.ToString());
        Environment.SetEnvironmentVariable("BOOTSTRAP_TOKEN", "TEST_BOOTSTRAP_TOKEN_FOR_INTEGRATION_TESTS");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddEnvironmentVariables();
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
