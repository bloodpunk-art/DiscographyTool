using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DiscographyTool.Covers
{
    public static class CoverProcessor
    {
        private static readonly string[] ImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".gif"
        };

        private static readonly string[] PriorityNames =
        {
            "folder",
            "front",
            "cover"
        };

        private const int TargetSize = 400;

        public static void Process(string rootDir)
        {
            string coversDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DiscographyTool",
                "covers"
            );

            Directory.CreateDirectory(coversDir);

            foreach (var currentDir in Directory.GetDirectories(rootDir, "*", SearchOption.AllDirectories))
            {
                string folderName = Path.GetFileName(currentDir);

                var imageFile = FindBestImage(currentDir);
                if (imageFile == null)
                    continue;

                string dstPath = Path.Combine(
                    coversDir,
                    folderName + Path.GetExtension(imageFile).ToLower()
                );

                try
                {
                    ResizeProportional(imageFile, dstPath);
                }
                catch
                {
                    // игнорируем ошибки, как и раньше
                }
            }
        }

        private static string? FindBestImage(string dir)
        {
            var files = Directory.GetFiles(dir)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToList();

            if (files.Count == 0)
                return null;

            foreach (var name in PriorityNames)
            {
                var match = files.FirstOrDefault(f =>
                    string.Equals(
                        Path.GetFileNameWithoutExtension(f),
                        name,
                        StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    return match;
            }

            return files[0];
        }

        private static void ResizeProportional(string srcPath, string dstPath)
        {
            using var img = Image.FromFile(srcPath);

            int w = img.Width;
            int h = img.Height;

            if (w <= TargetSize && h <= TargetSize)
            {
                img.Save(dstPath);
                return;
            }

            double scale = (double)TargetSize / Math.Min(w, h);
            int newW = (int)Math.Round(w * scale);
            int newH = (int)Math.Round(h * scale);

            using var resized = new Bitmap(newW, newH);
            using var g = Graphics.FromImage(resized);

            g.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.DrawImage(img, 0, 0, newW, newH);
            resized.Save(dstPath);
        }
    }
}
