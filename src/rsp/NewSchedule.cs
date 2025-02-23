using MediatR;
using Schedule.Telegram;
using Schedule.Repository;
using Microsoft.Extensions.Logging;

namespace Schedule;
public class NewSchedule : INotification
{
    public required Schedule Schedule { get; init; }
}
public class NewScheduleHandler : INotificationHandler<NewSchedule>
{
    private readonly TelegramBotService _telegramBot;
    private readonly ILogger<NewScheduleHandler> _logger;
    private readonly ScheduleRepository _dataBase;
    public NewScheduleHandler(ScheduleRepository dataBase, TelegramBotService telegramBot, ILogger<NewScheduleHandler> logger)
    {
        _dataBase = dataBase;
        _telegramBot = telegramBot;
        _logger = logger;
    }
    public async Task Handle(NewSchedule notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Trying to deal with new schedule");
        var group = notification.Schedule.Group;
        var url = notification.Schedule.Url;
        var date = notification.Schedule.Date;
        if (_dataBase.AddNewSchedule(group, url, date))
            _logger.LogInformation($"New schedule for group: {group}");

        var students = await _dataBase.GetUsersWithMailingEnabledAsync(group);
        await _telegramBot.SendPhotoToListStudents(students, url, date.DayOfWeek.ToString());
    }
}