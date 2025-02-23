using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Schedule;
using Schedule.Pulling;
using Schedule.Telegram;
using Schedule.Repository;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
try
{
    var app = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration((context, builder) =>
            builder.AddUserSecrets(typeof(Program).Assembly)
        )
        .ConfigureServices((context, services) =>
        {
            services.AddSerilog();
            services.AddHostedService<SchedulePullingService>();
            services.AddHostedService<TelegramBot>();
            services.AddHostedService<ScheduleHostService>();

            services.AddHttpClient();
            services.AddTransient<CloudinaryUploader>();
            services.AddSingleton<TelegramBotService>();
            services.AddSingleton<ScheduleConstants>();
            services.AddSingleton<ScheduleRepository>();
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(typeof(Program).Assembly);
            });

            services.AddSingleton<TelegramBotClient>(services =>
            {
                string? token = services.GetService<IConfiguration>()?["TelegramBot:Token"]
                    ?? throw new InvalidDataException("Value for TelegramBot:Token missing in config.");
                return new TelegramBotClient(token);
            });
        });
    await app.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception.");
}
finally
{
    await Log.CloseAndFlushAsync();
}

//class Program
//{
//    private static int RaspisanieCounter { get; set; }
//    public static List<Raspisanie> Raspisanies { get; private set; } = new List<Raspisanie>();
//    public static string workingDirectory = Environment.CurrentDirectory;
//    public static string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
//
//    private static async Task Main(string[] args)
//    {
//        
//        await app.Build().RunAsync();
//
//        var start = Console.ReadLine();
//        try
//        {
//            RaspisanieCounter = Convert.ToInt32(start);
//        }
//        catch (FormatException)
//        {
//            Console.WriteLine("Raspisanie counter not int");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine(ex);
//        }
//        while (true)
//        {
//            string url = $"https://permaviat.ru/_engine/get_file.php?f={RaspisanieCounter}&d=_res/fs/&p=file.pdf";
//            string filePath = $"{projectDirectory}{RaspisanieCounter}.pdf";
//            var downloadStatus = await DownloadFileAsync(url, filePath);
//            if (downloadStatus == true)
//            {
//                var fileInfo = new FileInfo(fileName);
//                var raspisanie = new Raspisanie(fileInfo);
//                Raspisanies.Add(raspisanie);
//                if (raspisanie.IsRaspisanieValid())
//                {
//                    Console.WriteLine($"File load succses: {fileName}");
//                    await TelegramBotRaspisanie.SendToAllSubscribers(raspisanie);
//                }
//                RaspisanieCounter++;
//            }
//            await Task.Delay(5 * 1000);
//        }
//    }
//
//    static async Task<bool> DownloadFileAsync(string url, string filePath)
//    {
//        Console.WriteLine($"Download {url}...");
//        
//        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
//        using var client = new HttpClient(handler);
//        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
//        
//        if (response.StatusCode == HttpStatusCode.OK)
//        {
//            Console.WriteLine($"Error downloading file: {response.StatusCode}");
//            return false;
//        }
//
//        await using var streamToReadFrom = await response.Content.ReadAsStreamAsync();
//        await using var streamToWriteTo = File.Open(filePath, FileMode.Create);
//        
//        await streamToReadFrom.CopyToAsync(streamToWriteTo);
//        
//        return true;
//    }
//
//}
