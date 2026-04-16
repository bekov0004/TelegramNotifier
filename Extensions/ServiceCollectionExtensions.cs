using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramNotifier.Models;
using TelegramNotifier.Services;

namespace TelegramNotifier.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramNotifier(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TelegramNotifierOptions>(
            configuration.GetSection("TelegramNotifier"));

        services.AddSingleton<TelegramNotifierQueue>();
        services.AddHttpClient<ITelegramNotifier, Services.TelegramNotifier>();
        services.AddHostedService<TelegramBackgroundWorker>();

        return services;
    }
}