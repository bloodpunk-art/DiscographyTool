using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscographyTool.Core
{
    public static class DiscographyGenerator
    {
        public static string Generate(
            string rootFolder,
            string performer,
            string genre,
            string country,
            string artistImageUrl,
            string artistLogoUrl,
            Dictionary<string, string> albumImages,
            AudioFormat format,
            ScanStructure scanResult,
            TracklistLayout layout)
        {
            bool isFlatStructure = true;

            foreach (var key in scanResult.Categories.Keys)
            {
                if (key.Equals("Albums", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("EP's", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Singles", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Compilation", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Other", StringComparison.OrdinalIgnoreCase))
                {
                    isFlatStructure = false;
                    break;
                }
            }

            List<ScannedAlbum> albumsToRender = new List<ScannedAlbum>();
            Dictionary<string, List<ScannedAlbum>> categoriesToRender =
                new Dictionary<string, List<ScannedAlbum>>();

            if (isFlatStructure)
            {
                albumsToRender = scanResult.Categories
                    .Values
                    .SelectMany(v => v)
                    .GroupBy(a => a.AlbumPath)
                    .Select(g => g.First())
                    .OrderBy(a => a.Year)
                    .ToList();
            }
            else
            {
                categoriesToRender = scanResult.Categories;
            }

            List<ScannedAlbum> allAlbums = isFlatStructure
                ? albumsToRender
                : categoriesToRender.Values.SelectMany(v => v).ToList();

            var years = allAlbums.Where(a => a.Year > 0).Select(a => a.Year).ToList();
            int yearMin = years.Count > 0 ? years.Min() : 0;
            int yearMax = years.Count > 0 ? years.Max() : 0;

            string bitrateHeader = format == AudioFormat.FLAC
                ? "Lossless"
                : BuildMp3Bitrate(allAlbums);

            int totalSeconds = 0;
            foreach (var a in allAlbums)
            {
                if (a.IsMultiDisc)
                {
                    foreach (var d in a.Discs)
                        totalSeconds += d.Tracks.Sum(t => (int)t.Duration.TotalSeconds);
                }
                else
                {
                    totalSeconds += a.Tracks.Sum(t => (int)t.Duration.TotalSeconds);
                }
            }

            string totalDuration = FormatDuration(totalSeconds);
            string formatLabel = format == AudioFormat.FLAC ? "FLAC" : "MP3";

            int releaseCount = allAlbums.Count;
            string releasesWord = GetRussianReleasesWord(releaseCount);

            string header;
            if (releaseCount == 1 && allAlbums.Count > 0)
            {
                header =
                    $"({genre}) {performer} - {allAlbums[0].AlbumName} ({yearMin}) [{formatLabel}, {bitrateHeader}]";
            }
            else
            {
                header =
                    $"({genre}) {performer} - Дискография ({releaseCount} {releasesWord}) ({yearMin}-{yearMax}) [{formatLabel}, {bitrateHeader}]";
            }

            var sb = new StringBuilder();
            sb.AppendLine(header);
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(artistImageUrl))
                sb.AppendLine("[align=center][img]" + artistImageUrl + "[/img][/align]");
            if (!string.IsNullOrWhiteSpace(artistLogoUrl))
                sb.AppendLine("[align=center][img]" + artistLogoUrl + "[/img][/align]");
            if (!string.IsNullOrWhiteSpace(artistImageUrl) || !string.IsNullOrWhiteSpace(artistLogoUrl))
                sb.AppendLine();

            sb.AppendLine("[align=center][b]🎤 Исполнитель:[/b] " + performer + "[/align]");
            sb.AppendLine("[align=center][b]🎶 Жанр:[/b] " + genre + "[/align]");
            sb.AppendLine("[align=center][b]🌍 Страна:[/b] " + country + "[/align]");
            sb.AppendLine("[align=center][b]🎵 Формат:[/b] " + formatLabel + "[/align]");
            if (format == AudioFormat.MP3)
                sb.AppendLine("[align=center][b]⚡ Битрейт:[/b] " + bitrateHeader + "[/align]");
            sb.AppendLine("[align=center][b]⏱ Продолжительность:[/b] " + totalDuration + "[/align]\n");

            if (isFlatStructure)
            {
                foreach (var album in albumsToRender)
                    RenderAlbum(sb, album, format, albumImages, layout);
            }
            else
            {
                foreach (var category in categoriesToRender)
                {
                    sb.AppendLine("[align=center][size=18][b][color=red]" +
                                  category.Key +
                                  "[/color][/b][/size][/align]\n");

                    foreach (var album in category.Value.OrderBy(a => a.Year))
                        RenderAlbum(sb, album, format, albumImages, layout);
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static void RenderAlbum(
            StringBuilder sb,
            ScannedAlbum a,
            AudioFormat format,
            Dictionary<string, string> albumImages,
            TracklistLayout layout)
        {
            IEnumerable<ScannedTrack> albumTracks =
                a.IsMultiDisc
                    ? a.Discs.SelectMany(d => d.Tracks)
                    : a.Tracks;

            string title = a.Year > 0
                ? a.Year + " - " + a.AlbumName
                : a.AlbumName;

            string suffix = format == AudioFormat.MP3
                ? " (" + BuildMp3BitrateFromTracks(albumTracks) + ")"
                : "";

            sb.AppendLine("[spoiler=\"" + title + suffix + "\"]");

            bool centered = layout == TracklistLayout.Center;
            if (centered)
                sb.AppendLine("[align=center]");

            string coverUrl = "";
            albumImages.TryGetValue(a.AlbumPath, out coverUrl);

            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                sb.AppendLine(centered
                    ? "[img]" + coverUrl + "[/img]"
                    : "[img=right]" + coverUrl + "[/img]");
            }

            if (!a.IsMultiDisc)
            {
                int durationSec = a.Tracks.Sum(t => (int)t.Duration.TotalSeconds);
                sb.AppendLine("[b]⏱ Продолжительность:[/b] " + FormatDuration(durationSec));
                sb.AppendLine("[b]🎵 Треклист:[/b]");

                int i = 1;
                foreach (var t in a.Tracks)
                {
                    string trackText = a.IsSplit
                        ? t.Artist + " - " + t.Title
                        : t.Title;

                    sb.AppendLine("[color=gray]" + i.ToString("D2") +
                                  ".[/color] " + trackText +
                                  " [color=gray][" +
                                  FormatDuration((int)t.Duration.TotalSeconds) +
                                  "][/color]");
                    i++;
                }
            }
            else
            {
                sb.AppendLine("[b]🎵 Треклист:[/b]\n");

                foreach (var disc in a.Discs)
                {
                    string discSuffix = format == AudioFormat.MP3
                        ? " (" + BuildMp3BitrateFromTracks(disc.Tracks) + ")"
                        : "";

                    int discDuration = disc.Tracks.Sum(t => (int)t.Duration.TotalSeconds);

                    sb.AppendLine("[b][font=cursive1]" + disc.Name + discSuffix + "[/font][/b]");
                    sb.AppendLine("[b]⏱ Продолжительность:[/b] " + FormatDuration(discDuration));

                    int i = 1;
                    foreach (var t in disc.Tracks)
                    {
                        string trackText = a.IsSplit
                            ? t.Artist + " - " + t.Title
                            : t.Title;

                        sb.AppendLine("[color=gray]" + i.ToString("D2") +
                                      ".[/color] " + trackText +
                                      " [color=gray][" +
                                      FormatDuration((int)t.Duration.TotalSeconds) +
                                      "][/color]");
                        i++;
                    }

                    sb.AppendLine();
                }
            }

            if (centered)
                sb.AppendLine("[/align]");

            sb.AppendLine("[/spoiler]\n");
        }

        private static string BuildMp3Bitrate(IEnumerable<ScannedAlbum> albums)
        {
            var tracks = albums.SelectMany(a =>
                a.IsMultiDisc
                    ? a.Discs.SelectMany(d => d.Tracks)
                    : a.Tracks);

            return BuildMp3BitrateFromTracks(tracks);
        }

        private static string BuildMp3BitrateFromTracks(IEnumerable<ScannedTrack> tracks)
        {
            var bitrates = tracks
                .Select(t => t.Bitrate)
                .Where(b => b > 0)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            if (bitrates.Count == 0)
                return "Unknown";
            if (bitrates.Count == 1)
                return "CBR " + bitrates[0] + " kbps";

            return "VBR " + bitrates.First() + "-" + bitrates.Last() + " kbps";
        }

        private static string FormatDuration(int totalSeconds)
        {
            var ts = TimeSpan.FromSeconds(totalSeconds);
            return ts.Hours > 0
                ? ts.Hours + ":" + ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2")
                : ts.Minutes + ":" + ts.Seconds.ToString("D2");
        }

        private static string GetRussianReleasesWord(int count)
        {
            if (count % 10 == 1 && count % 100 != 11)
                return "релиз";
            if (count % 10 >= 2 && count % 10 <= 4 &&
                (count % 100 < 10 || count % 100 >= 20))
                return "релиза";
            return "релизов";
        }
    }
}