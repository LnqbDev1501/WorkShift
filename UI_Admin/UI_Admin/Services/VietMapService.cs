using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using UI_Admin.Models.VietMap;

namespace UI_Admin.Services;

public class VietMapService : IVietMapService
{
    private readonly HttpClient _httpClient;
    private readonly string _servicesKey;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VietMapService> _logger;

    public VietMapService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache, ILogger<VietMapService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
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

        var normalizedText = text.Trim().ToLowerInvariant();
        var cacheKey = $"vietmap:auto:{normalizedText}";
        if (_cache.TryGetValue(cacheKey, out List<VietMapSuggestion>? cached) && cached != null)
        {
            return cached;
        }

        var url = $"https://maps.vietmap.vn/api/autocomplete/v4?apikey={Uri.EscapeDataString(_servicesKey)}&text={Uri.EscapeDataString(text.Trim())}";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("VietMap Autocomplete API rate limited (HTTP 429).");
                }
                else
                {
                    _logger.LogWarning("VietMap Autocomplete API returned status code {StatusCode}.", response.StatusCode);
                }
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

            _cache.Set(cacheKey, list, TimeSpan.FromHours(2));
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

        var normalizedRefId = refId.Trim().ToLowerInvariant();
        var cacheKey = $"vietmap:place:{normalizedRefId}";
        if (_cache.TryGetValue(cacheKey, out VietMapPlaceResult? cached) && cached != null)
        {
            return cached;
        }

        var url = $"https://maps.vietmap.vn/api/place/v4?apikey={Uri.EscapeDataString(_servicesKey)}&refid={Uri.EscapeDataString(refId.Trim())}";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("VietMap Place API rate limited (HTTP 429).");
                }
                else
                {
                    _logger.LogWarning("VietMap Place API returned status code {StatusCode}.", response.StatusCode);
                }
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

            var result = new VietMapPlaceResult
            {
                RefId = refId,
                Name = name,
                Address = string.IsNullOrWhiteSpace(address) ? name : address,
                Latitude = lat,
                Longitude = lng
            };

            _cache.Set(cacheKey, result, TimeSpan.FromHours(4));
            return result;
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

        var latStr = latitude.ToString("F6", CultureInfo.InvariantCulture);
        var lngStr = longitude.ToString("F6", CultureInfo.InvariantCulture);
        var exactCoords = $"{latStr}, {lngStr}";

        var cacheKey = $"vietmap:rev:{latStr}:{lngStr}";
        if (_cache.TryGetValue(cacheKey, out VietMapReverseResult? cached) && cached != null)
        {
            return cached;
        }

        var url = $"https://maps.vietmap.vn/api/reverse/v4?apikey={Uri.EscapeDataString(_servicesKey)}&lat={latStr}&lng={lngStr}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("VietMap Reverse API returned 429 TooManyRequests. Using coordinate fallback.");
                    var fallback = new VietMapReverseResult
                    {
                        Address = exactCoords,
                        Display = exactCoords,
                        Latitude = latitude,
                        Longitude = longitude
                    };
                    // Cache the fallback for 30s to avoid spamming the provider during rate limit
                    _cache.Set(cacheKey, fallback, TimeSpan.FromSeconds(30));
                    return fallback;
                }

                _logger.LogWarning("VietMap Reverse API returned status code {StatusCode}.", response.StatusCode);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = doc.RootElement;

            JsonElement target = default;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                target = root[0];
            }
            else if (root.ValueKind == JsonValueKind.Object)
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
                else if (root.TryGetProperty("results", out var resultsProp) && resultsProp.ValueKind == JsonValueKind.Array && resultsProp.GetArrayLength() > 0)
                {
                    target = resultsProp[0];
                }
                else
                {
                    target = root;
                }
            }

            var display = GetStringProp(target, "display", "formatted_address");
            var address = GetStringProp(target, "address");
            var name = GetStringProp(target, "name");

            string fullAddress = !string.IsNullOrWhiteSpace(display)
                ? display
                : (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(address) && !address.Contains(name)
                    ? $"{name}, {address}"
                    : (!string.IsNullOrWhiteSpace(name) ? name : address));

            var resolvedText = string.IsNullOrWhiteSpace(fullAddress) ? exactCoords : fullAddress;

            var reverseResult = new VietMapReverseResult
            {
                Address = resolvedText,
                Display = resolvedText,
                Latitude = latitude,
                Longitude = longitude
            };

            _cache.Set(cacheKey, reverseResult, TimeSpan.FromHours(4));
            return reverseResult;
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
