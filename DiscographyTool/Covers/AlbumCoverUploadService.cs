using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DiscographyTool.Networking;

namespace DiscographyTool.Covers
{
    public static class AlbumCoverUploadService
    {
        public static async Task<Dictionary<string, string>> UploadAsync(
            string rootFolder,
            IEnumerable<string> albumNames)
        {
            var result = new Dictionary<string, string>();

            string coversDir = Path.Combine(rootFolder, "covers");
            if (!Directory.Exists(coversDir))
                return result;

            foreach (var album in albumNames)
            {
                var files = Directory.GetFiles(coversDir, album + ".*");
                if (files.Length == 0)
                    continue;

                var link = await ImageBanUploader.UploadAsync(files[0]);
                if (!string.IsNullOrWhiteSpace(link))
                    result[album] = link;
            }

            return result;
        }
    }
}
