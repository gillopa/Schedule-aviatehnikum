using System.Net;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Schedule.Pulling;

public class SchedulePullingService : IHostedService
{
    private readonly TimeSpan _pullingInterval = TimeSpan.FromMilliseconds(1000 * 30);
    private readonly CancellationTokenSource _cts = new();
    private readonly HttpClient _client;
    private readonly IMediator _mediator;
    private readonly ILogger<SchedulePullingService> _logger;

    private int _scheduleСode;
    private Task? _pullingTask;

    public SchedulePullingService(
        ILogger<SchedulePullingService> logger,
        IConfiguration configuration,
        IMediator mediator)
    {
        _logger = logger;
        _scheduleСode = configuration.GetValue<int>("scheduleСode");
        _logger.LogInformation($"ScheduleСode: {_scheduleСode}");
        _mediator = mediator;
        _client = new HttpClient(new HttpClientHandler()
        {
            AllowAutoRedirect = false
        });
    }

    public Task StartAsync(CancellationToken cancellatinoToken)
    {
        _logger.LogInformation("Running...");

        if (_pullingTask is not null)
            throw new InvalidOperationException("Already running.");

        _pullingTask = PullingLoop(_cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellatinoToken)
    {
        _logger.LogInformation("Stoping...");

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
                    $"https://permaviat.ru/_engine/get_file.php?f={_scheduleСode}&d=_res/fs/&p=file.pdf",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Found)
                    continue;

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning(
                        "Unexpected http status code ({StatusCode}/{StatusCodeName}) ocured on downloading schedule.",
                        response.StatusCode,
                        Enum.GetName(response.StatusCode));
                }
                byte[] rawSchedule = await response.Content.ReadAsByteArrayAsync();
                await _mediator.Publish<NewRawSchedule>(
                    new NewRawSchedule
                    {
                        Data = rawSchedule
                    });

                _scheduleСode++;

                _logger.LogInformation($"New schedule was pulled. New sequential code is {_scheduleСode}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "Unexpected HttpRequestException ocured on downloading schedule, probably connectivity issue.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exeption: {ex}");
            }
        }
    }

}
