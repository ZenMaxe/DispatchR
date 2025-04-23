using System.Collections.Concurrent;

namespace DispatchR;

public sealed class PipelineFactory
{


    public RequestHandlerDelegate<TResponse> GetPipeline<TRequest, TResponse>(
        TRequest request,
        IRequestHandler<TRequest, TResponse> handler,
        IEnumerable<IRequestPipeline<TRequest, TResponse>> pipelines)
    {
        return PipelineCache<TRequest, TResponse>.BuildPipeline(request, handler, pipelines);
    }

    private static class PipelineCache<TRequest, TResponse>
    {
        private static Func<TRequest, IRequestHandler<TRequest, TResponse>, IEnumerable<IRequestPipeline<TRequest, TResponse>>, RequestHandlerDelegate<TResponse>>? _cachedFactory;

        public static RequestHandlerDelegate<TResponse> BuildPipeline(
            TRequest request,
            IRequestHandler<TRequest, TResponse> handler,
            IEnumerable<IRequestPipeline<TRequest, TResponse>> pipelines)
        {
            _cachedFactory ??= BuildFactory();
            return _cachedFactory(request, handler, pipelines);
        }

        private static Func<TRequest, IRequestHandler<TRequest, TResponse>, IEnumerable<IRequestPipeline<TRequest, TResponse>>, RequestHandlerDelegate<TResponse>> BuildFactory()
        {
            return (req, h, pipes) =>
            {
                RequestHandlerDelegate<TResponse> terminal = ct => h.Handle(req, ct);

                var pipeline = pipes
                    .OrderByDescending(p => p.Priority)
                    .Aggregate(terminal, (next, pipeline) =>
                        ct => pipeline.Handle(req, next, ct));

                return pipeline;
            };
        }
    }

}