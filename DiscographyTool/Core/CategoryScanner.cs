using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiscographyTool.Core
{
    public static class CategoryScanner
    {
        public static Dictionary<string, List<string>> Scan(string root)
        {
            var result = new Dictionary<string, List<string>>();
            var subdirs = Directory.GetDirectories(root);

            bool hasCategories = subdirs.Any(d =>
                Directory.GetDirectories(d)
                    .Any(sd => Directory.GetFiles(sd, "*.mp3").Any()));

            // Нет категорий — считаем, что все альбомы лежат в корне
            if (!hasCategories)
            {
                result["Albums"] = Directory.GetDirectories(root)
                    .Where(d => Path.GetFileName(d).ToLower() != "covers")
                    .ToList();

                return result;
            }

            // Есть категории
            foreach (var cat in subdirs)
            {
                var albums = Directory.GetDirectories(cat)
                    .Where(d => Directory.GetFiles(d, "*.mp3").Any())
                    .ToList();

                if (albums.Count > 0)
                    result[Path.GetFileName(cat)] = albums;
            }

            return result;
        }
    }
}
