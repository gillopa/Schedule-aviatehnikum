using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration;
using Schedule.Repository;

namespace Schedule.Telegram;
public class TelegramBotService
{
    private TelegramBotClient _botClient;
    private readonly ILogger<TelegramBotService> _logger;
    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger, TelegramBotClient telegramBotClient)
    {
        _logger = logger;
        _botClient = telegramBotClient;
    }
    public async Task SendPhotoToListStudents(List<long> students, string url, string caption)
    {
        foreach (var student in students)
            await _botClient.SendPhoto(student, url, caption);
    }
    [Obsolete]
    public async Task<string> UploadPhotoAsync(InputFile inputFile)
    {
        var botId = (await _botClient.GetMe()).Id;
        var message = await _botClient.SendPhoto(botId, inputFile);
        if (message == null)
            _logger.LogInformation("message is null");
        _logger.LogInformation(message?.Photo?.LastOrDefault()?.FileId);
        return message?.Photo?.LastOrDefault()?.FileId ?? "null";
    }
}
public class TelegramBot : IHostedService
{
    private TelegramBotClient _botClient;
    private CancellationTokenSource? _cancelTokenSource;
    private long _botId { get; set; }
    private readonly ScheduleConstants _scheduleConstants;
    private readonly ILogger<TelegramBot> _logger;
    private readonly ScheduleRepository _dataBase;
    public TelegramBot(ILogger<TelegramBot> logger, TelegramBotClient telegramBotClient, ScheduleConstants scheduleConstants,
            ScheduleRepository dataBase)
    {
        _dataBase = dataBase;
        _scheduleConstants = scheduleConstants;
        _botClient = telegramBotClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancelTokenSource = new CancellationTokenSource();

        try
        {
            var botUser = await _botClient.GetMeAsync(cancellationToken);
            _botId = botUser.Id;
            _logger.LogInformation($"Bot {botUser.Username} has started.");

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: new ReceiverOptions()
                {
                    AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
                },
                cancellationToken: _cancelTokenSource.Token
            );
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error starting bot: {ex.Message}");
            StopAsync(CancellationToken.None).Wait();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cancelTokenSource != null)
        {
            _cancelTokenSource.Cancel();
            _logger.LogInformation("Bot stopped.");
        }
        else
            _logger.LogInformation("Bot was not started or already stopped.");

