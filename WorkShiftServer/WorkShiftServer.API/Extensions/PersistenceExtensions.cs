using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftServer.API.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistenceAndBackgroundProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        var myCnn = configuration.GetConnectionString("MyCnn")
            ?? throw new InvalidOperationException("Missing MyCnn connection string.");
        // The Supabase pooler allows only 15 session clients: bound client-side pooling
        // and Hangfire workers so background jobs cannot starve web requests or hit EMAXCONNSESSION.
        var efConnBuilder = new Npgsql.NpgsqlConnectionStringBuilder(myCnn)
        {
            MaxPoolSize = 8,
            MinPoolSize = 0,
            ConnectionIdleLifetime = 5,
            ConnectionPruningInterval = 2,
            Timeout = 15,
            CommandTimeout = 30
        };
        var pooledConnectionString = efConnBuilder.ToString();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(pooledConnectionString);
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        var hangfireRaw = configuration.GetConnectionString("Hangfire") ?? myCnn;
        var hangfireConnBuilder = new Npgsql.NpgsqlConnectionStringBuilder(hangfireRaw)
        {
            MaxPoolSize = 3,
            MinPoolSize = 0,
            ConnectionIdleLifetime = 5,
            ConnectionPruningInterval = 2,
            Timeout = 30,
            CommandTimeout = 30
        };
        var hangfireConnection = hangfireConnBuilder.ToString();
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnection), new PostgreSqlStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1)
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
        });

        return services;
    }
}
