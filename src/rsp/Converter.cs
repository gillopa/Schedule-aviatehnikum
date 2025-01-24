using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Docnet.Core;
using Docnet.Core.Models;

class Converter
{
    public static void PdfToPng(string pdfPath, string outputPath)
    {
        try
        {
            using var docReader = DocLib.Instance.GetDocReader(
                pdfPath,
                new PageDimensions(50 * 4, 174 * 4)
            );

            using var pageReader = docReader.GetPageReader(0);
            var rawBytes = pageReader.GetImage();

            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            if (rawBytes == null || rawBytes.Length == 0)
            {
                Console.WriteLine($"Error: Could not extract image data from the first page of {pdfPath}.");
                return;
            }
            Image<Bgra32> pdfImage = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
            Console.WriteLine($"Converting {pdfPath} to PNG...");
            pdfImage.SaveAsPng(outputPath);

            Console.WriteLine($"Successfully converted {pdfPath} to {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}