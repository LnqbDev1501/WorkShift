using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UI_Admin.Models;

public sealed class EditShiftTemplateViewModel : CreateShiftTemplateViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
    [BindNever]
    public string Status { get; set; } = string.Empty;
    [BindNever]
    public DateTime? CreatedAt { get; set; }
    [BindNever]
    public DateTime? UpdatedAt { get; set; }
}
