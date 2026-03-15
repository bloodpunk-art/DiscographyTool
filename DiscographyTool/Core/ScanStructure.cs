using System;
using System.Collections.Generic;

namespace DiscographyTool.Core
{
    public class ScanStructure
    {
        public string RootFolder { get; set; }
        public Dictionary<string, List<ScannedAlbum>> Categories { get; }
        public string DetectedArtist { get; set; }
        public string DetectedGenre { get; set; }

        public ScanStructure()
        {
            RootFolder = string.Empty;
            Categories = new Dictionary<string, List<ScannedAlbum>>();
            DetectedArtist = string.Empty;
            DetectedGenre = string.Empty;
        }
    }

    public class ScannedAlbum
    {
        public string AlbumPath { get; set; }
        public string AlbumName { get; set; }
        public int Year { get; set; }
        public string CoverPath { get; set; }
        public bool IsMultiDisc { get; set; }
        public List<ScannedDisc> Discs { get; }
        public List<ScannedTrack> Tracks { get; }

        // === НОВОЕ: определяет split-альбом (несколько артистов) ===
        public bool IsSplit { get; set; } = false;

        public ScannedAlbum()
        {
            AlbumPath = string.Empty;
            AlbumName = string.Empty;
            CoverPath = string.Empty;
            Discs = new List<ScannedDisc>();
            Tracks = new List<ScannedTrack>();
        }

        public ScannedAlbum(
            string albumPath,
            string albumName,
            int year,
            string coverPath,
            List<ScannedTrack> tracks
        ) : this()
        {
            AlbumPath = albumPath;
            AlbumName = albumName;
            Year = year;
            CoverPath = coverPath;
            Tracks.AddRange(tracks);
        }

        public ScannedAlbum(
            string albumPath,
            string albumName,
            int year,
            string coverPath,
            List<ScannedDisc> discs
        ) : this()
        {
            AlbumPath = albumPath;
            AlbumName = albumName;
            Year = year;
            CoverPath = coverPath;
            IsMultiDisc = true;
            Discs.AddRange(discs);
        }
    }

    public class ScannedDisc
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string CoverPath { get; set; }
        public List<ScannedTrack> Tracks { get; }

        public ScannedDisc()
        {
            Name = string.Empty;
            Path = string.Empty;
            CoverPath = string.Empty;
            Tracks = new List<ScannedTrack>();
        }

        public ScannedDisc(
            string name,
            string path,
            string coverPath,
            List<ScannedTrack> tracks
        ) : this()
        {
            Name = name;
            Path = path;
            CoverPath = coverPath;
            Tracks.AddRange(tracks);
        }
    }

    public class ScannedTrack
    {
        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public int Year { get; set; }
        public string Genre { get; set; }
        public TimeSpan Duration { get; set; }
        public int Bitrate { get; set; }

        public ScannedTrack()
        {
            FilePath = string.Empty;
            Title = string.Empty;
            Artist = string.Empty;
            Album = string.Empty;
            Genre = string.Empty;
            Duration = TimeSpan.Zero;
        }
    }
}