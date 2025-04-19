using Mediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mediator.Implementation;
public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _provider = serviceProvider;

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = _provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"Handler for request type {requestType} not found.");

        var method = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handle method not found in handler type {handlerType}.");

        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
        {
            return (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
        };

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = _provider
            .GetServices(behaviorType)
            .Reverse()
            .ToList();
        foreach (var behavior in behaviors)
        {
            var behaviorMethod = behaviorType.GetMethod("Handle")
                ?? throw new InvalidOperationException($"Handle method not found in behavior type {behaviorType}.");

            var next = handlerDelegate;
            handlerDelegate = () => (Task<TResponse>)behaviorMethod.Invoke(behavior, [request, next, cancellationToken])!;
        }
        return await handlerDelegate();
    }
}
