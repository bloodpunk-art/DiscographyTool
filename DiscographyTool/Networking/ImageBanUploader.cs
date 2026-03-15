using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DiscographyTool.Networking
{
    public static class ImageBanUploader
    {
        private static readonly HttpClient _http = new HttpClient();

        // ===== НОВЫЙ БАЗОВЫЙ МЕТОД (через мульти-токен сессию)
        public static async Task<string> UploadAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(ImageBanSession.Token))
                return null;

            return await UploadInternalAsync(filePath, ImageBanSession.Token);
        }

        // ===== СТАРЫЙ СОВМЕСТИМЫЙ МЕТОД
        public static async Task<string> UploadAsync(string filePath, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            return await UploadInternalAsync(filePath, token);
        }

        // ===== ОБЩАЯ РЕАЛИЗАЦИЯ
        private static async Task<string> UploadInternalAsync(string filePath, string token)
        {
            if (!File.Exists(filePath))
                return null;

            using (var content = new MultipartFormDataContent())
            using (var fs = File.OpenRead(filePath))
            {
                content.Add(new StreamContent(fs), "image", Path.GetFileName(filePath));

                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.PostAsync(
                    "https://api.imageban.ru/v1/upload",
                    content
                );

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);

                var link = obj["data"]?["link"];
                if (link != null)
                    return link.ToString();

                return null;
            }
        }
    }
}
