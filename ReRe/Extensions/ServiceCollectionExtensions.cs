using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using ReRe.Interfaces;
using ReRe.Models;
using ReRe.Services;

namespace ReRe.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReRe(this IServiceCollection services, Action<ConnectionFactory, RabidOptions> setup, params Assembly[] assemblies)
    {
        services.AddSingleton<ReRe.Services.ConnectionMultiplexer>((_) =>
        {
            var mplx = new ConnectionMultiplexer(setup);
            return mplx;
        });
        services.AddSingleton<ConsumerHandler>();
        services.AddTransient(typeof(IRequestClient<>), typeof(RequestClient<>));

        ConsumerHandler.RegisterConsumers(services, assemblies);

        return services;
    }
}

public static class IHostExtensions
{
    public static IHost UseReRe(this IHost host)
    {
        var handler = host.Services.GetRequiredService<ConsumerHandler>();
        handler.StartConsumers(host.Services).GetAwaiter().GetResult();

        return host;
    }
}

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseReRe(this IApplicationBuilder app)
    {
        var handler = app.ApplicationServices.GetRequiredService<ConsumerHandler>();
        handler.StartConsumers(app.ApplicationServices).GetAwaiter().GetResult();
        return app;
    }
}