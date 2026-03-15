using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscographyTool.Networking;

namespace DiscographyTool.Covers
{
    public static class ArtistImageService
    {
        private static readonly string[] Extensions =
        {
            ".jpg", ".jpeg", ".png", ".gif"
        };

        public static async Task<(string? ArtistImageUrl, string? ArtistLogoUrl)>
            TryUploadAsync(string rootFolder)
        {
            string? artistImage = FindFile(rootFolder, "band", "photo");
            string? artistLogo = FindFile(rootFolder, "logo");

            string? artistImageUrl = null;
            string? artistLogoUrl = null;

            if (artistImage != null)
                artistImageUrl = await UploadResizedAsync(artistImage);

            if (artistLogo != null)
                artistLogoUrl = await UploadResizedAsync(artistLogo);

            return (artistImageUrl, artistLogoUrl);
        }

        // ===== ПОИСК ФАЙЛА =====
        private static string? FindFile(string rootFolder, params string[] names)
        {
            return Directory.GetFiles(rootFolder)
                .FirstOrDefault(f =>
                {
                    var name = Path.GetFileNameWithoutExtension(f).ToLower();
                    var ext = Path.GetExtension(f).ToLower();
                    return names.Contains(name) && Extensions.Contains(ext);
                });
        }

        // ===== РЕСАЙЗ + ЗАГРУЗКА =====
        private static async Task<string?> UploadResizedAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(ImageBanSession.Token))
                return null;

            string uploadFile = filePath;
            string? tempFile = null;

            using (var image = Image.FromFile(filePath))
            {
                int maxSide = image.Width > image.Height
                    ? image.Width
                    : image.Height;

                if (maxSide > 600)
                {
                    double scale = 600.0 / maxSide;
                    int newW = (int)(image.Width * scale);
                    int newH = (int)(image.Height * scale);

                    using var resized = new Bitmap(newW, newH);
                    using var g = Graphics.FromImage(resized);
                    g.DrawImage(image, 0, 0, newW, newH);

                    tempFile = Path.Combine(
                        Path.GetTempPath(),
                        Path.GetRandomFileName() + ".jpg"
                    );

                    resized.Save(tempFile, ImageFormat.Jpeg);
                    uploadFile = tempFile;
                }
            }

            var link = await ImageBanUploader.UploadAsync(uploadFile);

            if (tempFile != null && File.Exists(tempFile))
                File.Delete(tempFile);

            return link;
        }
    }
}
