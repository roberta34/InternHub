using InternHub.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InternHub.Tests.Integration.Infrastructure;

public sealed class InternHubSqliteFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;
    private readonly TimeProvider _timeProvider;
    private readonly bool _useSeparateConnections;
    private readonly string? _sharedMemoryConnectionString;
    private readonly Action<IServiceCollection>? _configureTestServices;

    public InternHubSqliteFactory(
        TimeProvider? timeProvider = null,
        bool useSeparateConnections = false,
        Action<IServiceCollection>? configureTestServices = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _useSeparateConnections = useSeparateConnections;
        _configureTestServices = configureTestServices;
        _sharedMemoryConnectionString = useSeparateConnections
            ? new SqliteConnectionStringBuilder
            {
                DataSource = $"InternHub-{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                DefaultTimeout = 1,
                Pooling = false
            }.ToString()
            : null;
        _connection = new SqliteConnection(
            _sharedMemoryConnectionString ?? "Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<InternHubDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<InternHubDbContext>>();
            services.AddDbContext<InternHubDbContext>(options =>
            {
                if (_useSeparateConnections)
                    options.UseSqlite(_sharedMemoryConnectionString!);
                else
                    options.UseSqlite(_connection);
            });
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(_timeProvider);
            _configureTestServices?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
}
