using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;
using Schedule;
using Docnet.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = iText.Kernel.Geom.Rectangle;
using Docnet.Core.Models;

static class PdfDocumentExtensions
{
    public static string ExtractText(this PdfDocument pdfDocument)
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
    [Obsolete]
    public static string? GetDayOfWeek(this PdfDocument pdfDocument)
    {
        try
        {
            string extractedText = pdfDocument.ExtractText();
            foreach (var day in Raspisanie.DaysOfWeek)
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
    public static DateOnly? GetDate(this PdfDocument pdfDocument)
    {
        string pattern = @"учебных занятий на (\d{2}\.\d{2}\.\d{4}) \((.*?)\)"; // Паттерн для поиска
        RegexBasedLocationExtractionStrategy strategy = new RegexBasedLocationExtractionStrategy(pattern);
        new PdfCanvasProcessor(strategy).ProcessPageContent(pdfDocument.GetPage(1));
        foreach (IPdfTextLocation location in strategy.GetResultantLocations())
        {
            if (location != null)
            {
                string text = location.GetText();
                Match match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    string dateString = match.Groups[1].Value;
                    string format = "dd.MM.yyyy";

                    if (DateOnly.TryParseExact(dateString, format, out DateOnly date))
                        return date;
                }
            }
        }
        return null;
    }

    public static (float x, float y) GetCoords(this PdfDocument pdfDocument, string nameOfGrup)
    {

        for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
        {
            RegexBasedLocationExtractionStrategy strategy = new RegexBasedLocationExtractionStrategy(nameOfGrup);
            new PdfCanvasProcessor(strategy).ProcessPageContent(pdfDocument.GetPage(page));
            foreach (IPdfTextLocation location in strategy.GetResultantLocations())
            {
                if (location != null)
                {
                    var rect = location.GetRectangle();
                    return (rect.GetX() - 15, rect.GetY() + 19);
                }
            }
        }
        return (0, 0);
    }
    public static void CropPdf(this PdfDocument pdfDocument, float x, float y, float width = 49, float height = -174)
    {
        var page = pdfDocument.GetPage(1);
        var cropRect = new Rectangle(x, y, width, height);
        page.SetMediaBox(cropRect);
        page.SetCropBox(cropRect);
    }
    private static byte[] ToByteArray(this PdfDocument pdfDocument)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (PdfWriter writer = new PdfWriter(stream))
            {
                using (PdfDocument destPdfDocument = new PdfDocument(writer))
                {
                    pdfDocument.CopyPagesTo(1, pdfDocument.GetNumberOfPages(), destPdfDocument);
                }
            }
            return stream.ToArray();
        }
    }

    public static Image<Bgra32>? ConvertToPng(this PdfDocument pdfDocument, int widthPdf = 49, int heightPdf = 174)
    {
        try
        {
            using var docReader = DocLib.Instance.GetDocReader(
                pdfDocument.ToByteArray(),
                new PageDimensions(widthPdf * 4, heightPdf * 4)
            );

            using var pageReader = docReader.GetPageReader(0);
            var rawBytes = pageReader.GetImage();

            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            if (rawBytes == null || rawBytes.Length == 0)
            {
                return null;
            }
            return Image.LoadPixelData<Bgra32>(rawBytes, width, height);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        return null;
    }
}

