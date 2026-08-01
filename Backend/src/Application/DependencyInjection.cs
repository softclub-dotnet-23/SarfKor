using System.Reflection;
using Application.Abstractions;
using Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IStoreAccessAuthorizer, StoreAccessAuthorizer>();

        var assembly = Assembly.GetExecutingAssembly();
        var handlerGenericDefinitions = new[] { typeof(IQueryHandler<,>), typeof(ICommandHandler<,>) };

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var handlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && handlerGenericDefinitions.Contains(i.GetGenericTypeDefinition()));

            foreach (var handlerInterface in handlerInterfaces)
                services.AddScoped(handlerInterface, type);
        }

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
