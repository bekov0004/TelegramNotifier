using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramNotifier.Models;
using TelegramNotifier.Services;

namespace TelegramNotifier.Extensions;

public static class ServiceCollectionExtensions
{
    // Только из кода
    public static IServiceCollection AddTelegramNotifier(
        this IServiceCollection services,
        Action<TelegramNotifierOptions> configure)
    {
        services.Configure<TelegramNotifierOptions>(configure);
        return services.RegisterServices();
    }

    // Только из секции appsettings
    public static IServiceCollection AddTelegramNotifier(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        services.Configure<TelegramNotifierOptions>(section);
        return services.RegisterServices();
    }

    // Секция appsettings + переопределение из кода
    public static IServiceCollection AddTelegramNotifier(
        this IServiceCollection services,
        IConfigurationSection section,
        Action<TelegramNotifierOptions> configure)
    {
        services.Configure<TelegramNotifierOptions>(section);
        services.PostConfigure<TelegramNotifierOptions>(configure);
        return services.RegisterServices();
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddOptions<TelegramNotifierOptions>()
            .PostConfigure<IHostEnvironment>((options, env) =>
            {
                options.ApplicationName ??= env.ApplicationName;
                options.EnvironmentName ??= env.EnvironmentName;
            });

        services.AddSingleton<TelegramNotifierQueue>();
        services.AddSingleton<TelegramNotifierThrottle>();
        services.AddScoped<ITelegramNotifier, Services.TelegramNotifier>();
        services.AddHttpClient(nameof(TelegramBackgroundWorker));
        services.AddHostedService<TelegramBackgroundWorker>();

        return services;
    }
}
