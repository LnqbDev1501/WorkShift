using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class Province
{
    public int ProvinceId { get; set; }

    public string ProvinceCode { get; set; } = null!;

    public string ProvinceName { get; set; } = null!;

    public int RegionId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Region Region { get; set; } = null!;
}
