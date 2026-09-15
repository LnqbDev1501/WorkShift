using System.Globalization;
using System.Text.Json;
using UI_Admin.Models.VietMap;

namespace UI_Admin.Services;

public class VietMapService : IVietMapService
{
    private readonly HttpClient _httpClient;
    private readonly string _servicesKey;
    private readonly ILogger<VietMapService> _logger;

    public VietMapService(HttpClient httpClient, IConfiguration configuration, ILogger<VietMapService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _servicesKey = (configuration["VietMap:ServicesKey"] ?? string.Empty).Trim();
    }

    public async Task<List<VietMapSuggestion>> AutocompleteAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(_servicesKey))
        {
            if (string.IsNullOrWhiteSpace(_servicesKey))
            {
                _logger.LogWarning("VietMap ServicesKey is missing in configuration.");
            }
            return [];
        }

        var url = $"https://maps.vietmap.vn/api/autocomplete/v4?apikey={Uri.EscapeDataString(_servicesKey)}&text={Uri.EscapeDataString(text.Trim())}";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VietMap Autocomplete API returned status code {StatusCode}.", response.StatusCode);
                return [];
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = doc.RootElement;
            var list = new List<VietMapSuggestion>();

            JsonElement itemsElement = root;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                {
                    itemsElement = dataProp;
                }
                else if (root.TryGetProperty("results", out var resProp) && resProp.ValueKind == JsonValueKind.Array)
                {
                    itemsElement = resProp;
                }
                else if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                {
                    itemsElement = itemsProp;
                }
            }

            if (itemsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsElement.EnumerateArray())
                {
                    var refId = GetStringProp(item, "ref_id", "refid", "id");
                    var name = GetStringProp(item, "name", "display", "title");
                    var address = GetStringProp(item, "address", "formatted_address", "display_name");
                    var display = GetStringProp(item, "display", "name", "address");

                    if (!string.IsNullOrWhiteSpace(refId) || !string.IsNullOrWhiteSpace(name))
                    {
                        list.Add(new VietMapSuggestion
                        {
                            RefId = refId,
                            Name = string.IsNullOrWhiteSpace(name) ? display : name,
                            Address = address,
                            Display = string.IsNullOrWhiteSpace(display) ? name : display
                        });
                    }
                }
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling VietMap Autocomplete API.");
            return [];
        }
    }

    public async Task<VietMapPlaceResult?> GetPlaceAsync(string refId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refId) || string.IsNullOrWhiteSpace(_servicesKey))
        {
            if (string.IsNullOrWhiteSpace(_servicesKey))
            {
                _logger.LogWarning("VietMap ServicesKey is missing in configuration.");
            }
            return null;
        }

        var url = $"https://maps.vietmap.vn/api/place/v4?apikey={Uri.EscapeDataString(_servicesKey)}&refid={Uri.EscapeDataString(refId.Trim())}";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VietMap Place API returned status code {StatusCode}.", response.StatusCode);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = doc.RootElement;

            JsonElement target = root;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Object)
                {
                    target = dataProp;
                }
                else if (root.TryGetProperty("result", out var resProp) && resProp.ValueKind == JsonValueKind.Object)
                {
                    target = resProp;
                }
            }

            var lat = GetDoubleProp(target, "lat", "latitude", "y");
            var lng = GetDoubleProp(target, "lng", "longitude", "x");

            if (!lat.HasValue || !lng.HasValue)
            {
                if (target.TryGetProperty("location", out var loc) && loc.ValueKind == JsonValueKind.Object)
                {
                    lat ??= GetDoubleProp(loc, "lat", "latitude");
                    lng ??= GetDoubleProp(loc, "lng", "longitude");
                }
                else if (target.TryGetProperty("geometry", out var geom) && geom.ValueKind == JsonValueKind.Object)
                {
                    if (geom.TryGetProperty("location", out var geomLoc) && geomLoc.ValueKind == JsonValueKind.Object)
                    {
                        lat ??= GetDoubleProp(geomLoc, "lat", "latitude");
                        lng ??= GetDoubleProp(geomLoc, "lng", "longitude");
                    }
                }
            }

            var name = GetStringProp(target, "name", "display", "title");
            var address = GetStringProp(target, "address", "formatted_address", "display_name");

            return new VietMapPlaceResult
            {
                RefId = refId,
                Name = name,
                Address = string.IsNullOrWhiteSpace(address) ? name : address,
                Latitude = lat,
                Longitude = lng
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling VietMap Place API.");
            return null;
        }
    }

    public async Task<VietMapReverseResult?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_servicesKey))
        {
            _logger.LogWarning("VietMap ServicesKey is missing in configuration.");
            return null;
        }

        var latStr = latitude.ToString(CultureInfo.InvariantCulture);
        var lngStr = longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"https://maps.vietmap.vn/api/reverse/v4?apikey={Uri.EscapeDataString(_servicesKey)}&lat={latStr}&lng={lngStr}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VietMap Reverse API returned status code {StatusCode}.", response.StatusCode);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = doc.RootElement;

            JsonElement target = root;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("data", out var dataProp))
                {
                    if (dataProp.ValueKind == JsonValueKind.Object)
                        target = dataProp;
                    else if (dataProp.ValueKind == JsonValueKind.Array && dataProp.GetArrayLength() > 0)
                        target = dataProp[0];
                }
                else if (root.TryGetProperty("result", out var resProp))
                {
                    if (resProp.ValueKind == JsonValueKind.Object)
                        target = resProp;
                    else if (resProp.ValueKind == JsonValueKind.Array && resProp.GetArrayLength() > 0)
                        target = resProp[0];
                }
                else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    target = root[0];
                }
            }

            var address = GetStringProp(target, "address", "formatted_address", "display_name", "display", "name");

            return new VietMapReverseResult
            {
                Address = address,
                Display = address,
                Latitude = latitude,
                Longitude = longitude
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling VietMap Reverse API.");
            return null;
        }
    }

    private static string GetStringProp(JsonElement element, params string[] propNames)
    {
        if (element.ValueKind != JsonValueKind.Object) return string.Empty;

        foreach (var prop in propNames)
        {
            if (element.TryGetProperty(prop, out var val))
            {
                if (val.ValueKind == JsonValueKind.String)
                {
                    var str = val.GetString();
                    if (!string.IsNullOrWhiteSpace(str)) return str.Trim();
                }
            }
        }
        return string.Empty;
    }

    private static double? GetDoubleProp(JsonElement element, params string[] propNames)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;

        foreach (var prop in propNames)
        {
            if (element.TryGetProperty(prop, out var val))
            {
                if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var d))
                {
                    return d;
                }
                if (val.ValueKind == JsonValueKind.String && double.TryParse(val.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dParsed))
                {
                    return dParsed;
                }
            }
        }
        return null;
    }
}
