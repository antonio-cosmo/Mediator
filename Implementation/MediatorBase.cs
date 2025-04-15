using Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mediator.Implementation;
public class MediatorBase(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _provider = serviceProvider;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        
        var handler = _provider.GetService(handlerType) 
            ?? throw new InvalidOperationException($"Handler for request type {request.GetType()} not found.");
        
        var method = handlerType.GetMethod("Handle") 
            ?? throw new InvalidOperationException($"Handle method not found in handler type {handlerType}.");
 
        return (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
    }
}
