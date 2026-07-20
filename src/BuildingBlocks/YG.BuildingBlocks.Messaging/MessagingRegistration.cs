using Microsoft.Extensions.DependencyInjection;

namespace YG.BuildingBlocks.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddYGMessaging(this IServiceCollection services)
    {
        services.AddScoped<IYGMessageBus, WolverineMessageBus>();
        services.AddScoped<IYGOutbox, WolverineOutbox>();
        return services;
    }
}