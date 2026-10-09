using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WorkShiftManagement.BLL.DTOs.Staff.Attendance.Request;

public sealed class CheckOutRequest
{
    [Required]
    public int AssignmentId { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public decimal Latitude { get; set; }

    [Required]
    public decimal Longitude { get; set; }

    public decimal? GpsAccuracy { get; set; }

    [Required]
    public IFormFile FaceImage { get; set; } = null!;
}
