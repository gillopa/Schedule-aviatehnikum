using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


class TelegramBotRaspisanie
{
    static ITelegramBotClient bot;
    static string botToken = "YOUR_BOT_TOKEN";

    public static async Task Start()
    {
        bot = new TelegramBotClient(botToken);
        var me = await bot.GetMeAsync();
        Console.WriteLine($"Bot {me.Username} has started.");

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }
        };
        bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);
    }
    public static async Task SendToAllSubscribers(string filePath, string capture)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var users = await DataBase.GetUsersWithMailingEnabledAsync();
            foreach (var uniqueId in users)
            {
                await bot.SendPhoto(uniqueId, InputFile.FromStream(fileStream), capture);
            }
        }
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message == null)
            return;
        string updateMessage = update.Message.Text ?? String.Empty;
        if (updateMessage == "/start")
        {
            await DataBase.AddNewClientAsync($"{update.Message.From?.FirstName ?? "empty"} {update.Message.From?.LastName ?? "empty"}", update.Message.From?.Id ?? 0);
            await botClient.SendMessage(update.Message.From?.Id ?? 0, "Hello, i'm shrimp let's do shripies things shrimp shrimp");
        }
        if (updateMessage == "/subscribe")
        {
            await DataBase.UpdateMailingStatusAsync(update.Message.From?.Id ?? 0, 1);
            await botClient.SendMessage(update.Message.From?.Id ?? 0, "Shrimpy shrim shrimp, now you will get rsp right at the time posting it");
        }
        if (updateMessage == "/unsubscribe")
        {
            await DataBase.UpdateMailingStatusAsync(update.Message.From?.Id ?? 0, 0);
            await botClient.SendMessage(update.Message.From?.Id ?? 0, "You are bitch, go fucking out of here");
        }
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }
}
