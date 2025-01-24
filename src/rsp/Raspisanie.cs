using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser.Data;

class Raspisanie
{
    public static string NameGroup = "Название группы (именно так как указана в расписание если большими - то писать большими)";
    public readonly static List<string> DaysOfWeek = new List<string>
    {
        "ПОНЕДЕЛЬНИК",
        "ВТОРНИК",
        "СРЕДА",
        "ЧЕТВЕРГ",
        "ПЯТНИЦА",
        "СУББОТА"
    };
    public static async Task<string?> GetDayOfWeekAsync(FileInfo filePath)
    {
        try
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath.FullName);
            string extractedText = ExtractTextFromPdfBytes(fileBytes);
            foreach (var day in DaysOfWeek)
            {
                if (extractedText.Contains(day))
                    return day;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        return null;
    }
    public static int RaspisanieCounter { get; set; }

    public async static Task<bool> IsRaspisanieHasMineGroupNameAsync(string filePath)
    {
        try
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            string extractedText = ExtractTextFromPdfBytes(fileBytes);
            return extractedText.Contains(NameGroup);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }
    }
    private static string ExtractTextFromPdfBytes(byte[] pdfBytes)
    {
        using (var stream = new MemoryStream(pdfBytes))
        {
            using (var pdfReader = new PdfReader(stream))
            {
                using (var pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader))
                {
                    var text = new System.Text.StringBuilder();
                    for (int i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
                    {
                        ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                        var currentText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i), strategy);
                        text.Append(currentText);
                    }
                    return text.ToString();
                }
            }
        }
    }
}
public class TextLocationEventListener : IEventListener
{
    private readonly List<TextRenderInfo> textRenderInfos = new List<TextRenderInfo>();

    public void EventOccurred(IEventData data, EventType type)
    {
        if (data is TextRenderInfo renderInfo)
        {
            textRenderInfos.Add(renderInfo);
        }
    }

    public ICollection<EventType> GetSupportedEvents()
    {
        return new[] { EventType.RENDER_TEXT };
    }

    public List<TextRenderInfo> GetTextRenderInfo()
    {
        return textRenderInfos;
    }
}