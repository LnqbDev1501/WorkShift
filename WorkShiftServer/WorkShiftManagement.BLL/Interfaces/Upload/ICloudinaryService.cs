namespace WorkShiftManagement.BLL.Interfaces.Upload;

using Microsoft.AspNetCore.Http;

public interface ICloudinaryService
{
    Task<string> UploadAvatarAsync(Guid userId, IFormFile file);
    Task<string> UploadProductImageAsync(string productCode, IFormFile file);
    Task DeleteImageAsync(string publicId);
    Task<string> UploadContractPdfAsync(string contractNumber, byte[] pdfBytes);
    Task<byte[]> DownloadRawFileAsync(string publicId);
    Task DeleteRawFileAsync(string publicId);
}
