using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace DispatchR;

public interface IMediator
{
    Task<TResponse> Send<TRequest, TResponse>(IRequest<TRequest, TResponse> command,
        CancellationToken cancellationToken) where TRequest : IRequest;
}

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly PipelineFactory _factory = new();

    public Task<TResponse> Send<TRequest, TResponse>(IRequest<TRequest, TResponse> command,
        CancellationToken cancellationToken) where TRequest : IRequest
    {
        var request = (TRequest)command;

        var pipelines = serviceProvider.GetServices<IRequestPipeline<TRequest, TResponse>>().ToList();

        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        
        if (pipelines.Any())
        {
            var pipeline = _factory.GetPipeline(request, handler, pipelines);
            return pipeline(cancellationToken);
        }

        return handler.Handle(request, cancellationToken);
    }
}