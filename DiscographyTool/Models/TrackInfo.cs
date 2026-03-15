namespace DiscographyTool.Models
{
    public class TrackInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public int BitrateKbps { get; set; }
    }
}
