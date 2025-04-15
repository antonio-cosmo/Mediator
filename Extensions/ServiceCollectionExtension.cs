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
public static class ServiceCollectionExtension
{
    public static IServiceCollection AddMediator(this IServiceCollection services, params object[] args)
    {
        var assemblies = ResolveAssemblies(args);

        services.AddSingleton<IMediator, MediatorBase>();

        RegisterHandlers(services, assemblies, typeof(IRequestHandler<,>));
        return services;
    }

    private static Assembly[] ResolveAssemblies(object[] args)
    {
        if (args == null || args.Length == 0)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
                .ToArray();
        }

        if(args.All(a => a is Assembly))
        {
            return args.Cast<Assembly>().ToArray();
        }

        if(args.All(a => a is string))
        {
            var prefixes = args.Cast<string>().ToArray();
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName) && prefixes.Any(p => a.FullName.StartsWith(p)))
                .ToArray();
        }
        
        throw new ArgumentException("Invalid arguments. Expected either Assembly or string.");
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies, Type handlerInterface)
    {
        var types = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
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
