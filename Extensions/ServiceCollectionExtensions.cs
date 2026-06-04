using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramNotifier.Models;
using TelegramNotifier.Services;

namespace TelegramNotifier.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramNotifier(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<TelegramNotifierOptions>? configure = null)
    {
        services.Configure<TelegramNotifierOptions>(
            configuration.GetSection("TelegramNotifier"));

        if (configure != null)
            services.PostConfigure<TelegramNotifierOptions>(configure);

        services.AddSingleton<TelegramNotifierQueue>();
        services.AddSingleton<TelegramNotifierThrottle>();
        services.AddScoped<ITelegramNotifier, Services.TelegramNotifier>();
        services.AddHttpClient(nameof(TelegramBackgroundWorker));
        services.AddHostedService<TelegramBackgroundWorker>();

        return services;
    }
}