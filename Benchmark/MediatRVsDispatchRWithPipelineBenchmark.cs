using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using MediatR;
using DispatchR;

namespace Benchmark;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
[SimpleJob(RuntimeMoniker.Net90, baseline: true)]
public class MediatRVsDispatchWithPipelineRBenchmark
{
    private MediatR.IMediator _mediator;
    private DispatchR.IMediator _dispatcher;
    private ServiceProvider _serviceProvider;
    
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssemblies(typeof(PingHandlerMediatR).Assembly));
        services.AddDispatchR(typeof(PingDispatchR).Assembly);
        services.AddTransient<IPipelineBehavior<PingMediatR, string>, LoggingBehaviorMediat>();

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<MediatR.IMediator>();
        _dispatcher = _serviceProvider.GetRequiredService<DispatchR.IMediator>();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task<string> SendPing_WithPipeline_MediatR()
    {
        return await _mediator.Send(new PingMediatR(), CancellationToken.None);
    }

    [Benchmark]
    public async Task<string> SendPing_WithPipeline_DispatchR()
    {
        return await _dispatcher.Send(new PingDispatchR(), CancellationToken.None);
    }
}