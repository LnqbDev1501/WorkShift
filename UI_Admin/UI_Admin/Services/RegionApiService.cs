using UI_Admin.Models;

namespace UI_Admin.Services
{
    public class RegionApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public RegionApiService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;

            _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "WorkShiftApi:BaseUrl is missing.");
        }

        public async Task<List<RegionViewModel>> GetAllAsync()
        {
            var url =
                $"{_apiBaseUrl.TrimEnd('/')}/api/admin/regions?pageNumber=1&pageSize=100";

            using var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<
                    PagedApiResponse<RegionViewModel>>();

            return result?.Items ?? [];
        }

        private class PagedApiResponse<T>
        {
            public List<T> Items { get; set; } = [];

            public int PageNumber { get; set; }

            public int PageSize { get; set; }

            public int TotalItems { get; set; }

            public int TotalPages { get; set; }
        }
    }
}
