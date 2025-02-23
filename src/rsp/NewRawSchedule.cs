using iText.Kernel.Pdf;
using MediatR;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace Schedule;

public class NewRawSchedule : INotification
{
    public required byte[] Data { get; init; }
}
public class NewRawScheduleHandler : INotificationHandler<NewRawSchedule>
{
    private readonly ILogger<NewRawScheduleHandler> _logger;
    private readonly IMediator _mediator;
    private readonly CloudinaryUploader _cloudinaryUploader;
    private readonly ScheduleConstants _scheduleConstants;

    public NewRawScheduleHandler(ScheduleConstants scheduleConstants, CloudinaryUploader cloudinaryUploader, ILogger<NewRawScheduleHandler> logger, IMediator mediator)
    {
        _scheduleConstants = scheduleConstants;
        _cloudinaryUploader = cloudinaryUploader;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(NewRawSchedule newRawSchedule, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receved new raw schedule");
        using var stream = new MemoryStream(newRawSchedule.Data);
        using var readerPdf = new PdfReader(stream);
        using var pdfDoc = new PdfDocument(readerPdf);
        var date = pdfDoc.GetDate();
        if (date == null)
            return;
        foreach (var group in _scheduleConstants.Groups)
        {
            var pdfCopy = pdfDoc;
            var coords = pdfCopy.GetCoords(group);

            pdfCopy.CropPdf(coords.x, coords.y);

            var image = pdfCopy.ConvertToPng();
            if (image == null)
                throw new ArgumentException();
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Position = 0;
             var photoUrl = await _cloudinaryUploader.UploadImageAsync(ms.ToArray());
            if (string.IsNullOrEmpty(photoUrl))
            {
                _logger.LogCritical("PhotoUrl is null");
                continue;
            }
            var schedule = new Schedule((DateOnly)date, group, photoUrl);
            await _mediator.Publish<NewSchedule>(new NewSchedule { Schedule = schedule });
        }

    }
}