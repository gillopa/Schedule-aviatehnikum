using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Schedule;
public class ScheduleConstants
{
    public List<string> Groups { get; set; } = new();
    public void GenerateGroupNames(List<string> groups, ILogger<ScheduleHostService> logger)
    {
        List<string> generatedNames = new();
        int currentYear = DateTime.Now.Year;
        int deltaYear = 4;

        int startYear = (DateTime.Now.Month > 8) ? currentYear - deltaYear + 1 : currentYear - deltaYear;
        for (int i = 0; i < deltaYear; i++)
        {
            int year = startYear + i;

            foreach (string groupName in groups)
            {
                generatedNames.Add($"{groupName}-{year - 2000}-1");
            }
        }
        logger.LogInformation(generatedNames.Any() ? $"New list of groups: {string.Join(", ", generatedNames)}" : "Group list is empty");
        generatedNames.Sort();
        Groups = generatedNames;
    }
}
public class ScheduleHostService : IHostedService
{
    private Task? _update;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ScheduleHostService> _logger;
    private readonly ScheduleConstants _scheduleConstants;
    public ScheduleHostService(IConfiguration configuration, ILogger<ScheduleHostService> logger, ScheduleConstants scheduleConstants)
    {
        _scheduleConstants = scheduleConstants;
        _logger = logger;
        _configuration = configuration;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start updating grups");
        _update = UpdateListGroups(cancellationToken);
        return Task.CompletedTask;
    }
    public async Task UpdateListGroups(CancellationToken cancellationToken)
    {
        var groups = _configuration.GetSection("GroupList").Get<List<string>>();
        _logger.LogInformation($"All whached goups: {string.Join(", ", groups)}");
        while (!cancellationToken.IsCancellationRequested)
        {
            _scheduleConstants.GenerateGroupNames(groups, _logger);
            await Task.Delay(1000 * 60 * 60 * 24);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_update != null)
        {
            await _update;
            _update.Dispose();
        }
    }
}
