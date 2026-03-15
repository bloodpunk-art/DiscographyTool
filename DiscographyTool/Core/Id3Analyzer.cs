using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TagLib;

namespace DiscographyTool.Core
{
    public static class Id3Analyzer
    {
        public static Id3Discography Analyze(string rootFolder)
        {
            var albums = new List<Id3Album>();

            foreach (var dir in GetAllAlbumDirs(rootFolder))
            {
                var album = AnalyzeAlbum(dir);
                if (album.Tracks.Count > 0)
                    albums.Add(album);
            }

            var artist = albums
                .SelectMany(a => a.Tracks)
                .Select(t => t.Artist)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "";

            var genres = albums
                .SelectMany(a => a.Tracks)
                .SelectMany(t => SplitGenres(t.Genre))
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            return new Id3Discography
            {
                Artist = artist,
                Genre = string.Join(" / ", genres),
                Albums = albums
            };
        }

        // =========================

        private static Id3Album AnalyzeAlbum(string albumDir)
        {
            var tracks = new List<Id3Track>();
            string pattern = DetectPattern(albumDir);

            foreach (var file in Directory.GetFiles(albumDir, pattern))
            {
                try
                {
                    using (var f = TagLib.File.Create(file))
                    {
                        tracks.Add(new Id3Track
                        {
                            Artist = f.Tag.FirstPerformer ?? "",
                            Title = f.Tag.Title ?? Path.GetFileNameWithoutExtension(file),
                            Album = f.Tag.Album ?? "",
                            Year = (int)f.Tag.Year,
                            Genre = f.Tag.FirstGenre ?? "",
                            DurationSeconds = (int)f.Properties.Duration.TotalSeconds,
                            Bitrate = f.Properties.AudioBitrate
                        });
                    }
                }
                catch
                {
                    // игнорируем битые файлы
                }
            }

            int year = tracks.Select(t => t.Year).FirstOrDefault(y => y > 0);
            string albumArtist = tracks
                .Select(t => t.Artist)
                .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a)) ?? "";

            string albumName = GetCleanAlbumName(
                tracks.FirstOrDefault() != null ? tracks.First().Album : "",
                Path.GetFileName(albumDir),
                albumArtist
            );

            bool isCompilation =
                tracks.Select(t => t.Artist)
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() > 1;

            return new Id3Album
            {
                FolderName = Path.GetFileName(albumDir),
                AlbumName = albumName,
                AlbumArtist = albumArtist,
                Year = year,
                IsCompilation = isCompilation,
                Tracks = tracks
            };
        }

        // =========================

        private static IEnumerable<string> GetAllAlbumDirs(string root)
        {
            var result = new List<string>();

            foreach (var dir in Directory.GetDirectories(root))
            {
                if (HasAudioFiles(dir))
                {
                    result.Add(dir);
                    continue;
                }

                foreach (var sub in Directory.GetDirectories(dir))
                {
                    if (HasAudioFiles(sub))
                        result.Add(sub);
                }
            }

            return result;
        }

        private static bool HasAudioFiles(string dir)
        {
            return Directory.GetFiles(dir, "*.mp3").Any()
                || Directory.GetFiles(dir, "*.flac").Any();
        }

        private static string DetectPattern(string dir)
        {
            return Directory.GetFiles(dir, "*.flac").Any()
                ? "*.flac"
                : "*.mp3";
        }

        private static IEnumerable<string> SplitGenres(string genre)
        {
            if (string.IsNullOrWhiteSpace(genre))
                yield break;

            foreach (var g in Regex.Split(genre, @"[/;,]"))
            {
                var clean = g.Trim();
                if (!string.IsNullOrWhiteSpace(clean))
                    yield return clean;
            }
        }

        private static string GetCleanAlbumName(
            string tagAlbum,
            string folderName,
            string artist)
        {
            if (!string.IsNullOrWhiteSpace(tagAlbum))
                return tagAlbum.Trim();

            string name = folderName;

            if (!string.IsNullOrWhiteSpace(artist))
            {
                var prefix = artist + " - ";
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(prefix.Length);
            }

            name = Regex.Replace(name, @"\s*\(\d{4}\)\s*$", "");
            return name.Trim();
        }
    }

    // =========================
    // МОДЕЛИ
    // =========================

    public class Id3Discography
    {
        public string Artist = "";
        public string Genre = "";
        public List<Id3Album> Albums = new List<Id3Album>();
    }

    public class Id3Album
    {
        public string FolderName = "";
        public string AlbumName = "";
        public string AlbumArtist = "";
        public int Year;
        public bool IsCompilation;
        public List<Id3Track> Tracks = new List<Id3Track>();
    }

    public class Id3Track
    {
        public string Artist = "";
        public string Title = "";
        public string Album = "";
        public int Year;
        public string Genre = "";
        public int DurationSeconds;
        public int Bitrate;
    }
}
