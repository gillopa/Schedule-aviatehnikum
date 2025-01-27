using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace rsp;

public class SchedulePullingService : IHostedService
{
    private readonly TimeSpan _pullingInterval = TimeSpan.FromMilliseconds(100);
    private readonly CancellationTokenSource _cts = new();
    private readonly HttpClient _client = new();
    private readonly IMediator _mediator;
    
    private int _sequentialCode;
    private Task? _pullingTask;

    public SchedulePullingService(
        IConfiguration configuration,
        IMediator mediator) 
    {
        _sequentialCode = configuration.GetValue<int>("sequentialCode");
        _mediator = mediator;
    }
    
    public Task StartAsync(CancellationToken cancellatinoToken)
    {
        if (_pullingTask is not null)
            throw new InvalidOperationException("Already running.");

        _pullingTask = PullingLoop(_cts.Token);

        return Task.CompletedTask;
    }
    
    public async Task StopAsync(CancellationToken cancellatinoToken)
    {
        if (_pullingTask is null)
            throw new InvalidOperationException("Not running.");
        
        await _cts.CancelAsync();
        await _pullingTask;
        _pullingTask.Dispose();
    }
    
    private async Task PullingLoop(CancellationToken cancellationToken) 
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            byte[] rawSchedule = await _client.GetByteArrayAsync(
                $"https://permaviat.ru/_engine/get_file.php?f={_sequentialCode}&d=_res/fs/&p=file.pdf");

            await _mediator.Publish<NewRawSchedule>(
                new NewRawSchedule {
                    Data = rawSchedule
                });

            _sequentialCode++;
            await Task.Delay(_pullingInterval, cancellationToken);
        }
    }

}