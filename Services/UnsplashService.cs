using System.Text.Json;

namespace vehicle_management_system_mvc.Services
{
    public class UnsplashService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessKey;

        public UnsplashService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _accessKey = configuration["Unsplash:AccessKey"] ?? "";
        }

        public async Task<string?> GetVehicleImageUrlAsync(string brand, string model)
        {
            if (string.IsNullOrEmpty(_accessKey))
                return null;

            var query = Uri.EscapeDataString($"{brand} {model} car");
            var url = $"https://api.unsplash.com/search/photos?query={query}&per_page=1&orientation=landscape";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Client-ID {_accessKey}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");

            if (results.GetArrayLength() == 0)
                return null;

            return results[0].GetProperty("urls").GetProperty("regular").GetString();
        }
    }
}
