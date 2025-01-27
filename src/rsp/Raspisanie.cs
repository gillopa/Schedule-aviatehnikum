using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using Telegram.Bot.Types;

class Raspisanie
{
    public static readonly List<string> DaysOfWeek = new List<string>
    {
        "ПОНЕДЕЛЬНИК",
        "ВТОРНИК",
        "СРЕДА",
        "ЧЕТВЕРГ",
        "ПЯТНИЦА",
        "СУББОТА"
    };
    public string? DayOfWeek { get; private set; }
    public string? Date { get; private set; }
    public FileInfo FileInfo { get; private set; }
    
    public Raspisanie(FileInfo fileInfo)
    {
        DayOfWeek = PdfExten.GetDayOfWeek(fileInfo);
        Date = PdfExten.GetDate(fileInfo);
        FileInfo = fileInfo;
    }
    
    public static readonly string RaspisanieZvonokvPath = "rspzvonkov.png";

    public static InputFileStream GetRaspisenieZnonkov()
    {
        using (var fileStream = new FileStream(RaspisanieZvonokvPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var inputFile = InputFile.FromStream(fileStream);
            return inputFile;
        }
    }
    
    public bool IsRaspisanieValid()
    {
        if (DayOfWeek == null || Date == null || FileInfo == null)
            return false;
        return true;
    }

    public async Task<bool> IsRaspisanieHasMineGroupNameAsync(string filePath, string groupName)
    {
        try
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            string extractedText = PdfExten.ExtractTextFromPdfBytes(fileBytes);
            return extractedText.Contains(groupName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }
    }
    public FileInfo? GetMyGroupRaspisanie(string groupName)
    {
        string filePath = $"{groupName}crop{FileInfo.FullName}";
        if (File.Exists(filePath))
            return new FileInfo(filePath);
        var coordsTuple = PdfExten.GetCoords(FileInfo, groupName);
        PdfExten.CropPdf(FileInfo, new FileInfo(filePath), coordsTuple.x - 15, coordsTuple.y + 19, 49, -174); // This digits for cool croping
        Converter.PdfToPng(filePath, $"{filePath}.png");
        return new FileInfo(filePath);
    }
}