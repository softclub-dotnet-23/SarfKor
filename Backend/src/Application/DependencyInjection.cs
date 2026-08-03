using System.Reflection;
using Application.Abstractions;
using Application.Assistant.Abstractions;
using Application.Assistant.Tools;
using Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IStoreAccessAuthorizer, StoreAccessAuthorizer>();
        services.AddScoped<AssistantToolRegistry>();

        var assembly = Assembly.GetExecutingAssembly();
        var handlerGenericDefinitions = new[] { typeof(IQueryHandler<,>), typeof(ICommandHandler<,>) };
        // IAssistantTool/IPendingActionExecutor are plugin-style interfaces (many implementations,
        // resolved as IEnumerable<T>) -- scanned here for the same reason the handlers are: adding a
        // new tool/executor should never require remembering a second edit to this file.
        var pluginInterfaces = new[] { typeof(IAssistantTool), typeof(IPendingActionExecutor) };

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var handlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && handlerGenericDefinitions.Contains(i.GetGenericTypeDefinition()));
            foreach (var handlerInterface in handlerInterfaces)
                services.AddScoped(handlerInterface, type);

            foreach (var pluginInterface in pluginInterfaces.Where(type.GetInterfaces().Contains))
                services.AddScoped(pluginInterface, type);
        }

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
