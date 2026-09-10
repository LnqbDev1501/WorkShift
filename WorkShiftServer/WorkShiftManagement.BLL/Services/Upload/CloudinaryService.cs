using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using WorkShiftManagement.BLL.Interfaces.Upload;

namespace WorkShiftManagement.BLL.Services.Upload;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"] 
            ?? throw new ArgumentException("Cloudinary:CloudName is missing");
        var apiKey = configuration["Cloudinary:ApiKey"] 
            ?? throw new ArgumentException("Cloudinary:ApiKey is missing");
        var apiSecret = configuration["Cloudinary:ApiSecret"] 
            ?? throw new ArgumentException("Cloudinary:ApiSecret is missing");

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadAvatarAsync(Guid userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null.");
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            throw new InvalidOperationException("File size must not exceed 5MB.");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Invalid file format. Only JPG, PNG and WEBP are allowed.");
        }

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"account/{userId}",
            PublicId = "avatar",
            Overwrite = true,
            Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.AbsoluteUri;
    }
}
