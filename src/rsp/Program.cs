class Program
{
    private static async Task Main(string[] args)
    {
        var bot = TelegramBotRaspisanie.Start();
        var raspisanieCounter = Raspisanie.RaspisanieCounter;
        var start = Console.ReadLine();
        try
        {
            raspisanieCounter = Convert.ToInt32(start);
            Raspisanie.RaspisanieCounter = raspisanieCounter;
        }
        catch (FormatException)
        {
            Console.WriteLine("Raspisanie counter not int");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        while (true)
        {
            string url = $"https://permaviat.ru/_engine/get_file.php?f={Raspisanie.RaspisanieCounter}&d=_res/fs/&p=file.pdf";
            string fileName = Raspisanie.RaspisanieCounter + ".pdf";
            var downloadStatus = await DownloadFileAsync(url, fileName);
            if (downloadStatus == true)
            {
                var fileInfo = new FileInfo(fileName);
                if (await Raspisanie.IsRaspisanieHasMineGroupNameAsync(fileName))
                {
                    Console.WriteLine($"File load succses: {fileName}");
                    var coordsTuple = PdfExten.GetCoords(fileInfo);
                    PdfExten.CropPdf(fileInfo, new FileInfo("crop" + fileName), coordsTuple.x - 15, coordsTuple.y + 19, 49, -174);
                    var dayOfWeek = await Raspisanie.GetDayOfWeekAsync(fileInfo);
                    Console.WriteLine(dayOfWeek);
                    Converter.PdfToPng("crop" + fileName, fileName + ".png");
                    await TelegramBotRaspisanie.SendToAllSubscribers(fileName + ".png", dayOfWeek ?? "Без даты");
                }
                Raspisanie.RaspisanieCounter++;
            }
            await Task.Delay(60 * 1000);
        }
    }

    static async Task<bool> DownloadFileAsync(string url, string filePath)
    {
        Console.WriteLine($"Download {url}...");
        using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
        using (var client = new HttpClient(handler))
        {
            try
            {
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    {
                        using (var streamToWriteTo = File.Open(filePath, FileMode.Create))
                        {
                            await streamToReadFrom.CopyToAsync(streamToWriteTo);
                        }
                    }
                    return true;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return false;
            }
        }
    }

}