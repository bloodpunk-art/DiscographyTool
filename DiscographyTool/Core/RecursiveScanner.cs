using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TagLib;

namespace DiscographyTool.Core
{
    public static class RecursiveScanner
    {
        private static readonly string[] AudioExtensions = { ".mp3", ".flac" };
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

        public static ScanStructure Scan(string rootFolder)
        {
            var structure = new ScanStructure { RootFolder = rootFolder };

            var topLevelDirs = Directory.GetDirectories(rootFolder)
                .Where(d => !Path.GetFileName(d).Equals("covers", StringComparison.OrdinalIgnoreCase))
                .ToList();

            bool hasCategories = topLevelDirs.Any(d =>
            {
                var name = Path.GetFileName(d);
                if (IsAlbumFolderName(name))
                    return false;

                return Directory.GetDirectories(d).Any(sd =>
                    Directory.GetFiles(sd)
                        .Any(f => AudioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())));
            });

            if (!hasCategories)
            {
                var albums = ScanAlbumsInDirectory(rootFolder);
                if (albums.Any())
                {
                    structure.Categories[""] = albums;
                }
            }
            else
            {
                var categoryDirs = topLevelDirs.OrderBy(d =>
                {
                    var name = Path.GetFileName(d).ToLowerInvariant();
                    if (name == "other") return 1000;
                    if (name.Contains("album")) return 1;
                    if (name.Contains("ep") || name.Contains("single")) return 2;
                    return 500;
                }).ToList();

                foreach (var catDir in categoryDirs)
                {
                    var catName = Path.GetFileName(catDir);
                    var albumsInCat = ScanAlbumsInDirectory(catDir);

                    if (albumsInCat.Any())
                    {
                        structure.Categories[catName] = albumsInCat;
                    }
                }
            }

            // Определяем DetectedArtist и DetectedGenre после сканирования всех альбомов
            var allTracks = structure.Categories.Values
                .SelectMany(albs => albs)
                .SelectMany(a => a.IsMultiDisc ? a.Discs.SelectMany(d => d.Tracks) : a.Tracks);

            structure.DetectedArtist = allTracks
                .Select(t => t.Artist)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "";

            structure.DetectedGenre = allTracks
                .SelectMany(t => SplitGenres(t.Genre))
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(3) // ограничиваем до 3 самых частых жанров
                .Aggregate((a, b) => a + " / " + b);

            return structure;
        }

        private static List<ScannedAlbum> ScanAlbumsInDirectory(string dir)
        {
            var albums = new List<ScannedAlbum>();

            var subdirs = Directory.GetDirectories(dir);

            foreach (var albumDir in subdirs)
            {
                var album = BuildAlbum(albumDir);
                if (album != null)
                    albums.Add(album);
            }

            // Если в папке нет подпапок-альбомов, но есть аудиофайлы прямо в dir — считаем это одним альбомом
            if (!albums.Any() && HasAudioFiles(dir))
            {
                var album = BuildAlbum(dir);
                if (album != null)
                    albums.Add(album);
            }

            return albums;
        }

        private static ScannedAlbum? BuildAlbum(string albumDir)
        {
            var discDirs = Directory.GetDirectories(albumDir)
                .Where(d => IsDiscFolder(Path.GetFileName(d)))
                .ToList();

            if (discDirs.Any())
            {
                var discs = new List<ScannedDisc>();

                foreach (var discDir in discDirs)
                {
                    var tracks = GetTracksFromDirectory(discDir);
                    if (tracks.Any())
                    {
                        var disc = new ScannedDisc(
                            Path.GetFileName(discDir),
                            discDir,
                            FindCoverInDir(discDir),
                            tracks
                        );
                        discs.Add(disc);
                    }
                }

                if (discs.Any())
                {
                    var album = BuildMultiDiscAlbum(discs, albumDir);

                    // === НОВОЕ: проверка на split ===
                    album.IsSplit = IsSplitAlbum(album);

                    return album;
                }
            }
            else
            {
                var tracks = GetTracksFromDirectory(albumDir);
                if (tracks.Any())
                {
                    var cover = FindCoverInDir(albumDir);

                    string albumName = !string.IsNullOrWhiteSpace(tracks.First().Album)
                        ? tracks.First().Album.Trim()
                        : Path.GetFileName(albumDir);

                    int year = tracks
                        .Where(t => t.Year > 0)
                        .Select(t => t.Year)
                        .DefaultIfEmpty(0)
                        .Max();

                    var album = new ScannedAlbum(albumDir, albumName, year, cover, tracks);

                    // === НОВОЕ: проверка на split ===
                    album.IsSplit = IsSplitAlbum(album);

                    return album;
                }
            }

            return null;
        }

