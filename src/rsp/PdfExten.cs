using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Globalization;
using iText.Layout.Element;
using iText.Kernel.Pdf.Canvas;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf.Annot;

static class PdfDocumentExtensions
{
    public static string ExtractTextFromPdfBytes(this PdfDocument pdfDocument)
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
    public static string? GetDayOfWeek(this PdfDocument pdfDocument)
    {
        try
        {
            string extractedText = ExtractTextFromPdfBytes(pdfDocument);
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
    public static string? GetDate(this PdfDocument pdfDocument)
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
                    return match.Groups[1].Value;
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
                    Rectangle rect = location.GetRectangle();
                    return (rect.GetX(), rect.GetY());
                }
            }
        }
        return (0, 0);
    }
    public static void CropPdf(this PdfDocument pdfDocument, FileInfo outputFile, float x, float y, float width, float height)
    {
        using (PdfReader reader = pdfDocument.GetReader())
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

