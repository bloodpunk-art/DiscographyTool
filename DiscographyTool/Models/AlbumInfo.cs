using System.Collections.Generic;

namespace DiscographyTool.Models
{
    public class AlbumInfo
    {
        public string Name { get; set; } = string.Empty;
        public List<TrackInfo> Tracks { get; set; } = new List<TrackInfo>();

        public string CoverUrl { get; set; } = string.Empty;

        public int BitrateMin { get; set; }
        public int BitrateMax { get; set; }
        public string BitrateType { get; set; } = string.Empty;

        public double DurationSeconds { get; set; }
    }
}
