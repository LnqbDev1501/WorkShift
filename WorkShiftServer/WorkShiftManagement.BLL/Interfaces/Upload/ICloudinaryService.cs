namespace WorkShiftManagement.BLL.Interfaces.Upload;

using Microsoft.AspNetCore.Http;

public interface ICloudinaryService
{
    Task<string> UploadAvatarAsync(Guid userId, IFormFile file);
}
