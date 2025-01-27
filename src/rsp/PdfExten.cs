using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Globalization;
using iText.Layout.Element;
using iText.Kernel.Pdf.Canvas;
using System.Text.RegularExpressions;

class PdfExten
{
    public static string ExtractTextFromPdfBytes(byte[] pdfBytes)
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
    public static string? GetDayOfWeek(FileInfo filePath)
    {
        try
        {
            byte[] fileBytes = File.ReadAllBytes(filePath.FullName);
            string extractedText = ExtractTextFromPdfBytes(fileBytes);
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
    public static string? GetDate(FileInfo fileInfo)
    {
        string pattern = @"учебных занятий на (\d{2}\.\d{2}\.\d{4}) \((.*?)\)"; // Паттерн для поиска
        using (var reader = new PdfReader(fileInfo.FullName))
        {
            using (PdfDocument pdfDocument = new PdfDocument(reader))
            {
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
                            return match.Groups[1].Value;
                        }

                    }
                }
            }
        }
        return null;
    }
    public static (float x, float y) GetCoords(FileInfo fileInfo, string nameOfGrup)
    {
        using (var reader = new PdfReader(fileInfo))
        {
            PdfDocument pdfDocument = new PdfDocument(reader);

            for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
            {
                RegexBasedLocationExtractionStrategy strategy = new RegexBasedLocationExtractionStrategy(nameOfGrup);
                new PdfCanvasProcessor(strategy).ProcessPageContent(pdfDocument.GetPage(page));
                foreach (IPdfTextLocation location in strategy.GetResultantLocations())
                {
                    if (location != null)
                    {
                        Rectangle rect = location.GetRectangle();
                        return (rect.GetX(), rect.GetY());
                    }
                }
            }
        }
        return (0, 0);
    }
    public static void CropPdf(FileInfo inputFile, FileInfo outputFile, float x, float y, float width, float height)
    {
        using (PdfReader reader = new PdfReader(inputFile.FullName))
        using (PdfWriter writer = new PdfWriter(outputFile.FullName))
        using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
        {
            int numberOfPages = pdfDoc.GetNumberOfPages();

            for (int i = 1; i <= numberOfPages; i++)
            {
                PdfPage page = pdfDoc.GetPage(i);
                Rectangle cropRect = new Rectangle(x, y, width, height);
                page.SetMediaBox(cropRect);
                page.SetCropBox(cropRect);
            }
        }
    }
}

