using UI_Admin.Models.VietMap;

namespace UI_Admin.Services;

public interface IVietMapService
{
    Task<List<VietMapSuggestion>> AutocompleteAsync(string text, CancellationToken cancellationToken = default);
    Task<VietMapPlaceResult?> GetPlaceAsync(string refId, CancellationToken cancellationToken = default);
    Task<VietMapReverseResult?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
