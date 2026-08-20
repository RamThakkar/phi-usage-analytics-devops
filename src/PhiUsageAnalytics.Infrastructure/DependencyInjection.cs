using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhiUsageAnalytics.Application.Interfaces;
using PhiUsageAnalytics.Infrastructure.Cache;
using PhiUsageAnalytics.Infrastructure.Data;
using PhiUsageAnalytics.Infrastructure.Repositories;
using StackExchange.Redis;

namespace PhiUsageAnalytics.Infrastructure;

/// <summary>
/// Registers Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // SQL Server
        services.AddDbContext<SyllabusDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("PhiSyllabusDb"),
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(120);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null
                    );
                }
            )
        );

        // Redis
        var redisConnection = configuration.GetConnectionString("RedisConnection") ?? "localhost:7575,abortConnect=False";
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Repository
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

        return services;
    }
}
