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

    public async Task<string> UploadProductImageAsync(string productCode, IFormFile file)
    {
        ValidateImageFile(file);

        var code = (productCode ?? "product").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            code = "product";

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"products/{code}",
            PublicId = "main",
            Overwrite = true,
            Transformation = new Transformation().Width(800).Height(800).Crop("limit")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.AbsoluteUri;
    }

    private static void ValidateImageFile(IFormFile file)
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
    }

    public async Task DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var deletionParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deletionParams);
    }

    public async Task<string> UploadContractPdfAsync(string contractNumber, byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            throw new ArgumentException("PDF file content is empty.");
        }

        var safeContractNumber = (contractNumber ?? "contract").Trim();
        var publicIdWithExt = safeContractNumber.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? safeContractNumber
            : $"{safeContractNumber}.pdf";

        using var stream = new MemoryStream(pdfBytes);
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(publicIdWithExt, stream),
            Folder = "workshift/contracts",
            PublicId = safeContractNumber,
            Type = "authenticated",
            Overwrite = true
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary PDF upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.PublicId;
    }

    public async Task<byte[]> DownloadRawFileAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new ArgumentException("PublicId is required for download.");
        }

        var targetPublicId = publicId.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? publicId
            : $"{publicId}.pdf";

        var signedUrl = _cloudinary.Api.Url.ResourceType("raw").Type("authenticated").Signed(true).BuildUrl(targetPublicId);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(signedUrl);
        if (!response.IsSuccessStatusCode)
        {
            // If fetching with .pdf failed, try fetching with original publicId as fallback
            var fallbackSignedUrl = _cloudinary.Api.Url.ResourceType("raw").Type("authenticated").Signed(true).BuildUrl(publicId);
            var fallbackResponse = await httpClient.GetAsync(fallbackSignedUrl);
            if (!fallbackResponse.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Unable to download the contract PDF ({response.StatusCode}): {errorBody}");
            }
            return await fallbackResponse.Content.ReadAsByteArrayAsync();
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task DeleteRawFileAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Raw,
            Type = "authenticated"
        };
        await _cloudinary.DestroyAsync(deletionParams);
    }
}
