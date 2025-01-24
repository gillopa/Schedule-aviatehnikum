using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Globalization;
using iText.Layout.Element;
using iText.Kernel.Pdf.Canvas;

class PdfExten
{
    public static (float x, float y) GetCoords(FileInfo fileInfo)
    {
        using (var reader = new PdfReader(fileInfo))
        {
            PdfDocument pdfDocument = new PdfDocument(reader);

            for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
            {
                RegexBasedLocationExtractionStrategy strategy = new RegexBasedLocationExtractionStrategy(Raspisanie.NameGroup);
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

