using Microsoft.Extensions.DependencyInjection;
using PhiUsageAnalytics.Application.Services;

namespace PhiUsageAnalytics.Application;

/// <summary>
/// Registers Application layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AnalyticsService>();
        return services;
    }
}