        _botClient?.Close();
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleCallbackQuery(botClient, update.CallbackQuery, cancellationToken);
            }
            else if (update.Type == UpdateType.Message && update.Message != null)
            {
                await HandleMessage(botClient, update.Message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("idk some error i don't wanna to read your shity code", ex);
        }
    }

    private async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.From?.Id ?? 0;
        if (callbackQuery == null || callbackQuery.Data == null || chatId == 0)
            return;
        try
        {
            var json = JsonObject.Parse(callbackQuery.Data)?.AsObject();
            if (json == null)
                return;
            if (json.ContainsKey("group"))
            {
                var group = json["group"]?.ToString();
                if (string.IsNullOrEmpty(group))
                {
                    await _botClient.SendMessage(chatId,
                        "I dont really understand how you get in there, but your group is empty.",
                        cancellationToken: cancellationToken);
                    return;
                }

                await _dataBase.UpdateGroupStatusAsync(chatId, group);
            }
            else if (json.ContainsKey("getDay"))
            {
                var day = json["getDay"]?.GetValue<string>();
                var group = await _dataBase.GetGroup(chatId);
                if (group == null || day == null)
                    return;
                var photoId = await _dataBase.GetSchedileIdByDateAndGroup(group, day);
                var photo = InputFile.FromString(photoId);
                await _botClient.SendPhoto(chatId, photo, cancellationToken: cancellationToken);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError($"JsonExeption: {ex}");
            await botClient.SendMessage(chatId, "Your request are dumb", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exeption: {ex}");
        }
    }

    private async Task HandleMessage(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        if (message == null)
            return;

        string updateMessage = message.Text ?? string.Empty;

        switch (updateMessage)
        {
            case "/start":
                await HandleStartCommand(botClient, message, cancellationToken);
                break;
            case "/subscribe":
                await HandleSubscribeCommand(botClient, message, cancellationToken);
                break;
            case "/unsubscribe":
                await HandleUnsubscribeCommand(botClient, message, cancellationToken);
                break;
            case "/zvonki":
                await HandleZvonkiCommand(botClient, message, cancellationToken);
                break;
            case "/raspisanie":
                await HandleRaspisanieCommand(botClient, message, cancellationToken);
                break;
        }
    }

    private async Task HandleStartCommand(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        List<InlineKeyboardButton> buffer = new();
        List<InlineKeyboardButton[]> currentButtonsRow = new();
        foreach (var group in _scheduleConstants.Groups)
        {
            buffer.Add(InlineKeyboardButton.WithCallbackData(group, new JsonObject { { "group", group } }.ToString()));
            if (buffer.Count == 4)
            {
                currentButtonsRow.Add(buffer.ToArray());
                buffer.Clear();
            }
        }

        if (buffer.Count != 0)
            currentButtonsRow.Add(buffer.ToArray());

        InlineKeyboardMarkup inlineKeyboardDevice = new(currentButtonsRow);
        await _dataBase.AddNewClientAsync($"{message.From?.FirstName ?? "empty"} {message.From?.LastName ?? "empty"}",
            message.From?.Id ?? 0);
        await botClient.SendMessage(message.From?.Id ?? 0, "Hello, i'm shrimp let's do shripies things shrimp shrimp",
            cancellationToken: cancellationToken);
        await botClient.SendMessage(message.From?.Id ?? 0, "Выбери группу: ", replyMarkup: inlineKeyboardDevice,
            cancellationToken: cancellationToken);
    }

    private async Task HandleSubscribeCommand(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        await _dataBase.UpdateMailingStatusAsync(message.From?.Id ?? 0, 1);
        await botClient.SendMessage(message.From?.Id ?? 0,
            "Shrimpy shrim shrimp, now you will get rsp right at the time posting it",
            cancellationToken: cancellationToken);
    }

    private async Task HandleUnsubscribeCommand(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        await _dataBase.UpdateMailingStatusAsync(message.From?.Id ?? 0, 0);
        await botClient.SendMessage(message.From?.Id ?? 0, "You are bitch, go fucking out of here",
            cancellationToken: cancellationToken);
    }

    private async Task HandleZvonkiCommand(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {

        var rsp = await _dataBase.GetScheduleCalls();
        await botClient.SendPhoto(message.From?.Id ?? 0, InputFile.FromFileId(rsp), cancellationToken: cancellationToken);
    }

    private async Task HandleRaspisanieCommand(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var group = await _dataBase.GetGroup(message.From?.Id ?? 0);

        var currentButtonsRow = new List<InlineKeyboardButton>();
        var scheduleDateList = await _dataBase.GetDatesByDateAndGroup(group, 4);
        scheduleDateList.Sort();

        foreach (var scheduleDate in scheduleDateList)
            currentButtonsRow.Add(InlineKeyboardButton.WithCallbackData($"{scheduleDate.ToString("dd.MM")} {scheduleDate.DayOfWeek}",
                    new JsonObject { { "getDay", scheduleDate.ToString("yyyy.MM.dd") } }.ToString()));


        var keyboardButtonsRows = new List<InlineKeyboardButton[]> { currentButtonsRow.ToArray() };

        InlineKeyboardMarkup inlineKeyboardDevice = new(keyboardButtonsRows);

        await botClient.SendMessage(message.From?.Id ?? 0, "Доступные расписания:", replyMarkup: inlineKeyboardDevice,
            cancellationToken: cancellationToken);
    }


    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception.Message, exception);
        return Task.CompletedTask;
    }
}