using Mediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mediator.Implementation;
public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _provider = serviceProvider;
    private static readonly Dictionary<Type, MethodInfo> HandlerHandleMethodCache = [];
    private static readonly Dictionary<Type, Type> HandlerTypeCache = [];
    private static readonly MethodInfo BehaviorHandleMethod = typeof(IPipelineBehavior<,>).GetMethod("Handle")
                                       ?? throw new InvalidOperationException($"Handle method not found in behavior type {typeof(IPipelineBehavior<,>)}.");

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();

        if (!HandlerTypeCache.TryGetValue(requestType, out var handlerType))
        {
            handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            HandlerTypeCache[requestType] = handlerType;
        }

        var handler = _provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"Handler for request type {requestType} not found.");

        if (!HandlerHandleMethodCache.TryGetValue(handlerType, out var handleMethod))
        {
            handleMethod = handlerType.GetMethod("Handle")
                ?? throw new InvalidOperationException($"Handle method not found in handler type {handlerType}.");
            HandlerHandleMethodCache[handlerType] = handleMethod;
        }

        RequestHandlerDelegate<TResponse> handlerDelegate = () => (Task<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = _provider
            .GetServices(behaviorType)
            .Reverse();

        foreach (var behavior in behaviors)
        {
            var next = handlerDelegate;
            handlerDelegate = () => (Task<TResponse>)BehaviorHandleMethod.Invoke(behavior, [request, next, cancellationToken])!;
        }
        return handlerDelegate();
    }
}
