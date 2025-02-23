using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class CloudinaryUploader
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryUploader> _logger;

    public CloudinaryUploader(IConfiguration configuration, ILogger<CloudinaryUploader> logger)
    {
        _logger = logger;
        var cloudName = configuration["Cloudinary:CloudName"] ?? throw new ArgumentNullException("Cloudinary:CloudName", "Cloudinary CloudName is missing in configuration.");
        var apiKey = configuration["Cloudinary:ApiKey"] ?? throw new ArgumentNullException("Cloudinary:ApiKey", "Cloudinary ApiKey is missing in configuration.");
        var apiSecret = configuration["Cloudinary:ApiSecret"] ?? throw new ArgumentNullException("Cloudinary:ApiSecret", "Cloudinary ApiSecret is missing in configuration.");
        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string?> UploadImageAsync(byte[] imageData, string? publicId = null)
    {
        try
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription("image", new MemoryStream(imageData)),
                PublicId = publicId
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (uploadResult != null && uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Image uploaded successfully. URL: {Url}", uploadResult.SecureUrl);
                return uploadResult.SecureUrl.ToString();
            }
            else
            {
                _logger.LogError("Error uploading image. Error: {Error}", uploadResult?.Error?.Message);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during image upload. Public ID: {PublicId}", publicId);
            return null;
        }
    }
}