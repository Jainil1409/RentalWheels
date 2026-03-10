using System.Text.Json;

namespace vehicle_management_system_mvc.Services
{
    public class UnsplashService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessKey;
        private readonly ILogger<UnsplashService> _logger;

        // A local placeholder (served from wwwroot) used when Unsplash is unavailable
        private const string PlaceholderPath = "/images/vehicle-placeholder.svg";

        public UnsplashService(HttpClient httpClient, IConfiguration configuration, ILogger<UnsplashService> logger)
        {
            _httpClient = httpClient;
            _accessKey = configuration["Unsplash:AccessKey"] ?? "";
            _logger = logger;
        }

        public async Task<string?> GetVehicleImageUrlAsync(string brand, string model)
        {
            if (string.IsNullOrEmpty(_accessKey))
            {
                _logger.LogWarning("Unsplash access key is not configured. Returning placeholder image.");
                return PlaceholderPath;
            }

            try
            {
                var query = Uri.EscapeDataString($"{brand} {model} car");
                var url = $"https://api.unsplash.com/search/photos?query={query}&per_page=1&orientation=landscape";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Client-ID {_accessKey}");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Unsplash returned non-success status {Status} for query {Brand} {Model}", response.StatusCode, brand, model);
                    return PlaceholderPath;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var results = doc.RootElement.GetProperty("results");

                if (results.GetArrayLength() == 0)
                {
                    _logger.LogInformation("Unsplash returned no results for {Brand} {Model}", brand, model);
                    return PlaceholderPath;
                }

                var imageUrl = results[0].GetProperty("urls").GetProperty("regular").GetString();
                return imageUrl ?? PlaceholderPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching image from Unsplash for {Brand} {Model}", brand, model);
                return PlaceholderPath;
            }
        }
    }
}
