using System.Net;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace rsp;

public class SchedulePullingService : IHostedService
{
    private readonly TimeSpan _pullingInterval = TimeSpan.FromMilliseconds(100);
    private readonly CancellationTokenSource _cts = new();
    private readonly HttpClient _client; 
    private readonly IMediator _mediator;
    private readonly ILogger<SchedulePullingService> _logger;

    private int _sequentialCode;
    private Task? _pullingTask;

    public SchedulePullingService(
        ILogger<SchedulePullingService> logger,
        IConfiguration configuration,
        IMediator mediator) 
    {
        _logger = logger;
        _sequentialCode = configuration.GetValue<int>("sequentialCode");
        _mediator = mediator;
        _client = new HttpClient(new HttpClientHandler() {
                AllowAutoRedirect = false
            });
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
            await Task.Delay(_pullingInterval, cancellationToken);

            try 
            {
                HttpResponseMessage response = await _client.GetAsync(
                    $"https://permaviat.ru/_engine/get_file.php?f={_sequentialCode}&d=_res/fs/&p=file.pdf",
                    cancellationToken);

                if (response.StatusCode is HttpStatusCode.Found)
                    continue;

                if (response.StatusCode is not HttpStatusCode.OK)
                {
                    _logger.LogWarning(
                        "Unexpected http status code ({StatusCode}/{StatusCodeName}) ocured on downloading schedule.",
                        response.StatusCode,
                        Enum.GetName(response.StatusCode));
                }

                byte[] rawSchedule = await response.Content.ReadAsByteArrayAsync();

                await _mediator.Publish<NewRawSchedule>(
                    new NewRawSchedule {
                        Data = rawSchedule
                    });

                _sequentialCode++;
            }
            catch (HttpRequestException ex) 
            {
                _logger.LogError(0, ex, 
                    "Unexpected HttpRequestException ocured on downloading schedule, probably connectivity issue.");
            }
        }
    }

}