        private static bool IsSplitAlbum(ScannedAlbum album)
        {
            var artists = album.IsMultiDisc
                ? album.Discs.SelectMany(d => d.Tracks).Select(t => t.Artist)
                : album.Tracks.Select(t => t.Artist);

            var distinctArtists = artists
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Если больше 1 уникального артиста (игнорируя пустые) → это split
            return distinctArtists.Count > 1;
        }

        private static List<ScannedTrack> GetTracksFromDirectory(string dir)
        {
            var tracks = new List<ScannedTrack>();

            var files = Directory.GetFiles(dir)
                .Where(f => AudioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f) // сортировка по имени файла
                .ToList();

            foreach (var file in files)
            {
                try
                {
                    using var tagFile = TagLib.File.Create(file);
                    var tag = tagFile.Tag;
                    var props = tagFile.Properties;

                    var track = new ScannedTrack
                    {
                        FilePath = file,
                        Title = tag.Title ?? Path.GetFileNameWithoutExtension(file),
                        Artist = tag.FirstPerformer ?? "",
                        Album = tag.Album ?? "",
                        Year = (int)tag.Year,
                        Genre = tag.FirstGenre ?? "",
                        Duration = props.Duration,
                        Bitrate = (int)props.AudioBitrate
                    };

                    tracks.Add(track);
                }
                catch
                {
                    // Пропускаем битые файлы
                }
            }

            return tracks;
        }

        private static ScannedAlbum BuildMultiDiscAlbum(List<ScannedDisc> discs, string albumDir)
        {
            var firstTrack = discs.SelectMany(d => d.Tracks).FirstOrDefault();

            string albumName = !string.IsNullOrWhiteSpace(firstTrack?.Album)
                ? firstTrack.Album.Trim()
                : "Multi-Disc Album";

            int year = discs.SelectMany(d => d.Tracks)
                .Where(t => t.Year > 0)
                .Select(t => t.Year)
                .DefaultIfEmpty(0)
                .Max();

            string coverPath = discs
                .Select(d => d.CoverPath)
                .FirstOrDefault(c => !string.IsNullOrEmpty(c))
                ?? FindCoverInDir(albumDir)
                ?? "";

            return new ScannedAlbum(albumDir, albumName, year, coverPath, discs)
            {
                // IsSplit уже установлен в BuildAlbum
            };
        }

        private static bool IsDiscFolder(string folderName)
        {
            var lower = folderName.ToLowerInvariant();
            return lower.StartsWith("cd") ||
                   lower.StartsWith("disc") ||
                   lower.StartsWith("диск") ||
                   lower.Contains(" cd") ||
                   lower.Contains("диск ");
        }

        private static string? FindCoverInDir(string dir)
        {
            var files = Directory.GetFiles(dir)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (files.Count == 0) return null;

            var priorityNames = new[] { "cover", "front", "folder", "cd", "back" };

            foreach (var pri in priorityNames)
            {
                var match = files.FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains(pri));
                if (match != null) return match;
            }

            return files[0];
        }

        private static IEnumerable<string> SplitGenres(string? genre)
        {
            if (string.IsNullOrWhiteSpace(genre)) yield break;

            var separators = new[] { ',', '/', '|', ';' };
            foreach (var part in genre.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                var clean = part.Trim();
                if (!string.IsNullOrWhiteSpace(clean)) yield return clean;
            }
        }

        private static bool HasAudioFiles(string dir)
        {
            return Directory.GetFiles(dir)
                .Any(f => AudioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        }

        private static bool IsAlbumFolderName(string name)
        {
            var lower = name.ToLowerInvariant();
            // Простые эвристики, можно расширить
            return lower.Contains(" - ") || // Artist - Album
                   Regex.IsMatch(lower, @"\(\d{4}\)") || // (1991)
                   Regex.IsMatch(lower, @"\d{4}") && lower.Length > 10; // содержит год и длинное имя
        }
    }
}