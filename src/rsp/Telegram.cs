using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


class TelegramBot : IHostedService
{
    private TelegramBotClient bot;
    private string botTokenPath = "Token.json";
    private ConcurrentDictionary<string, string> _cashFileIds = new();
    private CancellationTokenSource _cancelTokenSource;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancelTokenSource = new CancellationTokenSource();
        string jsonString = File.ReadAllText(botTokenPath);
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var configuration = JsonSerializer.Deserialize<JsonObject>(jsonString, options);
        if (configuration == null || !configuration.ContainsKey("Token"))
        {
            throw new Exception("Not found");
        }

        var botToken = configuration["token"].ToString();
        bot = new TelegramBotClient(botToken);
        var me = await bot.GetMeAsync();
        Console.WriteLine($"Bot {me.Username} has started.");
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }
        };
        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new Telegram.Bot.Polling.ReceiverOptions()
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery
                }
            },
            cancellationToken: _cancelTokenSource.Token
        );
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cancelTokenSource.CancelAsync();
    }

    public bool AddNewRaspisanie(string filePath, string capture)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(capture))
        {
            return false;
        }

        _cashFileIds.TryRemove(capture, out _);
        if (_cashFileIds.Count >= 5 && _cashFileIds.TryRemove(_cashFileIds.Keys.FirstOrDefault(), out _))
        {
        }

        return _cashFileIds.TryAdd(capture, filePath);
    }
    //  TODO
    // public async Task SendToAllSubscribers(Raspisanie raspisanie)
    // {
    //     using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
    //     {
    //         var users = await DataBase.GetUsersWithMailingEnabledAsync();
    //         var inputFile = InputFile.FromStream(fileStream);
    //         foreach (var uniqueId in users)
    //         {
    //             await bot.SendPhoto(uniqueId, inputFile, capture);
    //         }
    //     }
    // }


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
            Console.WriteLine(ex);
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
                    await bot.SendMessage(chatId,
                        "I dont really understand how you get in there, but your group is empty.",
                        cancellationToken: cancellationToken);
                    return;
                }

                await DataBase.UpdateGroupStatusAsync(chatId, group);
            }
            else if (json.ContainsKey("getDay"))
            {
                var day = json["getDay"]?.ToString();
                var group = await DataBase.GetGroup(chatId);
                if (group == null || day == null)
                    return;
                var photoId = await DataBase.GetSchedileIdByDateAndGroup(group, DateOnly.Parse(day));
                var photo = InputFile.FromFileId(photoId.ToString());
                await bot.SendPhoto(chatId, photo, cancellationToken: cancellationToken);
            }
        }
        catch (JsonException e)
        {
            Console.WriteLine(e);
            await botClient.SendMessage(chatId, "Your request are dumb", cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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
        InlineKeyboardButton[] buttons =
        {
            InlineKeyboardButton.WithCallbackData("РП-21-1", new JsonObject { { "group", "РП-21-1" } }.ToString())
        };
        var currentButtonsRow = new List<InlineKeyboardButton[]>
        {
            buttons
        };
        InlineKeyboardMarkup inlineKeyboardDevice = new(currentButtonsRow);
        await DataBase.AddNewClientAsync($"{message.From?.FirstName ?? "empty"} {message.From?.LastName ?? "empty"}",
            message.From?.Id ?? 0);
        await botClient.SendMessage(message.From?.Id ?? 0, "Hello, i'm shrimp let's do shripies things shrimp shrimp",
            cancellationToken: cancellationToken);
        await botClient.SendMessage(message.From?.Id ?? 0, "Выбери группу: ", replyMarkup: inlineKeyboardDevice,
            cancellationToken: cancellationToken);
    }

    private async Task HandleSubscribeCommand(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        await DataBase.UpdateMailingStatusAsync(message.From?.Id ?? 0, 1);
        await botClient.SendMessage(message.From?.Id ?? 0,
            "Shrimpy shrim shrimp, now you will get rsp right at the time posting it",
            cancellationToken: cancellationToken);
    }

    private async Task HandleUnsubscribeCommand(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        await DataBase.UpdateMailingStatusAsync(message.From?.Id ?? 0, 0);
        await botClient.SendMessage(message.From?.Id ?? 0, "You are bitch, go fucking out of here",
            cancellationToken: cancellationToken);
    }

    private async Task HandleZvonkiCommand(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var rsp = GetRaspisenieZnonkov();
        await botClient.SendPhoto(message.From?.Id ?? 0, rsp, cancellationToken: cancellationToken);
    }

    private async Task HandleRaspisanieCommand(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var group = await DataBase.GetGroup(message.From?.Id ?? 0);
        var keyboardButtonsRows = new List<InlineKeyboardButton[]>();
        var currentButtonsRow = new List<InlineKeyboardButton>();
        var scheduleDateList = await DataBase.GetDatesByDateAndGroup(group, 5);
        for (var day = DayOfWeek.Monday; day <= DayOfWeek.Sunday; day++)
        {
            if (scheduleDateList.Where(x => x.DayOfWeek == day).Any())
            {
                var schedule = scheduleDateList.Where(x => x.DayOfWeek == day).FirstOrDefault();
                currentButtonsRow.Add(InlineKeyboardButton.WithCallbackData(day.ToString(),
                    new JsonObject { { "getDay", schedule.ToString("yyyy.MM.dd") } }.ToString()));
            }
        }

        if (currentButtonsRow.Count > 0)
            keyboardButtonsRows.Add(currentButtonsRow.ToArray());
        InlineKeyboardMarkup inlineKeyboardDevice = new(keyboardButtonsRows);

        await botClient.SendMessage(message.From?.Id ?? 0, "Доступные расписания:", replyMarkup: inlineKeyboardDevice,
            cancellationToken: cancellationToken);
    }


    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }

    public string GetTextInsideParentheses(string input)
    {
        string pattern = @"\((.*?)\)";

        Match match = Regex.Match(input, pattern);

        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}