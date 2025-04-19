using Mediator.Implementation;
using Mediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mediator.Extensions;
public static class MediatorExtension
{
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddTransient<IMediator, Implementation.Mediator>();

        RegisterHandlers(services, assemblies, typeof(IRequestHandler<,>));
        return services;
    }
    public static IServiceCollection AddPipelineBehaviors(this IServiceCollection services, params Type[] pipelineBehaviors)
    {
        foreach (var behaviorType in pipelineBehaviors)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), behaviorType);
        }

        return services;
    }
    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies, Type handlerInterface)
    {
        var types = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface)
            .ToList();

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .ToList();

            foreach (var interfaceType in interfaces)
            {
                services.AddTransient(interfaceType, type);
            }
        }
    }
}
