namespace UI_Admin.Models.VietMap;

public class VietMapSuggestion
{
    public string RefId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
}

public class VietMapPlaceResult
{
    public string RefId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class VietMapReverseResult
{
    public string Address { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
