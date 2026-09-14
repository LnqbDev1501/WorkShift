using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models;

public sealed class ChangeShiftTemplateStatusViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
    [Required]
    public bool? Restore { get; set; }
    public string? ReturnUrl { get; set; }
}
